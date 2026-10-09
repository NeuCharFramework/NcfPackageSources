import fcntl
import json
import logging
import os
import shutil
import signal
import subprocess
import sys
import threading
import time
import zipfile
from pathlib import Path

from . import VERSION
from .config import OFFLINE_ENV, WorkerError, safe_path
from .models import list_models, validate_model_request
from .storage import Store, TERMINAL, atomic_write
from .telemetry import Telemetry

LOG = logging.getLogger(__name__)
PACKAGE_ROOT = Path(__file__).resolve().parent.parent


class Worker:
    def __init__(self, settings):
        self.settings = settings
        settings.data.mkdir(parents=True, exist_ok=True)
        self.instance_lock = safe_path(settings.data, "worker.lock").open("a")
        try:
            fcntl.flock(self.instance_lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError as exc:
            self.instance_lock.close()
            raise RuntimeError("Another worker already owns this data root; use one API process.") from exc
        self.store = Store(settings)
        self.lock = threading.RLock()
        self.preflight_lock = threading.Lock()
        self.wake = threading.Event()
        self.stopping = False
        self.process = None
        self.active_id = None
        self.cancel_deadline = None
        self.stop_state = None
        self.stop_error = None
        for job in self.store.unfinished():
            if job["state"] != "Queued":
                self._reap_interrupted(job["id"])
                self.store.transition(job["id"], "Interrupted", "Worker restarted before the child job completed; no automatic resume.")
                self._publish(job["id"])
        self.capabilities = self._probe("capabilities", None)
        self.thread = threading.Thread(target=self._scheduler, name="training-scheduler", daemon=True)
        self.thread.start()

    def _environment(self):
        env = os.environ.copy()
        env.update(OFFLINE_ENV)
        env["PYTHONPATH"] = str(PACKAGE_ROOT)
        env["PYTHONUNBUFFERED"] = "1"
        env["NCF_TORCH_THREADS"] = str(self.settings.torch_threads)
        env["NCF_EVENT_LIMIT"] = str(self.settings.event_limit)
        env["OMP_NUM_THREADS"] = str(self.settings.torch_threads)
        env["MKL_NUM_THREADS"] = str(self.settings.torch_threads)
        # Training never needs credentials, cloud tokens, or proxy secrets.
        for key in list(env):
            if any(part in key.upper() for part in ("KEY", "TOKEN", "SECRET", "PASSWORD", "PROXY")) and key not in OFFLINE_ENV:
                del env[key]
        return env

    def _reap_interrupted(self, job_id):
        import psutil

        spec = str(safe_path(self.settings.data, "jobs", job_id, "spec.json"))
        matching = []
        for process in psutil.process_iter(["pid", "cmdline", "uids"]):
            try:
                command = process.info["cmdline"] or []
                if "worker.runner" in command and "--spec" in command and spec in command and process.uids().real == os.getuid():
                    if os.getpgid(process.pid) != process.pid:
                        raise RuntimeError("Interrupted child has an unexpected process group.")
                    os.killpg(process.pid, signal.SIGTERM)
                    matching.append(process)
            except (psutil.NoSuchProcess, ProcessLookupError):
                continue
            except psutil.AccessDenied:
                continue
        _, alive = psutil.wait_procs(matching, timeout=self.settings.cancel_grace_seconds)
        for process in alive:
            try:
                os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
        if alive:
            _, survivors = psutil.wait_procs(alive, timeout=5)
            if survivors:
                raise RuntimeError("Interrupted process tree could not be stopped; refusing to start another training job.")

    def _probe(self, operation, spec):
        process = subprocess.Popen(
            [sys.executable, "-m", "worker.probe", operation], cwd=PACKAGE_ROOT,
            env=self._environment(), stdin=subprocess.PIPE, stdout=subprocess.PIPE,
            stderr=subprocess.PIPE, start_new_session=True,
        )
        try:
            stdout, stderr = process.communicate(
                input=json.dumps(spec).encode() if spec else None,
                timeout=self.settings.preflight_seconds,
            )
        except subprocess.TimeoutExpired as exc:
            os.killpg(process.pid, signal.SIGKILL)
            process.communicate()
            raise WorkerError("Offline backend/model preflight timed out; model was not queued.", 422) from exc
        try:
            result = json.loads(stdout.decode().splitlines()[-1])
        except (ValueError, IndexError, UnicodeError) as exc:
            LOG.error("Backend probe failed: %s", stderr.decode(errors="replace")[-4096:])
            raise WorkerError("Backend probe failed; inspect worker logs.", 503) from exc
        if process.returncode or not result["ok"]:
            raise WorkerError("Preflight rejected: " + result.get("error", "backend dependency failure"))
        return result["result"]

    def health(self):
        active, queued = self.store.counts()
        return dict(status="ok" if not self.stopping else "stopping", version=VERSION,
                    capabilities=self.capabilities, activeJobs=active, queuedJobs=queued, storeId=self.store.store_id)

    def models(self):
        models, rejected = list_models(self.settings.models)
        for name, reason in rejected:
            LOG.warning("Excluded local model %s: %s", name, reason)
        return models

    def create_job(self, request):
        if not self.preflight_lock.acquire(blocking=False):
            raise WorkerError("Another job preflight is in progress; retry shortly.", 409)
        try:
            with self.lock:
                if self.stopping:
                    raise WorkerError("Worker is stopping.", 503)
                if self.store.counts()[1] >= self.settings.queue_limit:
                    raise WorkerError("Worker queue is full.", 429)
                if request["maxDurationMinutes"] > self.settings.max_duration_minutes:
                    raise WorkerError(f"maxDurationMinutes exceeds the operator budget ({self.settings.max_duration_minutes}).")
                if shutil.disk_usage(self.settings.data).free < self.settings.min_free_bytes:
                    raise WorkerError("Insufficient free disk space to start training.", 507)
            capability = next(item for item in self.capabilities if item["backend"] == request["backend"])
            if not capability["available"] or request["method"] not in capability["methods"]:
                raise WorkerError(capability["reason"] or "Requested backend/method is not available.", 422)
            validate_model_request(self.settings.models, request)
            self.store.read_dataset(request["datasetId"])
            if request["evalDatasetId"]:
                if request["evalDatasetId"] == request["datasetId"]:
                    raise WorkerError("Training and evaluation datasets must be different.")
                self.store.read_dataset(request["evalDatasetId"])
            self._probe("preflight", self._spec(request))
            with self.lock:
                if self.stopping:
                    raise WorkerError("Worker is stopping.", 503)
                job = self.store.create_job(request)
                self.wake.set()
                return job
        finally:
            self.preflight_lock.release()

    def _spec(self, request):
        return dict(
            request=request, modelsRoot=str(self.settings.models),
            datasetPath=str(safe_path(self.settings.data, "datasets", request["datasetId"] + ".jsonl")),
            evalDatasetPath=str(safe_path(self.settings.data, "datasets", request["evalDatasetId"] + ".jsonl")) if request["evalDatasetId"] else None,
        )

    def cancel(self, job_id):
        with self.lock:
            job = self.store.job(job_id)
            if job["state"] in TERMINAL or job["state"] == "Cancelling":
                return job
            if job["state"] == "Queued":
                return self.store.transition(job_id, "Cancelled")
            self.store.transition(job_id, "Cancelling")
            self._stop("Cancelled", None)
            return self.store.job(job_id)

    def _stop(self, state, error):
        if self.stop_state is not None:
            return
        self.stop_state, self.stop_error = state, error
        self.cancel_deadline = time.monotonic() + self.settings.cancel_grace_seconds
        if self.process is not None and self.process.poll() is None:
            try:
                os.killpg(self.process.pid, signal.SIGTERM)
            except ProcessLookupError:
                pass

    def _scheduler(self):
        try:
            while True:
                with self.lock:
                    if self.stopping:
                        return
                    queued = next((job for job in self.store.unfinished() if job["state"] == "Queued"), None)
                    if queued:
                        self._launch(queued)
                if queued:
                    self._monitor(queued)
                else:
                    self.wake.wait(0.5)
                    self.wake.clear()
        except Exception:
            LOG.exception("Training scheduler failed; worker is unhealthy.")
            with self.lock:
                self.stopping = True
                if self.process and self.process.poll() is None:
                    os.killpg(self.process.pid, signal.SIGKILL)
                    self.process.wait()
                if self.active_id and self.store.job(self.active_id)["state"] not in TERMINAL:
                    self.store.transition(self.active_id, "Failed", "Scheduler failed; inspect worker logs.")

    def _launch(self, job):
        job_id = job["id"]
        root = safe_path(self.settings.data, "jobs", job_id)
        root.mkdir(exist_ok=True)
        spec = self._spec(job["request"])
        spec.update(jobId=job_id, outputPath=str(root / "output"))
        atomic_write(root / "spec.json", json.dumps(spec).encode())
        self.active_id = job_id
        self.process = None
        self.stop_state = self.stop_error = self.cancel_deadline = None
        # The child observes EOF if this API process dies, even on macOS.
        self.parent_read, self.parent_write = os.pipe()
        try:
            self.process = subprocess.Popen(
                [sys.executable, "-m", "worker.runner", "--spec", str(root / "spec.json"),
                 "--parent-fd", str(self.parent_read)],
                cwd=PACKAGE_ROOT, env=self._environment(), stdin=subprocess.DEVNULL,
                stdout=subprocess.PIPE, stderr=subprocess.STDOUT, start_new_session=True,
                pass_fds=(self.parent_read,),
            )
        except OSError as exc:
            os.close(self.parent_read)
            os.close(self.parent_write)
            self.active_id = None
            self.store.transition(job_id, "Failed", f"Could not start training child: {exc}")
            return
        os.close(self.parent_read)
        self.store.transition(job_id, "Running")

    def _read_output(self, job_id, stream):
        try:
            while True:
                line = stream.readline(16385)
                if not line:
                    return
                if len(line) > 16384:
                    self.store.event(job_id, "warning", "Oversized child output was discarded.")
                    while line and not line.endswith(b"\n"):
                        line = stream.readline(16385)
                    continue
                text = line.decode("utf-8", errors="replace").strip()
                if not text:
                    continue
                try:
                    event = json.loads(text)
                    if not isinstance(event, dict) or not isinstance(event.get("metrics", {}), dict):
                        raise ValueError("Invalid child event.")
                    self.store.event(job_id, event["kind"], str(event.get("message", "")), event.get("metrics", {}))
                except (ValueError, KeyError, TypeError):
                    self.store.event(job_id, "log", text[:8192])
        except Exception:
            LOG.exception("Child event reader failed for %s", job_id)
            with self.lock:
                self._stop("Failed", "Child event persistence failed; inspect worker logs.")
        finally:
            stream.close()

    def _monitor(self, job):
        if self.process is None:
            return
        job_id, process = job["id"], self.process
        reader = threading.Thread(target=self._read_output, args=(job_id, process.stdout), daemon=True)
        reader.start()
        telemetry = Telemetry(process, self.settings.data)
        started, last_sample = time.monotonic(), 0
        warned = set()
        last_disk_check = 0
        while process.poll() is None:
            now = time.monotonic()
            with self.lock:
                if self.stopping:
                    self._stop("Interrupted", "Worker shut down; checkpoint saved if the child reached a safe boundary.")
                if now - started >= job["request"]["maxDurationMinutes"] * 60:
                    self._stop("Failed", "Training exceeded maxDurationMinutes; checkpoint saved if possible.")
                if self.cancel_deadline is not None and now >= self.cancel_deadline:
                    try:
                        os.killpg(process.pid, signal.SIGKILL)
                    except ProcessLookupError:
                        pass
                    self.cancel_deadline = None
                    self.store.event(job_id, "warning", "Grace period expired; process tree killed. Only previously completed checkpoints are usable.")
            if now - last_sample >= self.settings.telemetry_seconds:
                values, unavailable = telemetry.sample()
                if now - last_disk_check >= 10:
                    output = safe_path(self.settings.data, "jobs", job_id, "output")
                    used = sum(path.stat().st_size for path in output.rglob("*") if path.is_file())
                    values["jobOutputBytes"] = used
                    free = shutil.disk_usage(self.settings.data).free
                    if used >= self.settings.max_job_bytes or free < self.settings.min_free_bytes:
                        with self.lock:
                            self._stop("Failed", "Training exceeded the disk budget or minimum free-space reserve.")
                    elif used >= self.settings.max_job_bytes * 0.8 and "disk-budget" not in warned:
                        self.store.event(job_id, "warning", "Training output exceeds 80% of the operator disk budget.")
                        warned.add("disk-budget")
                    last_disk_check = now
                self.store.event(job_id, "metrics", "Process telemetry (CPU percent may exceed 100 on multicore; GPU values are device-wide).", values)
                for key, reason in unavailable.items():
                    if key not in warned:
                        self.store.event(job_id, "warning", json.dumps({"telemetryUnavailable": key, "reason": reason}))
                        warned.add(key)
                last_sample = now
            time.sleep(0.1)
        process.wait()
        # Terminate descendants that outlive their session leader, not just the parent PID.
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        reader.join(timeout=5)
        telemetry.close()
        os.close(self.parent_write)
        with self.lock:
            state = self.stop_state or ("Succeeded" if process.returncode == 0 else "Failed")
            error = self.stop_error
            if state == "Failed" and not error:
                error = f"Training child exited with code {process.returncode}; see retained error/log events."
            try:
                self._publish(job_id)
            except (OSError, ValueError, zipfile.BadZipFile, WorkerError) as exc:
                LOG.exception("Artifact publication failed for %s", job_id)
                state, error = "Failed", f"Artifact publication failed: {exc}"
            self.store.transition(job_id, state, error)
            self.process = None
            self.active_id = None

    def _publish(self, job_id):
        root = safe_path(self.settings.data, "jobs", job_id)
        output = root / "output"
        if not output.is_dir():
            return
        artifact_root = safe_path(root, "artifacts")
        artifact_root.mkdir(exist_ok=True)
        archive = artifact_root / "training-export.zip"
        if not archive.exists():
            output_size = sum(path.stat().st_size for path in output.rglob("*") if path.is_file())
            if shutil.disk_usage(self.settings.data).free < output_size + self.settings.min_free_bytes:
                raise WorkerError("Insufficient free space to archive training outputs; raw checkpoints remain on disk.", 507)
            temporary = archive.with_suffix(".tmp")
            with zipfile.ZipFile(temporary, "w", compression=zipfile.ZIP_DEFLATED) as bundle:
                for path in sorted(output.rglob("*")):
                    if path.is_symlink():
                        raise WorkerError("Training output must not contain symlinks.", 409)
                    if path.is_file() and not any(part.endswith(".tmp") for part in path.relative_to(output).parts):
                        bundle.write(path, path.relative_to(output).as_posix())
            with temporary.open("rb") as stream:
                os.fsync(stream.fileno())
            os.replace(temporary, archive)
            archive.chmod(0o440)
        self.store.register_artifact(job_id, archive)
        for name in ("manifest.json", "metrics.jsonl"):
            source, destination = output / name, artifact_root / name
            if source.is_file() and not destination.exists():
                shutil.copyfile(source, destination)
                destination.chmod(0o440)
                self.store.register_artifact(job_id, destination)

    def close(self):
        with self.lock:
            self.stopping = True
            self.wake.set()
        self.thread.join(timeout=self.settings.cancel_grace_seconds + 10)
        if self.thread.is_alive():
            raise RuntimeError("Training scheduler failed to stop safely; refusing to release instance lock.")
        self.store.close()
        self.instance_lock.close()

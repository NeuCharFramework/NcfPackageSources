"""Run a temporary native worker and tear it down even when validation fails."""

import argparse
import http.client
import os
import signal
import socket
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request
from pathlib import Path

from tests.make_tiny_model import create
from tests.smoke import run


def main(backend, dotnet_project=None):
    if dotnet_project is not None and backend != "cpu":
        raise ValueError("The .NET live integration test requires the CPU tiny-gpt2 fixture.")
    with tempfile.TemporaryDirectory(prefix="ncf-training-smoke-") as temporary:
        root = Path(temporary)
        models = root / "models"
        model_id = create(models, backend)
        with socket.socket() as listener:
            listener.bind(("127.0.0.1", 0))
            port = listener.getsockname()[1]
        key = "ncf-local-test-key-not-a-production-secret"
        env = {**os.environ, "NCF_WORKER_KEY": key, "NCF_WORKER_PORT": str(port),
               "NCF_BIND_HOST": "127.0.0.1", "NCF_MODELS_ROOT": str(models),
               "NCF_DATA_ROOT": str(root / "data"), "NCF_CANCEL_GRACE_SECONDS": "10"}
        endpoint = f"http://127.0.0.1:{port}"
        os.environ["NCF_WORKER_KEY"] = key
        with (root / "worker.log").open("w+") as log:
            process = subprocess.Popen([sys.executable, "-m", "worker"], env=env, stdout=log, stderr=log,
                                       start_new_session=True)
            try:
                deadline = time.monotonic() + 150
                while time.monotonic() < deadline:
                    if process.poll() is not None:
                        raise AssertionError("Worker exited before readiness.")
                    try:
                        request = urllib.request.Request(endpoint + "/health", headers={"X-NCF-Worker-Key": key})
                        with urllib.request.urlopen(request, timeout=3):
                            break
                    except (urllib.error.URLError, TimeoutError, http.client.RemoteDisconnected, ConnectionResetError):
                        time.sleep(0.25)
                else:
                    raise AssertionError("Worker did not become responsive.")
                run(endpoint, backend, model_id, models_root=models)
                if backend == "mlx":
                    run(endpoint, backend, model_id, "qlora", models_root=models)
                if dotnet_project is not None:
                    subprocess.run(
                        ["dotnet", "test", str(Path(dotnet_project).absolute()), "--no-restore",
                         "--filter", "TestCategory=FineTuningIntegration", "--verbosity", "quiet"],
                        env={**os.environ, "NCF_TEST_WORKER_ENDPOINT": endpoint, "NCF_TEST_WORKER_KEY": key},
                        check=True,
                    )
            except BaseException:
                log.flush()
                log.seek(0)
                print(log.read()[-10000:], file=sys.stderr)
                raise
            finally:
                os.killpg(process.pid, signal.SIGTERM) if process.poll() is None else None
                try:
                    process.wait(timeout=30)
                except subprocess.TimeoutExpired:
                    os.killpg(process.pid, signal.SIGKILL)
                    process.wait()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--backend", choices=["cpu", "mlx"], default="mlx")
    parser.add_argument("--dotnet-project")
    args = parser.parse_args()
    main(args.backend, args.dotnet_project)

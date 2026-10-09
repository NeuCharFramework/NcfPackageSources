import hashlib
import json
import math
import os
import shutil
import sqlite3
import threading
import uuid
from contextlib import contextmanager
from datetime import datetime, timezone

from .config import WorkerError, safe_path
from .datasets import validate_dataset

TERMINAL = {"Cancelled", "Succeeded", "Failed", "Interrupted"}


def utc_now():
    return datetime.now(timezone.utc).isoformat(timespec="milliseconds").replace("+00:00", "Z")


def atomic_write(path, content):
    temporary = path.with_name(path.name + "." + uuid.uuid4().hex + ".tmp")
    with open(temporary, "xb") as stream:
        stream.write(content)
        stream.flush()
        os.fsync(stream.fileno())
    os.replace(temporary, path)
    directory = os.open(path.parent, os.O_RDONLY)
    try:
        os.fsync(directory)
    finally:
        os.close(directory)


class Store:
    def __init__(self, settings):
        self.settings = settings
        self.root = settings.data
        self.root.mkdir(parents=True, exist_ok=True)
        for name in ("datasets", "jobs"):
            safe_path(self.root, name).mkdir(exist_ok=True)
        self.lock = threading.RLock()
        self.db = sqlite3.connect(safe_path(self.root, "worker.sqlite3"), check_same_thread=False, isolation_level=None)
        self.db.row_factory = sqlite3.Row
        self.db.executescript("""
            PRAGMA journal_mode=WAL;
            PRAGMA synchronous=FULL;
            PRAGMA foreign_keys=ON;
            CREATE TABLE IF NOT EXISTS datasets (
                id TEXT PRIMARY KEY, name TEXT NOT NULL, rows INTEGER NOT NULL,
                sha256 TEXT NOT NULL, createdUtc TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS jobs (
                id TEXT PRIMARY KEY, name TEXT NOT NULL, state TEXT NOT NULL,
                createdUtc TEXT NOT NULL, startedUtc TEXT, finishedUtc TEXT,
                request TEXT NOT NULL, error TEXT, latestMetrics TEXT NOT NULL DEFAULT '{}',
                lastEventSequence INTEGER NOT NULL DEFAULT 0);
            CREATE TABLE IF NOT EXISTS events (
                jobId TEXT NOT NULL REFERENCES jobs(id), sequence INTEGER NOT NULL,
                timestampUtc TEXT NOT NULL, kind TEXT NOT NULL, message TEXT,
                metrics TEXT NOT NULL, PRIMARY KEY(jobId, sequence));
            CREATE TABLE IF NOT EXISTS artifacts (
                id TEXT PRIMARY KEY, jobId TEXT NOT NULL REFERENCES jobs(id),
                name TEXT NOT NULL, bytes INTEGER NOT NULL, sha256 TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS job_order ON jobs(createdUtc);
            CREATE UNIQUE INDEX IF NOT EXISTS artifact_name ON artifacts(jobId,name);
            CREATE TABLE IF NOT EXISTS worker_metadata (name TEXT PRIMARY KEY, value TEXT NOT NULL);
        """)
        self.db.execute("INSERT OR IGNORE INTO worker_metadata VALUES ('storeId', ?)", (uuid.uuid4().hex,))
        self.store_id = self.db.execute("SELECT value FROM worker_metadata WHERE name='storeId'").fetchone()[0]

    @contextmanager
    def transaction(self):
        with self.lock:
            self.db.execute("BEGIN IMMEDIATE")
            try:
                yield
                self.db.execute("COMMIT")
            except BaseException:
                self.db.execute("ROLLBACK")
                raise

    def close(self):
        with self.lock:
            self.db.close()

    def dataset(self, dataset_id):
        with self.lock:
            row = self.db.execute("SELECT * FROM datasets WHERE id=?", (dataset_id,)).fetchone()
        if row is None:
            raise WorkerError("Dataset not found.", 404)
        return dict(row)

    def datasets(self):
        return self.datasets_page()["items"]

    def datasets_page(self, offset=0, limit=100):
        return self._catalog_page("datasets", offset, limit)

    def _catalog_page(self, table, offset, limit):
        if table not in {"datasets", "jobs"}:
            raise WorkerError("Unknown worker catalog.")
        if (type(offset) is not int or not 0 <= offset <= 100000
                or type(limit) is not int or not 1 <= limit <= 200):
            raise WorkerError("Catalog offset must be 0..100000 and limit must be 1..200.")
        with self.lock:
            total = self.db.execute(f"SELECT COUNT(*) FROM {table}").fetchone()[0]
            rows = self.db.execute(
                f"SELECT * FROM {table} ORDER BY createdUtc DESC,rowid DESC LIMIT ? OFFSET ?", (limit, offset),
            ).fetchall()
            items = [dict(row) for row in rows] if table == "datasets" else [self.job(row["id"]) for row in rows]
            return dict(items=items, total=total, offset=offset, limit=limit)

    def upload(self, name, content):
        rows, canonical, sha256 = validate_dataset(content)
        dataset = dict(id=uuid.uuid4().hex, name=name, rows=len(rows), sha256=sha256, createdUtc=utc_now())
        path = safe_path(self.root, "datasets", dataset["id"] + ".jsonl")
        with self.transaction():
            if self.db.execute("SELECT COUNT(*) FROM datasets").fetchone()[0] >= self.settings.max_datasets:
                raise WorkerError("Dataset retention limit reached; archive and rotate the worker store.", 429)
            if shutil.disk_usage(self.root).free < self.settings.min_free_bytes + len(canonical.encode("utf-8")):
                raise WorkerError("Insufficient free disk space for dataset upload.", 507)
            atomic_write(path, canonical.encode("utf-8"))
            self.db.execute("INSERT INTO datasets VALUES (:id,:name,:rows,:sha256,:createdUtc)", dataset)
        return dataset

    def read_dataset(self, dataset_id):
        metadata = self.dataset(dataset_id)
        path = safe_path(self.root, "datasets", dataset_id + ".jsonl")
        try:
            content = path.read_bytes()
        except FileNotFoundError as exc:
            raise WorkerError("Stored dataset file is missing.", 409) from exc
        if hashlib.sha256(content).hexdigest() != metadata["sha256"]:
            raise WorkerError("Stored dataset integrity check failed.", 409)
        rows, _, _ = validate_dataset(content.decode("utf-8"))
        return rows

    def create_job(self, request):
        with self.transaction():
            if self.db.execute("SELECT COUNT(*) FROM jobs").fetchone()[0] >= self.settings.max_jobs:
                raise WorkerError("Job retention limit reached; archive and rotate the worker store.", 429)
            queued = self.db.execute("SELECT COUNT(*) FROM jobs WHERE state='Queued'").fetchone()[0]
            if queued >= self.settings.queue_limit:
                raise WorkerError("Worker queue is full; retry after a job finishes.", 429)
            job_id = uuid.uuid4().hex
            self.db.execute(
                "INSERT INTO jobs(id,name,state,createdUtc,request) VALUES (?,?,'Queued',?,?)",
                (job_id, request["name"], utc_now(), json.dumps(request)),
            )
            self._event(job_id, "status", "Queued", {})
            return self.job(job_id)

    def job(self, job_id):
        with self.lock:
            row = self.db.execute("SELECT * FROM jobs WHERE id=?", (job_id,)).fetchone()
            if row is None:
                raise WorkerError("Job not found.", 404)
            job = dict(row)
            job["request"] = json.loads(job["request"])
            job["latestMetrics"] = json.loads(job["latestMetrics"])
            job["artifacts"] = [
                dict(item) for item in self.db.execute(
                    "SELECT id,name,bytes FROM artifacts WHERE jobId=? ORDER BY name", (job_id,)
                )
            ]
            return job

    def jobs(self):
        return self.jobs_page()["items"]

    def jobs_page(self, offset=0, limit=100):
        return self._catalog_page("jobs", offset, limit)

    def unfinished(self):
        with self.lock:
            return [self.job(row[0]) for row in self.db.execute(
                "SELECT id FROM jobs WHERE state NOT IN ('Cancelled','Succeeded','Failed','Interrupted') ORDER BY createdUtc,rowid"
            )]

    def counts(self):
        with self.lock:
            active = self.db.execute("SELECT COUNT(*) FROM jobs WHERE state IN ('Running','Cancelling')").fetchone()[0]
            queued = self.db.execute("SELECT COUNT(*) FROM jobs WHERE state='Queued'").fetchone()[0]
            return active, queued

    def transition(self, job_id, state, error=None):
        with self.transaction():
            old = self.job(job_id)["state"]
            allowed = {
                "Queued": {"Running", "Cancelled", "Failed"},
                "Running": {"Cancelling", "Succeeded", "Failed", "Interrupted"},
                "Cancelling": {"Cancelled", "Failed", "Interrupted"},
            }
            if state not in allowed.get(old, set()):
                raise WorkerError(f"Invalid lifecycle transition {old} -> {state}.", 409)
            self.db.execute("UPDATE jobs SET state=?,error=? WHERE id=?", (state, error, job_id))
            if state == "Running":
                self.db.execute("UPDATE jobs SET startedUtc=? WHERE id=?", (utc_now(), job_id))
            if state in TERMINAL:
                self.db.execute("UPDATE jobs SET finishedUtc=? WHERE id=?", (utc_now(), job_id))
            self._event(job_id, "status", state, {})
            if error:
                self._event(job_id, "error" if state == "Failed" else "warning", error, {})
            return self.job(job_id)

    def event(self, job_id, kind, message=None, metrics=None):
        if kind not in {"status", "log", "metrics", "warning", "error"}:
            raise ValueError("Unknown event kind")
        values = {}
        for key, value in (metrics or {}).items():
            if not isinstance(key, str) or len(key) > 100:
                raise ValueError("Invalid metric name")
            if value is not None and (isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value)):
                raise ValueError("Metrics must be finite numbers or null.")
            values[key] = value
        if len(values) > 64:
            raise ValueError("Too many metric keys")
        with self.transaction():
            self._event(job_id, kind, (message or "")[:8192], values)

    def _event(self, job_id, kind, message, metrics):
        job = self.job(job_id)
        sequence = job["lastEventSequence"] + 1
        latest = job["latestMetrics"]
        latest.update(metrics)
        self.db.execute(
            "INSERT INTO events VALUES (?,?,?,?,?,?)",
            (job_id, sequence, utc_now(), kind, message, json.dumps(metrics, allow_nan=False)),
        )
        self.db.execute(
            "UPDATE jobs SET lastEventSequence=?,latestMetrics=? WHERE id=?",
            (sequence, json.dumps(latest, allow_nan=False), job_id),
        )
        self.db.execute(
            "DELETE FROM events WHERE jobId=? AND sequence<=?",
            (job_id, sequence - self.settings.event_limit),
        )

    def events(self, job_id, after, limit):
        if after < 0 or not 1 <= limit <= 200:
            raise WorkerError("after must be nonnegative and limit between 1 and 200.")
        with self.lock:
            self.job(job_id)
            rows = list(self.db.execute(
                "SELECT sequence,timestampUtc,kind,message,metrics FROM events WHERE jobId=? AND sequence>? ORDER BY sequence LIMIT ?",
                (job_id, after, limit + 1),
            ))
            events = [dict(row) for row in rows[:limit]]
            for event in events:
                event["metrics"] = json.loads(event["metrics"])
            first = self.db.execute("SELECT MIN(sequence) FROM events WHERE jobId=?", (job_id,)).fetchone()[0]
            return dict(events=events, nextCursor=events[-1]["sequence"] if events else after,
                        hasMore=len(rows) > limit, truncated=first is not None and after < first - 1)

    def register_artifact(self, job_id, path):
        if path.is_symlink() or not path.is_file():
            raise WorkerError("Artifact must be a regular immutable file.", 409)
        with path.open("rb") as stream:
            digest = hashlib.file_digest(stream, "sha256").hexdigest()
        with self.transaction():
            self.db.execute(
                "INSERT OR IGNORE INTO artifacts VALUES (?,?,?,?,?)",
                (uuid.uuid4().hex, job_id, path.name, path.stat().st_size, digest),
            )

    def artifact(self, job_id, artifact_id):
        with self.lock:
            self.job(job_id)
            row = self.db.execute("SELECT * FROM artifacts WHERE jobId=? AND id=?", (job_id, artifact_id)).fetchone()
        if row is None:
            raise WorkerError("Artifact not found.", 404)
        path = safe_path(self.root, "jobs", job_id, "artifacts", row["name"])
        if not path.is_file() or path.stat().st_size != row["bytes"]:
            raise WorkerError("Artifact is missing or changed.", 409)
        with path.open("rb") as stream:
            if hashlib.file_digest(stream, "sha256").hexdigest() != row["sha256"]:
                raise WorkerError("Artifact integrity check failed.", 409)
        return path, row["name"]

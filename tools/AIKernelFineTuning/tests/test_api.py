import asyncio
import json
import tempfile
import unittest
from pathlib import Path

from fastapi.testclient import TestClient

from worker.api import MAX_BODY_BYTES, AuthAndSize, create_app
from worker.config import MAX_DATASET_BYTES, MAX_DATASET_ROWS, Settings
from worker.service import Worker
from worker.storage import Store
from tests.test_core import CONTENT, REQUEST


class FixtureWorker:
    def __init__(self, settings):
        self.settings = settings
        self.store = Store(settings)

    def close(self):
        self.store.close()

    def health(self):
        return {"status": "ok", "version": "fixture", "activeJobs": 0, "queuedJobs": 0, "capabilities": []}

    def models(self):
        return []

    def create_job(self, request):
        return self.store.create_job(request)

    def cancel(self, job_id):
        return self.store.transition(job_id, "Cancelled")

    validation = Worker.validation
    start_validation = Worker.start_validation


class ApiTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        root = Path(self.temp.name)
        self.settings = Settings("k" * 32, root / "models", root / "data", min_free_bytes=1024**2)
        self.client = TestClient(create_app(self.settings, FixtureWorker))
        self.client.__enter__()
        self.headers = {"X-NCF-Worker-Key": self.settings.key}

    def tearDown(self):
        self.client.__exit__(None, None, None)
        self.temp.cleanup()

    def test_every_endpoint_requires_authentication(self):
        for method, path in [("GET", "/health"), ("GET", "/models"), ("GET", "/datasets"), ("POST", "/datasets"),
                             ("POST", "/datasets/file?name=raw"),
                             ("GET", "/jobs"), ("POST", "/jobs"), ("GET", "/jobs/id"), ("POST", "/jobs/id/cancel"),
                             ("GET", "/jobs/id/events"), ("GET", "/jobs/id/artifacts/id"),
                             ("GET", "/jobs/id/validation"), ("POST", "/jobs/id/validation"),
                             ("GET", "/jobs/page"), ("GET", "/datasets/page"), ("GET", "/datasets/id")]:
            self.assertEqual(self.client.request(method, path).status_code, 401)
        self.assertEqual(self.client.get("/health", headers=self.headers).status_code, 200)
        self.assertEqual(self.client.get("/health", headers={"X-NCF-Worker-Key": "wrong"}).status_code, 401)

    def test_streaming_jsonl_upload_preserves_worker_contract(self):
        self.assertEqual(self.settings.max_dataset_bytes, 512 * 1024 * 1024)
        self.assertEqual(self.settings.max_dataset_rows, 1_000_000)
        self.assertEqual((MAX_DATASET_BYTES, MAX_DATASET_ROWS), (512 * 1024 * 1024, 1_000_000))
        response = self.client.post("/datasets/file?name=raw", headers=self.headers,
                                    content=CONTENT.encode("utf-8"))
        self.assertEqual(response.status_code, 200)
        dataset = response.json()
        self.assertEqual(dataset["rows"], 6)
        self.assertEqual(len(dataset["sha256"]), 64)
        self.assertEqual(self.client.get(f"/datasets/{dataset['id']}", headers=self.headers).json(), dataset)

    def test_validation_contract_requires_successful_job(self):
        dataset = self.client.post("/datasets", headers=self.headers, json={"name": "test", "content": CONTENT}).json()
        job = self.client.post("/jobs", headers=self.headers, json={**REQUEST, "datasetId": dataset["id"]}).json()
        path = f"/jobs/{job['id']}/validation"
        response = self.client.get(path, headers=self.headers)
        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json(), {"status": "Idle", "startedUtc": None, "finishedUtc": None,
                                           "completed": 0, "total": 0, "results": [], "error": None,
                                           "recordedBackend": REQUEST["backend"], "recordedMethod": REQUEST["method"]})
        response = self.client.post(path, headers=self.headers, json={"cases": [], "maxTokens": 128})
        self.assertEqual(response.status_code, 422)
        response = self.client.post(path, headers=self.headers,
                                    json={"cases": [{"id": "case1", "prompt": "hello"}], "maxTokens": 128})
        self.assertEqual(response.status_code, 409)

    def test_dataset_file_stream_bypasses_json_body_limit(self):
        asyncio.run(self._assert_size_gate("/datasets/file", MAX_BODY_BYTES + 1, passed=True))
        asyncio.run(self._assert_size_gate("/datasets", MAX_BODY_BYTES + 1, passed=False,
                                            detail="Request body exceeds the worker limit."))
        asyncio.run(self._assert_size_gate(
            "/datasets/file", self.settings.max_dataset_bytes + 1, passed=False,
            detail="Dataset body exceeds the worker limit."))

    def test_gateway_streams_dataset_files_above_json_limit(self):
        conf = Path(__file__).resolve().parents[1].joinpath("nginx.conf").read_text()
        file_location = conf.split("location = /datasets/file", 1)[1].split("location /", 1)[0]
        self.assertIn("client_max_body_size 13m;", conf)
        self.assertIn("client_max_body_size 512m;", file_location)
        self.assertIn("proxy_request_buffering off;", file_location)

    async def _assert_size_gate(self, path, length, passed, detail=None):
        called = []

        async def app(scope, receive, send):
            called.append(scope["path"])
            await send({"type": "http.response.start", "status": 204, "headers": []})
            await send({"type": "http.response.body", "body": b""})

        middleware = AuthAndSize(app, self.settings.key, self.settings.max_dataset_bytes)
        scope = {
            "type": "http",
            "method": "POST",
            "path": path,
            "headers": [
                (b"x-ncf-worker-key", self.settings.key.encode()),
                (b"content-length", str(length).encode()),
            ],
        }
        messages = []

        async def receive():
            return {"type": "http.request", "body": b"", "more_body": False}

        async def send(message):
            messages.append(message)

        await middleware(scope, receive, send)
        if passed:
            self.assertEqual(called, [path])
            self.assertEqual(messages[0]["status"], 204)
            return
        self.assertEqual(called, [])
        self.assertEqual(messages[0]["status"], 413)
        body = b"".join(message.get("body", b"") for message in messages if message["type"] == "http.response.body")
        self.assertEqual(json.loads(body), {"detail": detail})

    def test_body_limit_validation_never_echoes_data(self):
        response = self.client.post("/datasets", headers={**self.headers, "Content-Length": str(MAX_BODY_BYTES + 1)}, content="{}")
        self.assertEqual(response.status_code, 413)
        response = self.client.post("/datasets", headers=self.headers, json={"name": "test", "content": "SECRET", "extra": True})
        self.assertEqual(response.status_code, 422)
        self.assertNotIn("SECRET", response.text)
        response = self.client.post("/jobs", headers=self.headers, content='{"name":"x","learningRate":NaN}')
        self.assertEqual(response.status_code, 422)

    def test_upload_job_events_cancellation_contract(self):
        dataset = self.client.post("/datasets", headers=self.headers, json={"name": "test", "content": CONTENT}).json()
        self.assertEqual(dataset["rows"], 6)
        self.assertEqual(len(dataset["sha256"]), 64)
        job = self.client.post("/jobs", headers=self.headers, json={**REQUEST, "datasetId": dataset["id"]}).json()
        self.assertEqual(job["state"], "Queued")
        events = self.client.get(f"/jobs/{job['id']}/events", headers=self.headers).json()
        self.assertEqual(events["nextCursor"], 1)
        self.assertIn("timestampUtc", events["events"][0])
        self.assertEqual(self.client.get(f"/jobs/{job['id']}/events?limit=201", headers=self.headers).status_code, 422)
        cancelled = self.client.post(f"/jobs/{job['id']}/cancel", headers=self.headers, json={}).json()
        self.assertEqual(cancelled["state"], "Cancelled")
        self.assertIsNotNone(cancelled["finishedUtc"])
        self.assertEqual(self.client.get("/jobs/unknown", headers=self.headers).status_code, 404)

    def test_catalog_pages_and_dataset_lookup_preserve_contract_and_bounds(self):
        dataset = self.client.post("/datasets", headers=self.headers, json={"name": "test", "content": CONTENT}).json()
        job = self.client.post("/jobs", headers=self.headers, json={**REQUEST, "datasetId": dataset["id"]}).json()
        for catalog, record in [("datasets", dataset), ("jobs", job)]:
            page = self.client.get(f"/{catalog}/page?offset=0&limit=1", headers=self.headers).json()
            self.assertEqual(page, {"items": [record], "offset": 0, "limit": 1, "total": 1})
            self.assertEqual(self.client.get(f"/{catalog}/page?offset=1", headers=self.headers).json()["items"], [])
            for query in ["offset=-1", "offset=100001", "limit=0", "limit=201"]:
                self.assertEqual(self.client.get(f"/{catalog}/page?{query}", headers=self.headers).status_code, 422)
        self.assertEqual(self.client.get(f"/datasets/{dataset['id']}", headers=self.headers).json(), dataset)
        self.assertEqual(self.client.get("/datasets/missing", headers=self.headers).status_code, 404)

if __name__ == "__main__":
    unittest.main()

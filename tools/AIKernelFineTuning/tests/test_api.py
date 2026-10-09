import json
import tempfile
import unittest
from pathlib import Path

from fastapi.testclient import TestClient

from worker.api import MAX_BODY_BYTES, create_app
from worker.config import Settings
from worker.storage import Store
from tests.test_core import CONTENT, REQUEST


class FixtureWorker:
    def __init__(self, settings):
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
                             ("GET", "/jobs"), ("POST", "/jobs"), ("GET", "/jobs/id"), ("POST", "/jobs/id/cancel"),
                             ("GET", "/jobs/id/events"), ("GET", "/jobs/id/artifacts/id"),
                             ("GET", "/jobs/page"), ("GET", "/datasets/page"), ("GET", "/datasets/id")]:
            self.assertEqual(self.client.request(method, path).status_code, 401)
        self.assertEqual(self.client.get("/health", headers=self.headers).status_code, 200)
        self.assertEqual(self.client.get("/health", headers={"X-NCF-Worker-Key": "wrong"}).status_code, 401)

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

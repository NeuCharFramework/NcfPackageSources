import hashlib
import json
import math
import tempfile
import threading
import unittest
from dataclasses import replace
from pathlib import Path
from unittest.mock import patch

from worker.config import MAX_DATASET_BYTES, Settings, WorkerError, identifier, safe_path
from worker.datasets import encode_row, parse_json, split_rows, validate_dataset
from worker.models import list_models, validate_model_request
from worker.runner import batch_groups, schedule_rate
from worker.storage import Store, atomic_write

ROWS = [{"prompt": f"Question {index}", "completion": f"Answer {index}"} for index in range(6)]
CONTENT = "".join(json.dumps(row) + "\n" for row in ROWS)
REQUEST = dict(name="tiny test", modelId="tiny", datasetId="dataset", evalDatasetId=None, backend="cpu",
               method="lora", epochs=1, maxSteps=10, learningRate=0.0002, batchSize=1,
               gradientAccumulationSteps=1, maxSequenceLength=32, loraRank=8, loraAlpha=16,
               loraDropout=0.05, targetModules="all-linear", warmupRatio=0.03, weightDecay=0,
               loggingSteps=1, saveSteps=20, evalSteps=20, seed=42, maxDurationMinutes=60)

def maximum_dataset():
    rows = [dict(prompt="x" * 65536, completion="y") for _ in range(31)]
    rows.append(dict(prompt="", completion="y"))
    encode = lambda values: "\n".join(json.dumps(row, separators=(",", ":")) for row in values)
    rows[-1]["prompt"] = "x" * (MAX_DATASET_BYTES - len(encode(rows).encode("utf-8")))
    return encode(rows)


class DatasetTests(unittest.TestCase):
    def test_both_formats_and_canonical_hash(self):
        messages = {"messages": [{"role": "system", "content": "Helpful"},
                                  {"role": "user", "content": "Hello"}, {"role": "assistant", "content": "Hi"}]}
        content = json.dumps(messages) + "\n" + json.dumps(ROWS[0])
        rows, canonical, digest = validate_dataset(content)
        self.assertEqual(len(rows), 2)
        self.assertEqual(digest, hashlib.sha256(canonical.encode()).hexdigest())
        self.assertEqual(validate_dataset(canonical)[1], canonical)

    def test_strict_row_errors(self):
        for row in [dict(prompt="x", completion="y", extra=True), dict(prompt=" ", completion="y"),
                    {"messages": [{"role": "tool", "content": "x"}]}, {"messages": [{"role": "user", "content": "x"},
                     {"role": "system", "content": "y"}]}, dict(prompt=3, completion="y")]:
            with self.subTest(row=row), self.assertRaises(WorkerError) as ctx:
                validate_dataset(json.dumps(ROWS[0]) + "\n" + json.dumps(row))
            self.assertEqual(ctx.exception.details[0]["row"], 2)
        with self.assertRaises(WorkerError) as ctx:
            validate_dataset("\nnot json\n")
        self.assertEqual(ctx.exception.details[0]["row"], 1)

    def test_size_and_minimum_limits(self):
        with self.assertRaises(WorkerError):
            validate_dataset(json.dumps(ROWS[0]))
        with self.assertRaises(WorkerError) as ctx:
            validate_dataset("x" * (MAX_DATASET_BYTES + 1))
        self.assertEqual(ctx.exception.status, 413)

    def test_exact_byte_limit_remains_readable_after_canonicalization(self):
        content = maximum_dataset()
        self.assertEqual(len(content.encode("utf-8")), MAX_DATASET_BYTES)
        rows, canonical, _ = validate_dataset(content)
        self.assertEqual(len(rows), 32)
        self.assertLessEqual(len(canonical.encode("utf-8")), MAX_DATASET_BYTES)
        self.assertEqual(validate_dataset(canonical)[0], rows)
        with self.assertRaises(WorkerError):
            validate_dataset(content + "\n")

    def test_duplicate_properties_nonfinite_rejected(self):
        for value in ['{"a":1,"a":2}', '{"a":NaN}', '{"a":Infinity}']:
            with self.assertRaises(ValueError):
                parse_json(value)

    def test_group_split_is_disjoint_and_deterministic(self):
        first = split_rows(ROWS + [ROWS[0]], None, 42)
        self.assertEqual(first, split_rows(ROWS + [ROWS[0]], None, 42))
        self.assertFalse({json.dumps(row) for row in first[0]} & {json.dumps(row) for row in first[1]})
        with self.assertRaises(WorkerError):
            split_rows([ROWS[0], ROWS[0]], None, 42)
        with self.assertRaises(WorkerError):
            split_rows(ROWS, [ROWS[0]], 42)

    def test_conversation_requires_approved_chat_template(self):
        with self.assertRaises(WorkerError):
            encode_row(type("Tokenizer", (), {"chat_template": None})(),
                       {"messages": [{"role": "user", "content": "Hello"}, {"role": "assistant", "content": "Hi"}]}, 32)


class DurableStoreTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary.name)
        self.models = self.root / "models"
        self.models.mkdir()
        self.settings = Settings("k" * 32, self.models, self.root / "data", queue_limit=2, event_limit=200)
        self.store = Store(self.settings)

    def tearDown(self):
        self.store.close()
        self.temporary.cleanup()

    def test_upload_restart_and_integrity(self):
        dataset = self.store.upload("data", CONTENT)
        self.assertEqual(self.store.read_dataset(dataset["id"]), ROWS)
        self.store.close()
        self.store = Store(self.settings)
        self.assertEqual(self.store.datasets(), [dataset])
        path = safe_path(self.settings.data, "datasets", dataset["id"] + ".jsonl")
        path.write_text("corrupted")
        with self.assertRaises(WorkerError):
            self.store.read_dataset(dataset["id"])

    def test_exact_byte_limit_can_be_uploaded_and_read(self):
        dataset = self.store.upload("maximum", maximum_dataset())
        self.assertEqual(len(self.store.read_dataset(dataset["id"])), 32)

    def test_catalog_paging_reaches_records_beyond_the_legacy_first_100(self):
        datasets, jobs = [], []
        for index in range(101):
            datasets.append(self.store.upload(f"data-{index}", CONTENT)["id"])
            job = self.store.create_job({**REQUEST, "name": f"job-{index}"})
            jobs.append(job["id"])
            self.store.transition(job["id"], "Cancelled")
        for fetch, expected in [(self.store.datasets_page, datasets), (self.store.jobs_page, jobs)]:
            first, second = fetch(0, 100), fetch(100, 100)
            self.assertEqual(first["total"], 101)
            self.assertEqual(second["total"], 101)
            self.assertEqual(len(first["items"]), 100)
            self.assertEqual(len(second["items"]), 1)
            self.assertEqual({row["id"] for row in first["items"] + second["items"]}, set(expected))
            self.assertEqual(fetch(101, 10)["items"], [])
            for offset, limit in [(-1, 10), (100001, 10), (0, 0), (0, 201)]:
                with self.assertRaises(WorkerError):
                    fetch(offset, limit)
        self.assertEqual(len(self.store.datasets()), 100)
        self.assertEqual(len(self.store.jobs()), 100)

    def test_store_identity_survives_restart_and_differs_for_new_storage(self):
        original = self.store.store_id
        self.store.close()
        self.store = Store(self.settings)
        self.assertEqual(self.store.store_id, original)
        other = Store(replace(self.settings, data=self.root / "other-data"))
        try:
            self.assertNotEqual(other.store_id, original)
        finally:
            other.close()

    def test_lifecycle_queue_and_durable_metrics(self):
        first = self.store.create_job(REQUEST)
        self.store.create_job(REQUEST)
        with self.assertRaises(WorkerError) as ctx:
            self.store.create_job(REQUEST)
        self.assertEqual(ctx.exception.status, 429)
        self.store.transition(first["id"], "Running")
        self.store.event(first["id"], "metrics", metrics={"trainLoss": 1.25, "gpuMemoryBytes": None})
        self.store.transition(first["id"], "Cancelling")
        self.store.transition(first["id"], "Cancelled")
        with self.assertRaises(WorkerError):
            self.store.transition(first["id"], "Succeeded")
        job = self.store.job(first["id"])
        self.assertIsNotNone(job["startedUtc"])
        self.assertIsNotNone(job["finishedUtc"])
        self.assertEqual(job["latestMetrics"]["trainLoss"], 1.25)
        self.assertIsNone(job["latestMetrics"]["gpuMemoryBytes"])
        self.store.close()
        self.store = Store(self.settings)
        self.assertEqual(self.store.job(first["id"]), job)

    def test_atomic_queue_race(self):
        created, rejected = [], []

        def create():
            try:
                created.append(self.store.create_job(REQUEST))
            except WorkerError as exc:
                rejected.append(exc.status)
        threads = [threading.Thread(target=create) for _ in range(12)]
        for thread in threads:
            thread.start()
        for thread in threads:
            thread.join()
        self.assertEqual(len(created), 2)
        self.assertEqual(rejected, [429] * 10)

    def test_bounded_monotonic_event_pagination(self):
        job_id = self.store.create_job(REQUEST)["id"]
        for index in range(250):
            self.store.event(job_id, "metrics", metrics={"step": index})
        all_events, after = [], 0
        while True:
            page = self.store.events(job_id, after, 17)
            all_events += page["events"]
            after = page["nextCursor"]
            if not page["hasMore"]:
                break
        self.assertEqual(len(all_events), 200)
        self.assertEqual(all_events[0]["sequence"], 52)
        self.assertEqual(after, 251)
        self.assertTrue(self.store.events(job_id, 0, 17)["truncated"])
        self.assertFalse(self.store.events(job_id, 251, 17)["truncated"])
        self.assertEqual(self.store.job(job_id)["latestMetrics"]["step"], 249)
        with self.assertRaises(WorkerError):
            self.store.events(job_id, 0, 201)
        with self.assertRaises(ValueError):
            self.store.event(job_id, "metrics", metrics={"loss": math.nan})

    def test_operator_retention_and_space_budgets_reject_admission(self):
        self.store.settings = replace(self.settings, max_jobs=1, max_datasets=1)
        self.store.upload("first", CONTENT)
        with self.assertRaises(WorkerError) as dataset_error:
            self.store.upload("second", CONTENT)
        self.assertEqual(dataset_error.exception.status, 429)
        self.store.create_job(REQUEST)
        with self.assertRaises(WorkerError) as job_error:
            self.store.create_job(REQUEST)
        self.assertEqual(job_error.exception.status, 429)
        self.store.settings = self.settings
        with patch("worker.storage.shutil.disk_usage") as usage:
            usage.return_value.free = 0
            with self.assertRaises(WorkerError) as space_error:
                self.store.upload("no-space", CONTENT)
            self.assertEqual(space_error.exception.status, 507)

    def test_safe_immutable_artifacts(self):
        job_id = self.store.create_job(REQUEST)["id"]
        root = safe_path(self.settings.data, "jobs", job_id, "artifacts")
        root.mkdir(parents=True)
        path = root / "manifest.json"
        atomic_write(path, b"export")
        self.store.register_artifact(job_id, path)
        self.store.register_artifact(job_id, path)
        artifact = self.store.job(job_id)["artifacts"]
        self.assertEqual(len(artifact), 1)
        self.assertEqual(self.store.artifact(job_id, artifact[0]["id"])[0], path)
        path.write_bytes(b"change")
        with self.assertRaises(WorkerError):
            self.store.artifact(job_id, artifact[0]["id"])
        with self.assertRaises(WorkerError):
            self.store.artifact(job_id, "unknown")

    def test_model_identifiers_symlinks_and_remote_code(self):
        for value in ["../foo", "/tmp/model", ".", "a..b", "hello/world", "a%2Fb", "a;cmd"]:
            with self.assertRaises(WorkerError):
                identifier(value)
        tiny = self.models / "tiny"
        tiny.mkdir()
        (tiny / "config.json").write_text(json.dumps({"model_type": "gpt2", "n_positions": 64}))
        (tiny / "tokenizer_config.json").write_text("{}")
        (tiny / "model.safetensors").write_bytes(b"fixture")
        models, rejected = list_models(self.models)
        self.assertEqual(models, [dict(id="tiny", name="tiny", backends=["cpu", "cuda"])])
        self.assertFalse(rejected)
        with self.assertRaises(WorkerError):
            validate_model_request(self.models, {**REQUEST, "maxSequenceLength": 256})
        with self.assertRaises(WorkerError):
            validate_model_request(self.models, {**REQUEST, "method": "qlora"})
        (tiny / "escape").symlink_to(self.root)
        self.assertEqual(list_models(self.models)[0], [])
        (tiny / "escape").unlink()
        (tiny / "config.json").write_text('{"model_type":"gpt2","auto_map":{"AutoModel":"evil"}}')
        self.assertEqual(list_models(self.models)[0], [])


class LoopTests(unittest.TestCase):
    def test_epoch_and_gradient_accumulation_partial_groups(self):
        request = {**REQUEST, "maxSteps": 0, "epochs": 2, "batchSize": 2, "gradientAccumulationSteps": 2}
        groups = list(batch_groups(ROWS[:5], request))
        self.assertEqual(len(groups), 4)
        self.assertEqual(groups[-1][1], 2)
        self.assertEqual(sum(len(batch) for group, _ in groups for batch in group), 10)
        self.assertEqual(schedule_rate(REQUEST, 1, 10), REQUEST["learningRate"])
        self.assertGreater(schedule_rate(REQUEST, 10, 10), 0)


if __name__ == "__main__":
    unittest.main()

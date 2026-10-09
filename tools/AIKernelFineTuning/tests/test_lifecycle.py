import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from tests.test_core import REQUEST
from worker.config import Settings, WorkerError
from worker.service import Worker
from worker.storage import Store


class RecoveryTests(unittest.TestCase):
    def test_restart_marks_active_interrupted_and_enforces_single_store_owner(self):
        with tempfile.TemporaryDirectory() as temporary:
            settings = Settings("k" * 32, Path(temporary) / "models", Path(temporary) / "data")
            store = Store(settings)
            running = store.create_job(REQUEST)
            store.transition(running["id"], "Running")
            store.close()
            with patch.object(Worker, "_probe", return_value=[]):
                worker = Worker(settings)
                try:
                    job = worker.store.job(running["id"])
                    self.assertEqual(job["state"], "Interrupted")
                    self.assertIsNotNone(job["finishedUtc"])
                    self.assertIn("no automatic resume", job["error"])
                    with self.assertRaises(RuntimeError):
                        Worker(settings)
                finally:
                    worker.close()

    def test_operator_time_budget_and_stopping_state_are_explicit(self):
        with tempfile.TemporaryDirectory() as temporary:
            settings = Settings("k" * 32, Path(temporary) / "models", Path(temporary) / "data",
                                max_duration_minutes=1)
            with patch.object(Worker, "_probe", return_value=[]):
                worker = Worker(settings)
                try:
                    with self.assertRaises(WorkerError) as duration:
                        worker.create_job(REQUEST)
                    self.assertIn("operator budget", str(duration.exception))
                    worker.stopping = True
                    with self.assertRaises(WorkerError) as stopping:
                        worker.create_job({**REQUEST, "maxDurationMinutes": 1})
                    self.assertEqual(stopping.exception.status, 503)
                finally:
                    worker.close()

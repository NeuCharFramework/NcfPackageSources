import io
import json
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch

from tests.test_core import REQUEST
from worker.runner import STOP, train


class EarlyCancellationTests(unittest.TestCase):
    def test_cancel_during_initialization_does_not_export_success_shaped_metadata(self):
        class Tokenizer:
            pad_token_id = 0
            eos_token_id = 1

            def encode(self, text, add_special_tokens=True):
                return [2, 3]

            def save_pretrained(self, destination):
                destination.mkdir()
                (destination / "tokenizer.json").write_text("{}")

        tokenizer_module = SimpleNamespace(
            AutoTokenizer=SimpleNamespace(from_pretrained=lambda *args, **kwargs: Tokenizer()))
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            model = root / "models" / "tiny"
            model.mkdir(parents=True)
            dataset = root / "dataset.jsonl"
            dataset.write_text('{"prompt":"one","completion":"answer"}\n{"prompt":"two","completion":"answer"}')
            output = root / "output"
            spec = dict(request=REQUEST, outputPath=str(output), jobId="cancelled",
                        modelsRoot=str(root / "models"), datasetPath=str(dataset), evalDatasetPath=None)
            STOP.set()
            try:
                with patch.dict("sys.modules", {"transformers": tokenizer_module}), \
                        patch("worker.runner.validate_model_request", return_value=(model, {})), \
                        patch("worker.runner.importlib.metadata.version", return_value="test"), \
                        redirect_stdout(io.StringIO()):
                    self.assertEqual(train(spec), 143)
            finally:
                STOP.clear()
            manifest = json.loads((output / "manifest.json").read_text())
            self.assertEqual(manifest["status"], "cancelled")
            self.assertEqual(manifest["completedSteps"], 0)
            self.assertFalse((output / "adapter").exists())
            self.assertFalse((output / "checkpoints").exists())

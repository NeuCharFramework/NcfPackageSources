"""Exercise the real authenticated API, child training, evaluation and exports."""

import argparse
import io
import json
import os
import time
import urllib.error
import urllib.request
import zipfile
from pathlib import Path


def run(endpoint, backend, model_id, method="lora", models_root=None):
    key = os.environ["NCF_WORKER_KEY"]

    def api(path, body=None):
        request = urllib.request.Request(endpoint + path, headers={"X-NCF-Worker-Key": key})
        if body is not None:
            request.data = json.dumps(body).encode()
            request.add_header("Content-Type", "application/json")
        try:
            with urllib.request.urlopen(request, timeout=150) as response:
                return json.load(response)
        except urllib.error.HTTPError as exc:
            raise AssertionError(f"API HTTP {exc.code}: {exc.read().decode()}") from exc

    def wait(job_id, timeout=150):
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            job = api(f"/jobs/{job_id}")
            if job["state"] in {"Succeeded", "Cancelled", "Failed", "Interrupted"}:
                return job
            time.sleep(0.25)
        raise AssertionError(f"Job {job_id} did not terminate within {timeout}s.")

    health = api("/health")
    capability = next(cap for cap in health["capabilities"] if cap["backend"] == backend)
    assert capability["available"] and method in capability["methods"], capability
    assert any(model["id"] == model_id for model in api("/models"))
    dataset = api("/datasets", {
        "name": "Infrastructure smoke (random model, not quality evidence)",
        "content": "".join(json.dumps({"prompt": f"Question {index}", "completion": f"Answer {index}"}) + "\n" for index in range(8)),
    })
    request = {
        "name": f"{backend}-{method} infrastructure smoke", "modelId": model_id, "datasetId": dataset["id"],
        "backend": backend, "method": method, "maxSteps": 6, "maxSequenceLength": 32,
        "loraRank": 2, "loraAlpha": 4, "saveSteps": 2, "evalSteps": 2, "maxDurationMinutes": 5,
    }
    job = wait(api("/jobs", request)["id"])
    if job["state"] != "Succeeded":
        raise AssertionError({"job": job, "events": api(f"/jobs/{job['id']}/events?limit=200")})
    assert job["latestMetrics"]["step"] == 6 and job["latestMetrics"]["totalSteps"] == 6, job
    assert job["latestMetrics"]["evalLoss"] > 0
    events, cursor = [], 0
    while True:
        page = api(f"/jobs/{job['id']}/events?after={cursor}&limit=7")
        events.extend(page["events"])
        assert page["nextCursor"] >= cursor
        cursor = page["nextCursor"]
        if not page["hasMore"]:
            break
    assert any(event["kind"] == "metrics" and "trainLoss" in event["metrics"] for event in events)
    assert any(event["kind"] == "metrics" and "evalLoss" in event["metrics"] for event in events)
    artifact = next(item for item in job["artifacts"] if item["name"] == "training-export.zip")
    request_download = urllib.request.Request(
        f"{endpoint}/jobs/{job['id']}/artifacts/{artifact['id']}", headers={"X-NCF-Worker-Key": key})
    with urllib.request.urlopen(request_download, timeout=30) as response:
        payload = response.read()
    assert len(payload) == artifact["bytes"]
    with zipfile.ZipFile(io.BytesIO(payload)) as archive:
        names = archive.namelist()
        assert "manifest.json" in names and "metrics.jsonl" in names
        assert any(name.startswith("adapter/") and name.endswith(".safetensors") for name in names)
        manifest = json.loads(archive.read("manifest.json"))
        assert manifest["completedSteps"] == 6 and manifest["evaluationRows"] > 0
        assert manifest["baseModelSha256"]
        assert len({name.split("/")[1] for name in names if name.startswith("checkpoints/")}) == 2
    reload_result = {"adapterReloadVerified": False}
    if backend == "cpu" and models_root is not None:
        from tests.verify_export import verify_torch_export
        reload_result = verify_torch_export(payload, Path(models_root) / model_id)
    elif backend == "mlx" and models_root is not None:
        from tests.verify_export import verify_mlx_export
        reload_result = verify_mlx_export(payload, Path(models_root) / model_id)
    request.update(maxSteps=100000, name="Cancellation infrastructure smoke", saveSteps=1)
    active = api("/jobs", request)
    deadline = time.monotonic() + 60
    while api(f"/jobs/{active['id']}")["state"] == "Queued" and time.monotonic() < deadline:
        time.sleep(0.25)
    queued = api("/jobs", request)
    assert api(f"/jobs/{queued['id']}/cancel", {})["state"] == "Cancelled"
    api(f"/jobs/{active['id']}/cancel", {})
    cancelled = wait(active["id"])
    assert cancelled["state"] == "Cancelled", cancelled
    assert api("/health")["activeJobs"] == 0
    print(json.dumps({
        "backend": backend, "method": method, "jobId": job["id"], "state": job["state"], "steps": 6,
        "evalLoss": job["latestMetrics"]["evalLoss"], "eventCount": len(events), "artifactBytes": artifact["bytes"],
        "runningCancel": cancelled["state"], "queuedCancel": "Cancelled",
        **reload_result,
    }))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--endpoint", default="http://127.0.0.1:8091")
    parser.add_argument("--backend", choices=["cpu", "cuda", "mlx"], default="cpu")
    parser.add_argument("--model", default="tiny-gpt2")
    parser.add_argument("--method", choices=["lora", "qlora"], default="lora")
    parser.add_argument("--models-root", default=os.environ.get("NCF_MODELS_ROOT"))
    args = parser.parse_args()
    run(args.endpoint.rstrip("/"), args.backend, args.model, args.method, args.models_root)

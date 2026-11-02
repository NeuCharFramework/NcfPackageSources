import hmac
import json
import logging
import os
from contextlib import asynccontextmanager

from fastapi import Depends, FastAPI, Query, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import FileResponse, JSONResponse
from starlette.concurrency import run_in_threadpool
from starlette.types import ASGIApp, Receive, Scope, Send

from . import VERSION
from .config import Settings, WorkerError, identifier
from .datasets import parse_json
from .schemas import DatasetRequest, JobRequest, RuntimeRequest, ValidationRequest
from .service import Worker

LOG = logging.getLogger(__name__)
# JSON requests remain bounded separately; large JSONL uploads use the raw stream endpoint.
MAX_BODY_BYTES = 16 * 1024 * 1024


class AuthAndSize:
    def __init__(self, app: ASGIApp, key: str, max_dataset_bytes: int):
        self.app, self.key, self.max_dataset_bytes = app, key.encode("utf-8"), max_dataset_bytes

    async def __call__(self, scope: Scope, receive: Receive, send: Send):
        if scope["type"] != "http":
            await self.app(scope, receive, send)
            return
        headers = scope.get("headers", [])
        keys = [value for name, value in headers if name == b"x-ncf-worker-key"]
        if len(keys) != 1 or not hmac.compare_digest(keys[0], self.key):
            await JSONResponse({"detail": "Missing or invalid X-NCF-Worker-Key."}, 401)(scope, receive, send)
            return
        if scope.get("path") == "/datasets/file":
            length = next((value for name, value in headers if name == b"content-length"), None)
            if length:
                try:
                    if int(length) < 1 or int(length) > self.max_dataset_bytes:
                        raise ValueError()
                except ValueError:
                    await JSONResponse({"detail": "Dataset body exceeds the worker limit."}, 413)(scope, receive, send)
                    return
            await self.app(scope, receive, send)
            return
        length = next((value for name, value in headers if name == b"content-length"), None)
        if length:
            try:
                if int(length) < 0 or int(length) > MAX_BODY_BYTES:
                    raise ValueError()
            except ValueError:
                await JSONResponse({"detail": "Request body exceeds the worker limit."}, 413)(scope, receive, send)
                return
        chunks, size = [], 0
        while True:
            message = await receive()
            if message["type"] == "http.disconnect":
                return
            body = message.get("body", b"")
            size += len(body)
            if size > MAX_BODY_BYTES:
                await JSONResponse({"detail": "Request body exceeds the worker limit."}, 413)(scope, receive, send)
                return
            chunks.append(body)
            if not message.get("more_body", False):
                break
        body = b"".join(chunks)
        if body:
            try:
                parse_json(body.decode("utf-8"))
            except (ValueError, UnicodeError, RecursionError) as exc:
                await JSONResponse({"detail": f"Invalid JSON: {exc}"}, 422)(scope, receive, send)
                return
        delivered = False

        async def replay():
            nonlocal delivered
            if not delivered:
                delivered = True
                return {"type": "http.request", "body": body, "more_body": False}
            return await receive()

        await self.app(scope, replay, send)


def create_app(settings=None, worker_factory=Worker):
    settings = settings or Settings.from_env()

    @asynccontextmanager
    async def lifespan(app):
        worker = await run_in_threadpool(worker_factory, settings)
        app.state.worker = worker
        try:
            yield
        finally:
            await run_in_threadpool(worker.close)

    app = FastAPI(title="NCF local fine-tuning worker", version=VERSION, lifespan=lifespan,
                  docs_url=None, redoc_url=None, openapi_url=None)
    app.add_middleware(AuthAndSize, key=settings.key, max_dataset_bytes=settings.max_dataset_bytes)

    @app.exception_handler(WorkerError)
    async def domain_error(request, exc):
        LOG.warning("%s %s rejected: %s", request.method, request.url.path, exc)
        return JSONResponse({"detail": str(exc), "rowErrors": exc.details} if exc.details else {"detail": str(exc)}, exc.status)

    @app.exception_handler(RequestValidationError)
    async def validation_error(request, exc):
        # Never echo potentially multi-megabyte datasets or the authentication key.
        errors = [{"field": ".".join(map(str, error["loc"])), "error": error["msg"]} for error in exc.errors()]
        return JSONResponse({"detail": "Request validation failed.", "errors": errors}, 422)

    def worker(request: Request):
        return request.app.state.worker

    @app.get("/health")
    def health(service=Depends(worker)):
        return service.health()

    @app.get("/models")
    def models(service=Depends(worker)):
        return service.models()

    @app.post("/datasets")
    def upload(body: DatasetRequest, service=Depends(worker)):
        return service.store.upload(body.name, body.content)

    @app.post("/datasets/file")
    async def upload_file(request: Request, name: str = Query(..., min_length=1, max_length=100),
                          service=Depends(worker)):
        temporary_path = service.store.create_upload_temp()
        size = 0
        try:
            with temporary_path.open("wb") as output:
                async for chunk in request.stream():
                    size += len(chunk)
                    if size > service.settings.max_dataset_bytes:
                        raise WorkerError(
                            f"Dataset exceeds the {service.settings.max_dataset_bytes // (1024 * 1024)} MiB UTF-8 limit.", 413)
                    output.write(chunk)
            if size == 0:
                raise WorkerError("Dataset content must be nonempty JSONL.")
            return await run_in_threadpool(service.store.upload_file, name, temporary_path)
        finally:
            if temporary_path.exists():
                os.unlink(temporary_path)

    @app.get("/datasets")
    def datasets(service=Depends(worker)):
        return service.store.datasets()

    @app.get("/datasets/page")
    def datasets_page(offset: int = Query(0, ge=0, le=100000), limit: int = Query(100, ge=1, le=200), service=Depends(worker)):
        return service.store.datasets_page(offset, limit)

    @app.get("/datasets/{dataset_id}")
    def dataset(dataset_id: str, service=Depends(worker)):
        return service.store.dataset(identifier(dataset_id))

    @app.post("/jobs")
    def create_job(body: JobRequest, service=Depends(worker)):
        return service.create_job(body.model_dump())

    @app.get("/jobs")
    def jobs(service=Depends(worker)):
        return service.store.jobs()

    @app.get("/jobs/page")
    def jobs_page(offset: int = Query(0, ge=0, le=100000), limit: int = Query(100, ge=1, le=200), service=Depends(worker)):
        return service.store.jobs_page(offset, limit)

    @app.get("/jobs/{job_id}")
    def job(job_id: str, service=Depends(worker)):
        return service.store.job(identifier(job_id))

    @app.post("/jobs/{job_id}/cancel")
    def cancel(job_id: str, service=Depends(worker)):
        return service.cancel(identifier(job_id))

    @app.post("/jobs/{job_id}/pause")
    def pause(job_id: str, service=Depends(worker)):
        return service.pause(identifier(job_id))

    @app.post("/jobs/{job_id}/resume")
    def resume(job_id: str, service=Depends(worker)):
        return service.resume(identifier(job_id))

    @app.post("/jobs/{job_id}/runtime")
    def update_runtime(job_id: str, body: RuntimeRequest, service=Depends(worker)):
        return service.update_runtime(identifier(job_id), body.maxDurationMinutes)

    @app.delete("/jobs/{job_id}")
    def delete_job(job_id: str, service=Depends(worker)):
        return service.delete(identifier(job_id))

    @app.get("/jobs/{job_id}/events")
    def events(job_id: str, after: int = Query(0, ge=0), limit: int = Query(200, ge=1, le=200), service=Depends(worker)):
        return service.store.events(identifier(job_id), after, limit)

    @app.get("/jobs/{job_id}/validation")
    def validation(job_id: str, service=Depends(worker)):
        return service.validation(identifier(job_id))

    @app.post("/jobs/{job_id}/validation")
    def start_validation(job_id: str, body: ValidationRequest, service=Depends(worker)):
        return service.start_validation(identifier(job_id), body)

    @app.get("/jobs/{job_id}/artifacts/{artifact_id}")
    def artifact(job_id: str, artifact_id: str, service=Depends(worker)):
        path, name = service.store.artifact(identifier(job_id), identifier(artifact_id))
        return FileResponse(path, filename=name, media_type="application/octet-stream",
                            headers={"X-Content-Type-Options": "nosniff", "Cache-Control": "private, immutable"})

    return app

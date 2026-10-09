import logging
import os

import uvicorn

from .config import OFFLINE_ENV, env_int

os.environ.update(OFFLINE_ENV)
logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s: %(message)s")
uvicorn.run("worker.api:create_app", factory=True, host=os.environ.get("NCF_BIND_HOST", "127.0.0.1"),
            port=env_int("NCF_WORKER_PORT", 8091, 1024, 65535), workers=1, access_log=False)

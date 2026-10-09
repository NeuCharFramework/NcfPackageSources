import shutil
import time


class Telemetry:
    def __init__(self, process, root):
        import psutil

        self.psutil = psutil
        try:
            self.process = psutil.Process(process.pid)
            self.process.cpu_percent(None)
        except psutil.NoSuchProcess:
            self.process = None
        self.root = root
        self.started = time.monotonic()
        self.nvml = None
        self.gpu_reason = "NVML not installed or no NVIDIA GPU."
        try:
            import pynvml
            pynvml.nvmlInit()
            self.nvml = pynvml
            self.gpu_reason = None
        except ImportError:
            pass
        except Exception as exc:
            # NVML defines its optional error hierarchy only after import.
            if not isinstance(exc, pynvml.NVMLError):
                raise
            self.gpu_reason = f"NVML unavailable: {exc}"

    def sample(self):
        values = {"elapsedSeconds": time.monotonic() - self.started}
        unavailable = {}
        try:
            if self.process is None:
                raise self.psutil.NoSuchProcess(-1)
            processes = [self.process] + self.process.children(recursive=True)
            values["processRssBytes"] = sum(proc.memory_info().rss for proc in processes)
            values["processCpuPercent"] = self.process.cpu_percent(None)
            values["diskUsedBytes"] = shutil.disk_usage(self.root).used
            values["diskFreeBytes"] = shutil.disk_usage(self.root).free
        except (self.psutil.NoSuchProcess, self.psutil.AccessDenied, OSError) as exc:
            unavailable["process"] = str(exc)
        if self.nvml is None:
            unavailable["gpu"] = self.gpu_reason
            values.update(gpuMemoryBytes=None, gpuUtilizationPercent=None, gpuTemperatureCelsius=None)
        else:
            try:
                handle = self.nvml.nvmlDeviceGetHandleByIndex(0)
                memory = self.nvml.nvmlDeviceGetMemoryInfo(handle)
                utilization = self.nvml.nvmlDeviceGetUtilizationRates(handle)
                values.update(
                    gpuMemoryBytes=memory.used, gpuUtilizationPercent=utilization.gpu,
                    gpuTemperatureCelsius=self.nvml.nvmlDeviceGetTemperature(handle, self.nvml.NVML_TEMPERATURE_GPU),
                )
            except self.nvml.NVMLError as exc:
                unavailable["gpu"] = str(exc)
                values.update(gpuMemoryBytes=None, gpuUtilizationPercent=None, gpuTemperatureCelsius=None)
        return values, unavailable

    def close(self):
        if self.nvml:
            self.nvml.nvmlShutdown()

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LocalVoiceInputService.cs
    文件功能描述：跨平台麦克风录音、Whisper 模型下载与本地转写

    创建标识：Senparc - 20260801
----------------------------------------------------------------*/

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NcfDesktopApp.GUI.Models;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Structs;
using Whisper.net;
using Whisper.net.Ggml;

namespace NcfDesktopApp.GUI.Services;

internal interface ILocalVoiceInputService
{
    Guid? RecordingOwner { get; }

    Task DownloadModelAsync(
        VoiceModelOption option,
        Action<long>? progress,
        CancellationToken cancellationToken);

    Task StartRecordingAsync(
        Guid owner,
        string modelPath,
        CancellationToken cancellationToken);

    Task<string> StopAndTranscribeAsync(
        Guid owner,
        string language,
        CancellationToken cancellationToken);

    Task CancelRecordingAsync(Guid owner);
}

internal sealed class LocalVoiceInputService : ILocalVoiceInputService, IDisposable
{
    private static readonly Lazy<LocalVoiceInputService> SharedInstance = new(() => new LocalVoiceInputService());
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly object _stateLock = new();

    private MiniAudioEngine? _audioEngine;
    private AudioCaptureDevice? _captureDevice;
    private Recorder? _recorder;
    private MemoryStream? _recordingStream;
    private Guid? _recordingOwner;
    private string _recordingModelPath = string.Empty;
    private WhisperFactory? _loadedFactory;
    private string _loadedModelPath = string.Empty;
    private long _loadedModelLength;
    private DateTime _loadedModelWriteTimeUtc;
    private VoiceOperationStage _operationStage;
    private bool _disposed;

    public static LocalVoiceInputService Shared => SharedInstance.Value;

    public static void DisposeShared()
    {
        if (SharedInstance.IsValueCreated)
        {
            SharedInstance.Value.Dispose();
        }
    }

    public Guid? RecordingOwner
    {
        get
        {
            lock (_stateLock)
            {
                return _recordingOwner;
            }
        }
    }

    public async Task DownloadModelAsync(
        VoiceModelOption option,
        Action<long>? progress,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!option.CanDownload)
        {
            throw new InvalidOperationException("手动模型不能自动下载，请选择本地 GGML 文件。");
        }

        if (!await _operationGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("麦克风或语音模型正在被另一个工作台使用，请稍后重试。");
        }

        string? temporaryPath = null;
        try
        {
            Directory.CreateDirectory(VoiceModelCatalog.ModelsDirectory);
            var targetPath = VoiceModelCatalog.GetModelPath(option, null);
            temporaryPath = $"{targetPath}.download";
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            using var modelStream = await WhisperGgmlDownloader.Default
                .GetGgmlModelAsync(VoiceModelCatalog.GetGgmlType(option), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await using var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                1024 * 128,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var buffer = new byte[1024 * 128];
            long downloaded = 0;
            int read;
            while ((read = await modelStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                downloaded += read;
                progress?.Invoke(downloaded);
            }

            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            var readiness = VoiceModelCatalog.EvaluateFile(option, temporaryPath);
            if (!readiness.IsReady)
            {
                throw new InvalidDataException(readiness.Message);
            }

            File.Move(temporaryPath, targetPath, overwrite: true);
        }
        finally
        {
            try
            {
                if (!string.IsNullOrEmpty(temporaryPath) && File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            finally
            {
                _operationGate.Release();
            }
        }
    }

    public async Task StartRecordingAsync(
        Guid owner,
        string modelPath,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
        {
            throw new FileNotFoundException("语音模型尚未准备好。", modelPath);
        }

        if (!await _operationGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("麦克风或语音模型正在被另一个工作台使用，请稍后重试。");
        }

        try
        {
            _audioEngine ??= new MiniAudioEngine();
            _audioEngine.UpdateAudioDevicesInfo();
            var device = _audioEngine.CaptureDevices.FirstOrDefault(item => item.IsDefault);
            if (string.IsNullOrWhiteSpace(device.Name))
            {
                device = _audioEngine.CaptureDevices.FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(device.Name))
            {
                throw new InvalidOperationException("未检测到可用的麦克风，请检查系统录音设备和权限。");
            }

            var format = new AudioFormat
            {
                SampleRate = 16000,
                Channels = 1,
                Format = SampleFormat.S16,
                Layout = ChannelLayout.Mono
            };
            _recordingStream = new MemoryStream();
            _captureDevice = _audioEngine.InitializeCaptureDevice(device, format);
            _recorder = new Recorder(_captureDevice, _recordingStream, "wav");

            _captureDevice.Start();
            var result = _recorder.StartRecording();
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(result.Error?.Message ?? "无法启动录音。");
            }

            lock (_stateLock)
            {
                _recordingOwner = owner;
                _recordingModelPath = modelPath;
                _operationStage = VoiceOperationStage.Recording;
            }
        }
        catch
        {
            CleanupRecordingResources(discardAudio: true);
            _operationGate.Release();
            throw;
        }
    }

    public async Task<string> StopAndTranscribeAsync(
        Guid owner,
        string language,
        CancellationToken cancellationToken)
    {
        BeginTranscription(owner);
        byte[] audioBytes;
        string modelPath;
        try
        {
            var stopResult = await _recorder!.StopRecordingAsync().ConfigureAwait(false);
            if (!stopResult.IsSuccess)
            {
                throw new InvalidOperationException(stopResult.Error?.Message ?? "停止录音失败。");
            }

            _captureDevice?.Stop();
            audioBytes = _recordingStream!.ToArray();
            lock (_stateLock)
            {
                modelPath = _recordingModelPath;
            }
            CleanupRecordingResources(discardAudio: true);

            // WAV 头之外至少保留约 0.1 秒音频，避免把误触当作有效输入。
            if (audioBytes.Length < 3200)
            {
                throw new InvalidOperationException("录音时间过短，请重新录入。");
            }

            var factory = GetOrLoadFactory(modelPath);
            using var processor = factory.CreateBuilder()
                .WithLanguage(NormalizeLanguage(language))
                .WithThreads(Math.Clamp(Environment.ProcessorCount / 2, 1, 4))
                .Build();
            using var waveStream = new MemoryStream(audioBytes, writable: false);
            var transcript = new StringBuilder();
            await foreach (var segment in processor.ProcessAsync(waveStream, cancellationToken))
            {
                transcript.Append(segment.Text);
            }

            var text = transcript.ToString().Trim();
            if (text.Length == 0)
            {
                throw new InvalidOperationException("未识别到有效语音，请靠近麦克风后重试。");
            }

            return text;
        }
        finally
        {
            CleanupRecordingResources(discardAudio: true);
            ClearRecordingState();
            _operationGate.Release();
        }
    }

    public async Task CancelRecordingAsync(Guid owner)
    {
        lock (_stateLock)
        {
            if (_recordingOwner == null)
            {
                return;
            }

            if (_recordingOwner != owner)
            {
                throw new InvalidOperationException("麦克风正在被另一个工作台使用。");
            }

            // 转写阶段由其 CancellationToken 负责取消；不得在这里再次释放操作锁。
            if (_operationStage != VoiceOperationStage.Recording)
            {
                return;
            }

            _operationStage = VoiceOperationStage.Cancelling;
        }

        try
        {
            if (_recorder != null)
            {
                await _recorder.StopRecordingAsync().ConfigureAwait(false);
            }

            _captureDevice?.Stop();
        }
        finally
        {
            CleanupRecordingResources(discardAudio: true);
            ClearRecordingState();
            _operationGate.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _recorder?.Dispose();
        }
        catch
        {
            // 应用退出时不得因音频后端清理失败阻止进程结束。
        }

        CleanupRecordingResources(discardAudio: true);
        _loadedFactory?.Dispose();
        _loadedFactory = null;
        _audioEngine?.Dispose();
        _audioEngine = null;
        _operationGate.Dispose();
    }

    private WhisperFactory GetOrLoadFactory(string modelPath)
    {
        var file = new FileInfo(modelPath);
        if (_loadedFactory != null &&
            string.Equals(_loadedModelPath, modelPath, StringComparison.OrdinalIgnoreCase) &&
            _loadedModelLength == file.Length &&
            _loadedModelWriteTimeUtc == file.LastWriteTimeUtc)
        {
            return _loadedFactory;
        }

        _loadedFactory?.Dispose();
        _loadedFactory = WhisperFactory.FromPath(modelPath);
        _loadedModelPath = modelPath;
        _loadedModelLength = file.Length;
        _loadedModelWriteTimeUtc = file.LastWriteTimeUtc;
        return _loadedFactory;
    }

    private static string NormalizeLanguage(string language)
    {
        return language?.Trim().ToLowerInvariant() switch
        {
            "zh" => "zh",
            "en" => "en",
            _ => "auto"
        };
    }

    private void BeginTranscription(Guid owner)
    {
        lock (_stateLock)
        {
            if (_recordingOwner == null ||
                _operationStage != VoiceOperationStage.Recording ||
                _recorder == null ||
                _recordingStream == null)
            {
                throw new InvalidOperationException("当前没有正在进行的录音。");
            }

            if (_recordingOwner != owner)
            {
                throw new InvalidOperationException("麦克风正在被另一个工作台使用。");
            }

            _operationStage = VoiceOperationStage.Transcribing;
        }
    }

    private void CleanupRecordingResources(bool discardAudio)
    {
        try
        {
            _recorder?.Dispose();
        }
        catch
        {
            // 后续清理仍需继续。
        }
        _recorder = null;

        try
        {
            _captureDevice?.Dispose();
        }
        catch
        {
            // 后续清理仍需继续。
        }
        _captureDevice = null;

        if (discardAudio)
        {
            _recordingStream?.Dispose();
        }
        _recordingStream = null;
    }

    private void ClearRecordingState()
    {
        lock (_stateLock)
        {
            _recordingOwner = null;
            _recordingModelPath = string.Empty;
            _operationStage = VoiceOperationStage.None;
        }
    }

    private enum VoiceOperationStage
    {
        None,
        Recording,
        Transcribing,
        Cancelling
    }
}

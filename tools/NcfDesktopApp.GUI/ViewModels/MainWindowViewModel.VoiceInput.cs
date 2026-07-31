/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：MainWindowViewModel.VoiceInput.cs
    文件功能描述：本地语音模型配置、录音、转写及 AdminChat 输入联动

    创建标识：Senparc - 20260801
----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NcfDesktopApp.GUI.Models;
using NcfDesktopApp.GUI.Services;

namespace NcfDesktopApp.GUI.ViewModels;

public partial class MainWindowViewModel
{
    private readonly ILocalVoiceInputService _voiceInputService = LocalVoiceInputService.Shared;
    private readonly Guid _voiceInputOwner = Guid.NewGuid();
    private CancellationTokenSource? _voiceRecognitionCts;
    private CancellationTokenSource? _voiceModelDownloadCts;

    [ObservableProperty]
    private VoiceModelOption? _selectedVoiceModel;

    [ObservableProperty]
    private string _voiceCustomModelPath = string.Empty;

    [ObservableProperty]
    private string _voiceLanguage = "auto";

    [ObservableProperty]
    private string _voiceModelStatusText = "尚未选择语音模型。";

    [ObservableProperty]
    private string _voiceModelPathText = "—";

    [ObservableProperty]
    private string _voiceModelDownloadProgressText = string.Empty;

    [ObservableProperty]
    private bool _isVoiceModelBusy;

    [ObservableProperty]
    private bool _isVoiceModelReady;

    [ObservableProperty]
    private bool _isVoiceRecording;

    [ObservableProperty]
    private bool _isVoiceTranscribing;

    [ObservableProperty]
    private string _voiceInputStatusText = "语音将在本机转写；识别结果会先进入输入框，不会自动发送。";

    public IReadOnlyList<VoiceModelOption> VoiceModelOptions => VoiceModelCatalog.Options;

    public IReadOnlyList<string> VoiceLanguageOptions { get; } = new[] { "auto", "zh", "en" };

    public string VoiceInputButtonText => IsVoiceRecording ? "停止" : IsVoiceTranscribing ? "识别中" : "语音";

    public string VoiceInputButtonIcon => IsVoiceRecording ? "■" : IsVoiceTranscribing ? "…" : "🎙";

    public string VoiceModelStatusColor => IsVoiceModelReady ? "#16A34A" : "#D97706";

    public bool IsVoiceInputBusy => IsVoiceRecording || IsVoiceTranscribing;

    public bool IsVoiceCancelVisible => IsVoiceRecording || IsVoiceTranscribing || IsVoiceModelBusy;

    partial void OnSelectedVoiceModelChanged(VoiceModelOption? value)
    {
        RefreshVoiceModelReadiness();
        NotifyVoiceCommandsChanged();
        if (!_suppressDesktopSettingsSave)
        {
            SaveDesktopSettings();
        }
    }

    partial void OnVoiceCustomModelPathChanged(string value)
    {
        RefreshVoiceModelReadiness();
        NotifyVoiceCommandsChanged();
        if (!_suppressDesktopSettingsSave)
        {
            SaveDesktopSettings();
        }
    }

    partial void OnVoiceLanguageChanged(string value)
    {
        var normalized = NormalizeVoiceLanguage(value);
        if (!string.Equals(normalized, value, StringComparison.Ordinal))
        {
            VoiceLanguage = normalized;
            return;
        }

        if (!_suppressDesktopSettingsSave)
        {
            SaveDesktopSettings();
        }
    }

    partial void OnIsVoiceModelBusyChanged(bool value) => NotifyVoiceStateChanged();

    partial void OnIsVoiceModelReadyChanged(bool value)
    {
        OnPropertyChanged(nameof(VoiceModelStatusColor));
        NotifyVoiceCommandsChanged();
    }

    partial void OnIsVoiceRecordingChanged(bool value) => NotifyVoiceStateChanged();

    partial void OnIsVoiceTranscribingChanged(bool value) => NotifyVoiceStateChanged();

    [RelayCommand(CanExecute = nameof(CanDownloadVoiceModel))]
    private async Task DownloadVoiceModel()
    {
        var option = SelectedVoiceModel;
        if (option == null || !option.CanDownload)
        {
            VoiceModelStatusText = "请先选择可下载的内置模型。";
            return;
        }

        var current = VoiceModelCatalog.Evaluate(option, VoiceCustomModelPath);
        if (current.IsReady)
        {
            RefreshVoiceModelReadiness();
            return;
        }

        _voiceModelDownloadCts?.Cancel();
        _voiceModelDownloadCts?.Dispose();
        _voiceModelDownloadCts = new CancellationTokenSource();
        IsVoiceModelBusy = true;
        VoiceModelDownloadProgressText = $"正在下载 {option.DisplayName}…";
        VoiceModelStatusText = "模型下载完成前不能开始语音输入。";
        long lastReportedBytes = 0;
        try
        {
            await _voiceInputService.DownloadModelAsync(
                option,
                bytes =>
                {
                    if (bytes - lastReportedBytes < 2 * 1024 * 1024 && bytes < option.ApproximateBytes)
                    {
                        return;
                    }

                    lastReportedBytes = bytes;
                    Dispatcher.UIThread.Post(() =>
                    {
                        var percent = option.ApproximateBytes <= 0
                            ? 0
                            : Math.Clamp(bytes * 100d / option.ApproximateBytes, 0, 100);
                        VoiceModelDownloadProgressText =
                            $"已下载 {VoiceModelCatalog.FormatBytes(bytes)} / {option.ApproximateSizeText}（约 {percent:F0}%）";
                    });
                },
                _voiceModelDownloadCts.Token);

            RefreshVoiceModelReadiness();
            VoiceModelDownloadProgressText = "模型下载完成，可离线使用语音输入。";
            AddLog($"✅ 本地语音模型已下载: {VoiceModelPathText}");
        }
        catch (OperationCanceledException)
        {
            VoiceModelStatusText = "模型下载已取消，可稍后重新下载。";
            VoiceModelDownloadProgressText = string.Empty;
        }
        catch (Exception ex)
        {
            VoiceModelStatusText = $"模型下载失败：{ex.Message}";
            VoiceModelDownloadProgressText = string.Empty;
            AddLog($"❌ 语音模型下载失败: {ex.Message}");
        }
        finally
        {
            IsVoiceModelBusy = false;
            _voiceModelDownloadCts?.Dispose();
            _voiceModelDownloadCts = null;
            NotifyVoiceCommandsChanged();
        }
    }

    private bool CanDownloadVoiceModel()
    {
        return SelectedVoiceModel is { CanDownload: true } &&
               !IsVoiceModelReady &&
               !IsVoiceModelBusy &&
               !IsVoiceInputBusy;
    }

    [RelayCommand(CanExecute = nameof(CanSelectCustomVoiceModel))]
    private async Task SelectCustomVoiceModel()
    {
        try
        {
            var storageProvider = GetActiveStorageProvider();
            if (storageProvider is not { CanOpen: true })
            {
                VoiceModelStatusText = "当前平台无法打开模型文件选择器。";
                return;
            }

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择 Whisper GGML 模型",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Whisper GGML 模型")
                    {
                        Patterns = new[] { "*.bin" },
                        MimeTypes = new[] { "application/octet-stream" }
                    }
                }
            });
            var path = files.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            _suppressDesktopSettingsSave = true;
            VoiceCustomModelPath = path;
            SelectedVoiceModel = VoiceModelCatalog.FindById(VoiceModelCatalog.CustomModelId);
            _suppressDesktopSettingsSave = false;
            RefreshVoiceModelReadiness();
            SaveDesktopSettings();
        }
        catch (Exception ex)
        {
            VoiceModelStatusText = $"选择模型失败：{ex.Message}";
        }
    }

    private bool CanSelectCustomVoiceModel() => !IsVoiceModelBusy && !IsVoiceInputBusy;

    [RelayCommand]
    private void OpenVoiceModelDirectory()
    {
        Directory.CreateDirectory(VoiceModelCatalog.ModelsDirectory);
        OpenBrowser(VoiceModelCatalog.ModelsDirectory);
    }

    [RelayCommand]
    private async Task ToggleVoiceInput()
    {
        if (IsVoiceTranscribing || IsVoiceModelBusy)
        {
            VoiceInputStatusText = "当前语音操作尚未完成，请稍候。";
            return;
        }

        if (IsVoiceRecording)
        {
            await StopVoiceInputAndTranscribeAsync();
            return;
        }

        var readiness = VoiceModelCatalog.Evaluate(SelectedVoiceModel, VoiceCustomModelPath);
        ApplyVoiceModelReadiness(readiness);
        if (!readiness.IsReady)
        {
            VoiceInputStatusText = readiness.Message;
            ShowWorkspaceSettingsRequested?.Invoke();
            Robot.SetVoiceInputState("需要配置", readiness.Message, isError: true);
            return;
        }

        if (!IsAdminChatActive)
        {
            VoiceInputStatusText = "请先启动 NCF、连接 DesktopBridge 并登录 AdminChat。";
            Robot.SetVoiceInputState("等待登录", VoiceInputStatusText, isError: true);
            return;
        }

        if (IsAdminChatBusy)
        {
            VoiceInputStatusText = "AdminChat 正在处理上一条消息，请稍后再开始录音。";
            return;
        }

        _voiceRecognitionCts?.Cancel();
        _voiceRecognitionCts?.Dispose();
        _voiceRecognitionCts = new CancellationTokenSource();
        try
        {
            await _voiceInputService.StartRecordingAsync(
                _voiceInputOwner,
                readiness.ModelPath,
                _voiceRecognitionCts.Token);
            IsVoiceRecording = true;
            VoiceInputStatusText = "正在录音；再次点击“停止”后将在本机识别。";
            Robot.SetVoiceInputState("正在录音", "再次点击语音按钮即可停止并转写");
        }
        catch (OperationCanceledException)
        {
            VoiceInputStatusText = "语音输入已取消。";
        }
        catch (Exception ex)
        {
            VoiceInputStatusText = $"无法开始录音：{ex.Message}";
            Robot.SetVoiceInputState("录音失败", ex.Message, isError: true);
            AddLog($"❌ 无法开始语音输入: {ex.Message}");
        }
        finally
        {
            if (!IsVoiceRecording)
            {
                _voiceRecognitionCts?.Dispose();
                _voiceRecognitionCts = null;
            }
        }
    }

    [RelayCommand]
    private async Task CancelVoiceInput()
    {
        _voiceModelDownloadCts?.Cancel();
        _voiceRecognitionCts?.Cancel();
        try
        {
            if (IsVoiceRecording)
            {
                await _voiceInputService.CancelRecordingAsync(_voiceInputOwner);
            }
        }
        catch (Exception ex)
        {
            AddLog($"⚠️ 取消语音输入时清理失败: {ex.Message}");
        }
        finally
        {
            IsVoiceRecording = false;
            VoiceInputStatusText = "语音输入已取消。";
            Robot.SetVoiceInputState("已取消", "语音内容未发送");
        }
    }

    internal async Task CancelVoiceInputForShutdownAsync()
    {
        _voiceModelDownloadCts?.Cancel();
        _voiceRecognitionCts?.Cancel();
        if (_voiceInputService.RecordingOwner == _voiceInputOwner)
        {
            await _voiceInputService.CancelRecordingAsync(_voiceInputOwner);
        }
    }

    internal void RefreshVoiceModelReadiness()
    {
        ApplyVoiceModelReadiness(VoiceModelCatalog.Evaluate(SelectedVoiceModel, VoiceCustomModelPath));
    }

    private async Task StopVoiceInputAndTranscribeAsync()
    {
        IsVoiceRecording = false;
        IsVoiceTranscribing = true;
        VoiceInputStatusText = "正在使用本地模型识别，不会上传音频…";
        Robot.SetVoiceInputState("正在识别", "本地 Whisper 模型正在转写语音");
        try
        {
            var transcript = await _voiceInputService.StopAndTranscribeAsync(
                _voiceInputOwner,
                VoiceLanguage,
                _voiceRecognitionCts?.Token ?? CancellationToken.None);
            ChatInput = string.IsNullOrWhiteSpace(ChatInput)
                ? transcript
                : $"{ChatInput.TrimEnd()}{Environment.NewLine}{transcript}";
            VoiceInputStatusText = "识别完成，文字已放入输入框；确认后再发送。";
            Robot.SetVoiceInputState("识别完成", "文字已写入 AdminChat 输入框，请确认后发送");
            AddLog("✅ 本地语音识别完成，原始音频未上传");
        }
        catch (OperationCanceledException)
        {
            VoiceInputStatusText = "语音识别已取消，内容未发送。";
            Robot.SetVoiceInputState("已取消", "语音内容未发送");
        }
        catch (Exception ex)
        {
            VoiceInputStatusText = $"语音识别失败：{ex.Message}";
            Robot.SetVoiceInputState("识别失败", ex.Message, isError: true);
            AddLog($"❌ 本地语音识别失败: {ex.Message}");
        }
        finally
        {
            IsVoiceTranscribing = false;
            _voiceRecognitionCts?.Dispose();
            _voiceRecognitionCts = null;
        }
    }

    private void ApplyVoiceModelReadiness(VoiceModelReadiness readiness)
    {
        IsVoiceModelReady = readiness.IsReady;
        VoiceModelPathText = string.IsNullOrWhiteSpace(readiness.ModelPath) ? "—" : readiness.ModelPath;
        VoiceModelStatusText = readiness.Message;
        OnPropertyChanged(nameof(VoiceModelStatusColor));
        NotifyVoiceCommandsChanged();
    }

    private void NotifyVoiceStateChanged()
    {
        OnPropertyChanged(nameof(VoiceInputButtonText));
        OnPropertyChanged(nameof(VoiceInputButtonIcon));
        OnPropertyChanged(nameof(IsVoiceInputBusy));
        OnPropertyChanged(nameof(IsVoiceCancelVisible));
        NotifyVoiceCommandsChanged();
    }

    private void NotifyVoiceCommandsChanged()
    {
        DownloadVoiceModelCommand.NotifyCanExecuteChanged();
        SelectCustomVoiceModelCommand.NotifyCanExecuteChanged();
    }

    private static string NormalizeVoiceLanguage(string? language)
    {
        return language?.Trim().ToLowerInvariant() switch
        {
            "zh" => "zh",
            "en" => "en",
            _ => "auto"
        };
    }

    private static IStorageProvider? GetActiveStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return null;
        }

        return desktop.Windows.FirstOrDefault(window => window.IsActive)?.StorageProvider ??
               desktop.MainWindow?.StorageProvider;
    }
}

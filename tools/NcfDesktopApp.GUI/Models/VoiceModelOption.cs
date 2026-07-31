/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：VoiceModelOption.cs
    文件功能描述：桌面端本地语音识别模型选项

    创建标识：Senparc - 20260801
----------------------------------------------------------------*/

namespace NcfDesktopApp.GUI.Models;

public enum LocalVoiceModelKind
{
    Tiny,
    Base,
    Small,
    Custom
}

/// <summary>
/// 可在桌面端配置中选择的离线 Whisper 模型。
/// </summary>
public sealed record VoiceModelOption(
    string Id,
    string DisplayName,
    string Description,
    string ApproximateSizeText,
    LocalVoiceModelKind Kind,
    string FileName,
    long ApproximateBytes,
    long MinimumExpectedBytes,
    bool CanDownload)
{
    public override string ToString() => DisplayName;
}

internal enum VoiceModelReadinessState
{
    NotSelected,
    Missing,
    Incomplete,
    Ready
}

internal sealed record VoiceModelReadiness(
    VoiceModelReadinessState State,
    string ModelPath,
    string Message)
{
    public bool IsReady => State == VoiceModelReadinessState.Ready;
}

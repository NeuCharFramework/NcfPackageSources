/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DesktopUserSettings.cs
    文件功能描述：桌面应用用户设置模型
    
    
    创建标识：Senparc - 20260504
    
    修改标识：Senparc - 20260724
    修改描述：v0.1.0 增强更新源选择、下载反馈与桌面窗口兼容性

----------------------------------------------------------------*/
using System.Collections.Generic;
using NcfDesktopApp.GUI.Models;

namespace NcfDesktopApp.GUI.Services;

/// <summary>
/// 桌面端用户设置（持久化到 AppData 目录下的 JSON）。
/// </summary>
public sealed class DesktopUserSettings
{
    public const string DefaultMirrorServerBaseUrl = "https://www.ncf.pub";

    public bool AutoOpenBrowser { get; set; } = true;

    public bool AutoCleanDownloads { get; set; }

    public bool ShowDetailedInfo { get; set; } = true;

    public int StartPort { get; set; } = 5000;

    public int EndPort { get; set; } = 5300;

    /// <summary>
    /// 镜像更新源站点根地址（不含路径）。实际请求元数据为 {此地址}/NcfPackages/latest-release.json。
    /// </summary>
    public string MirrorServerBaseUrl { get; set; } = DefaultMirrorServerBaseUrl;

    /// <summary>
    /// 当前工作模式。托管模式允许桌面端更新 Runtime，外部模式只校验和启动用户选择的目标。
    /// </summary>
    public NcfLaunchTargetKind LaunchTargetKind { get; set; } = NcfLaunchTargetKind.ManagedPublished;

    /// <summary>
    /// 最近选择的外部发布目录或源码工作区。
    /// </summary>
    public string ExternalNcfPath { get; set; } = string.Empty;

    /// <summary>
    /// 最近连接的远程 NCF 站点。DesktopBridge 令牌不会持久化。
    /// </summary>
    public string RemoteSiteUrl { get; set; } = string.Empty;

    /// <summary>
    /// 从 Senparc.NCF.Template 创建工作区时使用的父目录。
    /// </summary>
    public string TemplateWorkspaceParentPath { get; set; } = string.Empty;

    /// <summary>
    /// 外部目标最近使用记录。
    /// </summary>
    public List<string> RecentNcfPaths { get; set; } = new();

    /// <summary>
    /// 启动 NCF 时使用的 ASP.NET Core 环境。
    /// </summary>
    public string AspNetCoreEnvironment { get; set; } = "Production";

    /// <summary>
    /// 已选择的桌面端离线语音模型标识。空值表示用户尚未选择。
    /// </summary>
    public string VoiceModelId { get; set; } = string.Empty;

    /// <summary>
    /// 手动加载的 Whisper GGML 模型路径；内置可下载模型不使用此字段。
    /// </summary>
    public string VoiceCustomModelPath { get; set; } = string.Empty;

    /// <summary>
    /// 语音识别语言：auto、zh 或 en。
    /// </summary>
    public string VoiceLanguage { get; set; } = "auto";
}

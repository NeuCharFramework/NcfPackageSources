/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DesktopUserSettings.cs
    文件功能描述：桌面应用用户设置模型
    
    
    创建标识：Senparc - 20260504
    
    修改标识：Senparc - 20260724
    修改描述：v0.1.0 增强更新源选择、下载反馈与桌面窗口兼容性

----------------------------------------------------------------*/
namespace NcfDesktopApp.GUI.Services;

/// <summary>
/// 桌面端用户设置（持久化到 AppData 目录下的 JSON）。
/// </summary>
public sealed class DesktopUserSettings
{
    public const string DefaultMirrorServerBaseUrl = "https://www.ncf.pub";

    /// <summary>
    /// 镜像更新源站点根地址（不含路径）。实际请求元数据为 {此地址}/NcfPackages/latest-release.json。
    /// </summary>
    public string MirrorServerBaseUrl { get; set; } = DefaultMirrorServerBaseUrl;
}

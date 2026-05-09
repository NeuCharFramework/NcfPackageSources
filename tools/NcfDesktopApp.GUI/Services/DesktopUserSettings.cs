namespace NcfDesktopApp.GUI.Services;

/// <summary>
/// 桌面端用户设置（持久化到 AppData 目录下的 JSON）。
/// </summary>
public sealed class DesktopUserSettings
{
    public const string DefaultMirrorServerBaseUrl = "https://www.ncf.pub";

    /// <summary>
    /// 备用更新源站点根地址（不含路径）。实际请求元数据为 {此地址}/NcfPackages/latest-release.json。
    /// </summary>
    public string MirrorServerBaseUrl { get; set; } = DefaultMirrorServerBaseUrl;
}

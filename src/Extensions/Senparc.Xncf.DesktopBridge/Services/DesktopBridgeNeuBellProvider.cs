/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopBridgeNeuBellProvider.cs
    文件功能描述：DesktopBridge 待审批配对的纽铃 Provider

    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.3.0-preview3 新增桌面端同步提供程序

    修改标识：Senparc - 20260804
    修改描述：v0.3.0-preview3 将同步提供程序统一更名为 NeuBell/纽铃

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.NeuBell;

namespace Senparc.Xncf.DesktopBridge.Services;

public sealed class DesktopBridgeNeuBellProvider : INeuBellProvider
{
    public const string ProviderIdValue = "desktop-bridge-pairing";
    private readonly DesktopBridgeCredentialStore _credentialStore;

    public DesktopBridgeNeuBellProvider(DesktopBridgeCredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public string ProviderId => ProviderIdValue;

    // Admin Footer 使用此 UID 校验 DesktopBridge 是否已安装且处于开放状态。
    public string ModuleUid => Register.ModuleUid;

    public ValueTask<NeuBellSnapshot> GetSnapshotAsync(
        NeuBellRequestContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var pendingCount = _credentialStore.GetPendingPairings().Count;
        IReadOnlyList<NeuBellItem> items =
        [
            new NeuBellItem(
                "pending-pairings",
                DesktopBridgeResource.Get("NeuBell.Title"),
                pendingCount > 0
                    ? DesktopBridgeResource.Format("NeuBell.Pending", "Device pairing requests awaiting review: {0}.", pendingCount)
                    : DesktopBridgeResource.Get("NeuBell.Empty"),
                pendingCount,
                pendingCount > 0 ? "warning" : "info",
                $"/Admin/DesktopBridge/Index?uid={Register.ModuleUid}",
                DateTimeOffset.Now)
        ];

        return ValueTask.FromResult(new NeuBellSnapshot(
            ProviderId,
            Register.ModuleUid,
            "DesktopBridge",
            "fa fa-desktop",
            true,
            items));
    }
}

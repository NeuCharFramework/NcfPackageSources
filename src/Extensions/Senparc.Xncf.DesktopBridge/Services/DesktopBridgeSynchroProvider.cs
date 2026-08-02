/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopBridgeSynchroProvider.cs
    文件功能描述：DesktopBridge 待审批配对的 Synchro（灵犀）Provider

    创建标识：Senparc - 20260802
----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Synchro;

namespace Senparc.Xncf.DesktopBridge.Services;

public sealed class DesktopBridgeSynchroProvider : ISynchroProvider
{
    public const string ProviderIdValue = "desktop-bridge-pairing";
    private readonly DesktopBridgeCredentialStore _credentialStore;

    public DesktopBridgeSynchroProvider(DesktopBridgeCredentialStore credentialStore)
    {
        _credentialStore = credentialStore;
    }

    public string ProviderId => ProviderIdValue;

    public ValueTask<SynchroSnapshot> GetSnapshotAsync(
        SynchroRequestContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var pendingCount = _credentialStore.GetPendingPairings().Count;
        IReadOnlyList<SynchroItem> items =
        [
            new SynchroItem(
                "pending-pairings",
                "DesktopBridge 远程连接审核",
                pendingCount > 0 ? $"有 {pendingCount} 个设备配对请求等待处理。" : "当前没有待处理的设备配对请求。",
                pendingCount,
                pendingCount > 0 ? "warning" : "info",
                $"/Admin/DesktopBridge/Index?uid={Register.ModuleUid}",
                DateTimeOffset.Now)
        ];

        return ValueTask.FromResult(new SynchroSnapshot(
            ProviderId,
            Register.ModuleUid,
            "DesktopBridge",
            "fa fa-desktop",
            true,
            items));
    }
}

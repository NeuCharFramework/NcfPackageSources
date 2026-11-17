/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DesktopBridgeNeuBellProvider.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.3.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.3.0-preview3 Updated module behavior and related support.

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

    //Legacy comment translated to English.
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
                "DesktopBridge 远程连接审核",
                pendingCount > 0 ? $"有 {pendingCount} 个设备配对请求等待处理。" : "当前没有待处理的设备配对请求。",
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

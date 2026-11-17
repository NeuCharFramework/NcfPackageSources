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
                "DesktopBridge remote connection approval",
                pendingCount > 0 ? $"{pendingCount} device pairing requests are waiting for review." : "There are no pending device pairing requests.",
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

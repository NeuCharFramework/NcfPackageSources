/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfModulesInventoryEvents.cs
    File description: XncfModulesInventoryEvents related implementation

    Created by: Senparc - 20260510

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.1.6-preview7 Localized XNCF Builder tools and template surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.XncfBuilder.Abstractions.Events
{
    /// <summary>
    /// Describes one installed or missing XNCF module returned by inventory collection.
    /// </summary>
    /// <param name="MenuName">The module administration menu name.</param>
    /// <param name="ModuleName">The module type or display name.</param>
    /// <param name="Uid">The stable module UID.</param>
    /// <param name="RegisterVersion">The registered module version.</param>
    /// <param name="DatabaseVersion">The detected database version, when available.</param>
    /// <param name="StateDescription">The human-readable inventory state.</param>
    public record XncfModuleInventoryItem(
        string MenuName,
        string ModuleName,
        string Uid,
        string RegisterVersion,
        string? DatabaseVersion,
        string StateDescription);

    /// <summary>
    /// Requests an asynchronous inventory scan from the XNCF Builder integration boundary.
    /// </summary>
    /// <param name="RequestId">The correlation identifier used to match the response.</param>
    public record XncfModulesInventoryRequestEvent(string RequestId) : IntegrationEvent;

    /// <summary>
    /// Returns the result of an XNCF module inventory scan.
    /// </summary>
    /// <param name="RequestId">The request correlation identifier.</param>
    /// <param name="Success">Whether inventory collection completed successfully.</param>
    /// <param name="Message">The diagnostic or status message.</param>
    /// <param name="InstalledModules">Modules detected as installed.</param>
    /// <param name="NotInstalledModules">Modules known to the scan but not installed.</param>
    public record XncfModulesInventoryResponseEvent(
        string RequestId,
        bool Success,
        string Message,
        XncfModuleInventoryItem[] InstalledModules,
        XncfModuleInventoryItem[] NotInstalledModules) : IntegrationEvent;
}

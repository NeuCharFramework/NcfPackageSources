/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: XncfModulesInventoryEvents.cs
    File description: XncfModulesInventoryEvents related implementation
    
    
    Created by: Senparc - 20260510
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.XncfBuilder.Abstractions.Events
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public record XncfModuleInventoryItem(
        string MenuName,
        string ModuleName,
        string Uid,
        string RegisterVersion,
        string? DatabaseVersion,
        string StateDescription);

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public record XncfModulesInventoryRequestEvent(string RequestId) : IntegrationEvent;

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public record XncfModulesInventoryResponseEvent(
        string RequestId,
        bool Success,
        string Message,
        XncfModuleInventoryItem[] InstalledModules,
        XncfModuleInventoryItem[] NotInstalledModules) : IntegrationEvent;
}

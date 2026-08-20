/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfModulesInventoryEvents.cs
    文件功能描述：XncfModulesInventoryEvents 相关实现
    
    
    创建标识：Senparc - 20260510
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Shared.Abstractions.Events;

namespace Senparc.Xncf.XncfBuilder.Abstractions.Events
{
    /// <summary>
    /// 单条 XNCF 模块清单项（与数据库 / Register 展示字段对齐的纯数据）。
    /// </summary>
    public record XncfModuleInventoryItem(
        string MenuName,
        string ModuleName,
        string Uid,
        string RegisterVersion,
        string? DatabaseVersion,
        string StateDescription);

    /// <summary>
    /// 请求通过 EventBus 获取当前已安装与未安装（或未对齐版本）的 XNCF 模块清单。
    /// </summary>
    public record XncfModulesInventoryRequestEvent(string RequestId) : IntegrationEvent;

    /// <summary>
    /// <see cref="XncfModulesInventoryRequestEvent"/> 的响应。
    /// </summary>
    public record XncfModulesInventoryResponseEvent(
        string RequestId,
        bool Success,
        string Message,
        XncfModuleInventoryItem[] InstalledModules,
        XncfModuleInventoryItem[] NotInstalledModules) : IntegrationEvent;
}

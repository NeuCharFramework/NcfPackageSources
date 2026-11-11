/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ConnectorAuthorization.cs
    文件功能描述：计算 Agent 授权与本次对话选择的连接器动作交集
    
    
    创建标识：Senparc - 20261111
    
    修改标识：Senparc - 20261111
    修改描述：v0.1.0 计算 Agent 授权与对话选择的交集

----------------------------------------------------------------*/

namespace Senparc.Xncf.Connectors.Abstractions;

/// <summary>
/// Agent 授权与本次 Chat 选择的交集。未授权的实例不能因为对话里勾选而获得权限。
/// </summary>
public static class ConnectorAuthorization
{
    public static bool Allows(
        IEnumerable<AgentConnectorBinding>? bindings,
        string? instanceId,
        string? actionKey,
        bool limitToSelection,
        IEnumerable<string>? selectedInstanceIds)
    {
        if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(actionKey))
        {
            return false;
        }

        if (limitToSelection)
        {
            var selected = new HashSet<string>(
                selectedInstanceIds ?? [],
                StringComparer.OrdinalIgnoreCase);
            if (!selected.Contains(instanceId))
            {
                return false;
            }
        }

        var binding = bindings?.FirstOrDefault(item =>
            item != null
            && item.Enabled
            && string.Equals(item.InstanceId, instanceId, StringComparison.OrdinalIgnoreCase));
        if (binding == null)
        {
            return false;
        }

        if (binding.ActionKeys == null || binding.ActionKeys.Count == 0)
        {
            return true;
        }

        return binding.ActionKeys.Any(key =>
            string.Equals(key, actionKey, StringComparison.OrdinalIgnoreCase));
    }
}

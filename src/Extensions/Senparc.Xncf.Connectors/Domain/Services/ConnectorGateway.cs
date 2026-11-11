/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ConnectorGateway.cs
    文件功能描述：按目录和授权调用已配置的连接器动作
    
    
    创建标识：Senparc - 20261111
    
    修改标识：Senparc - 20261111
    修改描述：v0.1.0 按目录和授权调用连接器动作

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.Connectors.Abstractions;
using Senparc.Xncf.Connectors.Domain.Models.DatabaseModel;

namespace Senparc.Xncf.Connectors.Domain.Services;

public sealed class ConnectorGateway : IConnectorGateway
{
    private readonly ConnectorsSenparcEntities _db;
    private readonly IReadOnlyDictionary<string, IConnectorProvider> _providers;
    private readonly ConnectorSecretProtector _secrets;
    private readonly ILogger<ConnectorGateway> _logger;

    public ConnectorGateway(
        ConnectorsSenparcEntities db,
        IEnumerable<IConnectorProvider> providers,
        ConnectorSecretProtector secrets,
        ILogger<ConnectorGateway> logger)
    {
        _db = db;
        _secrets = secrets;
        _logger = logger;
        _providers = providers
            .GroupBy(item => item.Describe().Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);
    }

    public Task<IReadOnlyList<ConnectorDescriptor>> ListCatalogAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ConnectorDescriptor> catalog = _providers.Values
            .Select(item => item.Describe())
            .OrderBy(item => item.Name, StringComparer.CurrentCulture)
            .ToArray();
        _logger.LogInformation("[Connector] catalog count={Count}", catalog.Count);
        return Task.FromResult(catalog);
    }

    public async Task<IReadOnlyList<ConfiguredConnector>> ListEnabledAsync(CancellationToken cancellationToken)
    {
        var instances = await _db.ConnectorInstances
            .AsNoTracking()
            .Where(item => item.Enabled)
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return instances
            .Select(ToConfigured)
            .Where(item => item != null)
            .Cast<ConfiguredConnector>()
            .ToArray();
    }

    public async Task<IReadOnlyList<ConnectorInstanceView>> ListInstancesAsync(CancellationToken cancellationToken)
    {
        var instances = await _db.ConnectorInstances
            .AsNoTracking()
            .OrderByDescending(item => item.AddTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return instances.Select(ToView).ToArray();
    }

    public async Task<(bool Success, string Message, string? InstanceId)> SaveAsync(
        ConnectorSaveInput input,
        CancellationToken cancellationToken)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.Name))
        {
            return (false, "请填写连接器名称。", null);
        }

        if (string.IsNullOrWhiteSpace(input.ConnectorKey) || !_providers.TryGetValue(input.ConnectorKey.Trim(), out var provider))
        {
            return (false, "没有找到这个连接器类型。请确认对应模块已经安装。", null);
        }

        var descriptor = provider.Describe();
        var existing = string.IsNullOrWhiteSpace(input.InstanceId)
            ? null
            : await _db.ConnectorInstances.FirstOrDefaultAsync(
                item => item.PublicId == input.InstanceId,
                cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(input.InstanceId) && existing == null)
        {
            return (false, "要修改的连接器不存在。", null);
        }

        if (existing != null && !string.Equals(existing.ConnectorKey, descriptor.Key, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "已保存的连接器不能改成另一种类型。请新建一个连接器。", null);
        }

        var previous = ConnectorConfigurationCodec.Read(existing?.ConfigJson);
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in descriptor.Fields)
        {
            input.Values.TryGetValue(field.Key, out var incoming);
            incoming = incoming?.Trim() ?? string.Empty;
            if (field.Kind == ConnectorFieldKind.Secret)
            {
                if (string.IsNullOrEmpty(incoming))
                {
                    if (previous.TryGetValue(field.Key, out var stored) && !string.IsNullOrEmpty(stored))
                    {
                        merged[field.Key] = stored;
                    }
                    else if (field.Required)
                    {
                        return (false, $"请填写{field.Label}。", null);
                    }
                }
                else
                {
                    merged[field.Key] = _secrets.Protect(incoming);
                }

                continue;
            }

            if (string.IsNullOrEmpty(incoming))
            {
                if (field.Required)
                {
                    return (false, $"请填写{field.Label}。", null);
                }

                continue;
            }

            if (field.Kind == ConnectorFieldKind.Select
                && field.Options.Count > 0
                && field.Options.All(option => !string.Equals(option.Value, incoming, StringComparison.Ordinal)))
            {
                return (false, $"{field.Label}的选项无效。", null);
            }

            merged[field.Key] = incoming;
        }

        var knownActions = descriptor.Actions.Select(item => item.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var enabledActions = (input.ActionKeys ?? [])
            .Where(knownActions.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (enabledActions.Length == 0)
        {
            return (false, "请至少选择一个可以交给 AI 的动作。", null);
        }

        var entity = existing ?? new ConnectorInstance(input.Name, descriptor.Key, input.Description);
        if (existing == null)
        {
            _db.ConnectorInstances.Add(entity);
        }
        else
        {
            entity.Rename(input.Name, input.Description);
        }

        entity.SetConfiguration(
            descriptor.Key,
            ConnectorConfigurationCodec.Write(merged),
            ConnectorConfigurationCodec.WriteList(enabledActions),
            input.Enabled);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "[Connector] saved instance={InstanceId} type={ConnectorKey} enabled={Enabled} actions={ActionCount}",
            entity.PublicId,
            entity.ConnectorKey,
            entity.Enabled,
            enabledActions.Length);
        return (true, "连接器已保存。", entity.PublicId);
    }

    public async Task<(bool Success, string Message)> DeleteAsync(string instanceId, CancellationToken cancellationToken)
    {
        var entity = await _db.ConnectorInstances
            .FirstOrDefaultAsync(item => item.PublicId == instanceId, cancellationToken)
            .ConfigureAwait(false);
        if (entity == null)
        {
            return (false, "连接器不存在。");
        }

        entity.Flag = true;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("[Connector] deleted instance={InstanceId}", instanceId);
        return (true, "连接器已删除。");
    }

    public async Task<ConnectorTestResult> TestAsync(string instanceId, CancellationToken cancellationToken)
    {
        var entity = await _db.ConnectorInstances
            .FirstOrDefaultAsync(item => item.PublicId == instanceId, cancellationToken)
            .ConfigureAwait(false);
        if (entity == null || !_providers.TryGetValue(entity.ConnectorKey, out var provider))
        {
            return new ConnectorTestResult(false, "连接器不存在或类型未安装。");
        }

        ConnectorTestResult result;
        try
        {
            result = await provider.TestAsync(Unprotect(entity), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "[Connector] test failed instance={InstanceId}", instanceId);
            result = new ConnectorTestResult(false, "测试连接失败，请检查账号和密钥。");
        }

        entity.RecordTest(result.Success, result.Message);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"[Connector] test instance={instanceId} success={result.Success} message={result.Message}");
        return result;
    }

    public async Task<ConnectorInvocationResult> InvokeAsync(
        string instanceId,
        string actionKey,
        IReadOnlyDictionary<string, string?> arguments,
        CancellationToken cancellationToken)
    {
        var entity = await _db.ConnectorInstances
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.PublicId == instanceId, cancellationToken)
            .ConfigureAwait(false);
        if (entity == null || !entity.Enabled)
        {
            return ConnectorInvocationResult.Fail("连接器不存在或未启用。");
        }

        if (!_providers.TryGetValue(entity.ConnectorKey, out var provider))
        {
            return ConnectorInvocationResult.Fail("连接器类型未安装。");
        }

        var descriptor = provider.Describe();
        var action = descriptor.Actions.FirstOrDefault(item =>
            string.Equals(item.Key, actionKey, StringComparison.OrdinalIgnoreCase));
        if (action == null)
        {
            return ConnectorInvocationResult.Fail("这个连接器没有所请求的动作。");
        }

        var enabledActions = ConnectorConfigurationCodec.ReadList(entity.EnabledActionsJson);
        if (enabledActions.Count > 0
            && enabledActions.All(item => !string.Equals(item, action.Key, StringComparison.OrdinalIgnoreCase)))
        {
            return ConnectorInvocationResult.Fail("这个动作没有在连接器里启用。");
        }

        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in arguments ?? new Dictionary<string, string?>())
        {
            if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            {
                normalized[pair.Key] = pair.Value.Trim();
            }
        }

        Console.WriteLine($"[Connector] invoke instance={entity.PublicId} action={action.Key} args={normalized.Count}");
        try
        {
            var result = await provider.InvokeAsync(new ConnectorInvocationContext
            {
                InstanceId = entity.PublicId,
                InstanceName = entity.Name,
                ActionKey = action.Key,
                Configuration = Unprotect(entity),
                Arguments = normalized
            }, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation(
                "[Connector] invoke finished instance={InstanceId} action={ActionKey} success={Success}",
                entity.PublicId,
                action.Key,
                result.Success);
            return result;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "[Connector] invoke failed instance={InstanceId} action={ActionKey}", entity.PublicId, action.Key);
            return ConnectorInvocationResult.Fail("连接器调用失败，请稍后重试或检查账号权限。");
        }
    }

    public ConnectorDescriptor? FindDescriptor(string connectorKey)
        => _providers.TryGetValue(connectorKey ?? string.Empty, out var provider) ? provider.Describe() : null;

    private Dictionary<string, string> Unprotect(ConnectorInstance entity)
    {
        var values = ConnectorConfigurationCodec.Read(entity.ConfigJson);
        if (!_providers.TryGetValue(entity.ConnectorKey, out var provider))
        {
            return values;
        }

        foreach (var field in provider.Describe().Fields.Where(item => item.Kind == ConnectorFieldKind.Secret))
        {
            if (values.TryGetValue(field.Key, out var stored) && ConnectorSecretProtector.IsProtected(stored))
            {
                values[field.Key] = _secrets.Unprotect(stored);
            }
        }

        return values;
    }

    private ConfiguredConnector? ToConfigured(ConnectorInstance entity)
    {
        if (!_providers.TryGetValue(entity.ConnectorKey, out var provider))
        {
            return null;
        }

        var descriptor = provider.Describe();
        var enabled = ConnectorConfigurationCodec.ReadList(entity.EnabledActionsJson);
        var actions = descriptor.Actions
            .Where(action => enabled.Count == 0
                || enabled.Any(item => string.Equals(item, action.Key, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        return new ConfiguredConnector
        {
            InstanceId = entity.PublicId,
            Name = entity.Name,
            ConnectorKey = descriptor.Key,
            ConnectorName = descriptor.Name,
            Description = string.IsNullOrWhiteSpace(entity.Description) ? descriptor.Description : entity.Description,
            Actions = actions
        };
    }

    private ConnectorInstanceView ToView(ConnectorInstance entity)
    {
        var descriptor = FindDescriptor(entity.ConnectorKey);
        var values = ConnectorConfigurationCodec.Read(entity.ConfigJson);
        var publicValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var secretFields = new List<string>();
        foreach (var field in descriptor?.Fields ?? [])
        {
            values.TryGetValue(field.Key, out var stored);
            if (field.Kind == ConnectorFieldKind.Secret)
            {
                if (!string.IsNullOrEmpty(stored))
                {
                    secretFields.Add(field.Key);
                }

                continue;
            }

            if (!string.IsNullOrEmpty(stored))
            {
                publicValues[field.Key] = stored;
            }
        }

        return new ConnectorInstanceView
        {
            InstanceId = entity.PublicId,
            Name = entity.Name,
            ConnectorKey = entity.ConnectorKey,
            ConnectorName = descriptor?.Name ?? entity.ConnectorKey,
            Description = entity.Description ?? string.Empty,
            Enabled = entity.Enabled,
            ActionKeys = ConnectorConfigurationCodec.ReadList(entity.EnabledActionsJson),
            Values = publicValues,
            SecretFields = secretFields,
            LastTestSucceeded = entity.LastTestSucceeded,
            LastTestMessage = entity.LastTestMessage ?? string.Empty,
            LastTestedAt = entity.LastTestedAt
        };
    }
}

public sealed class ConnectorSaveInput
{
    public string? InstanceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ConnectorKey { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> ActionKeys { get; set; } = [];
}

public sealed class ConnectorInstanceView
{
    public string InstanceId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ConnectorKey { get; set; } = string.Empty;

    public string ConnectorName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public List<string> ActionKeys { get; set; } = [];

    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<string> SecretFields { get; set; } = [];

    public bool? LastTestSucceeded { get; set; }

    public string LastTestMessage { get; set; } = string.Empty;

    public DateTime? LastTestedAt { get; set; }
}

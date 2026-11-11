/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ConnectorContracts.cs
    文件功能描述：定义连接器类型、配置字段、动作和绑定契约
    
    
    创建标识：Senparc - 20261111
    
    修改标识：Senparc - 20261111
    修改描述：v0.1.0 定义连接器描述、字段、动作与绑定契约

----------------------------------------------------------------*/

namespace Senparc.Xncf.Connectors.Abstractions;

/// <summary>
/// 一个可安装的连接器类型。模块通过 <see cref="IConnectorProvider"/> 注册，
/// 用户再创建带账号和密钥的实例。
/// </summary>
public sealed class ConnectorDescriptor
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public string Icon { get; init; } = "fa fa-plug";

    public IReadOnlyList<ConnectorFieldDescriptor> Fields { get; init; } = [];

    public IReadOnlyList<ConnectorActionDescriptor> Actions { get; init; } = [];
}

public enum ConnectorFieldKind
{
    Text = 0,
    Secret = 1,
    Select = 2,
    TextArea = 3
}

public sealed class ConnectorFieldDescriptor
{
    public required string Key { get; init; }

    public required string Label { get; init; }

    public string Description { get; init; } = string.Empty;

    public ConnectorFieldKind Kind { get; init; } = ConnectorFieldKind.Text;

    public bool Required { get; init; }

    public string Placeholder { get; init; } = string.Empty;

    public IReadOnlyList<ConnectorOption> Options { get; init; } = [];
}

public sealed class ConnectorOption
{
    public required string Value { get; init; }

    public required string Label { get; init; }
}

public sealed class ConnectorActionDescriptor
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public IReadOnlyList<ConnectorParameterDescriptor> Parameters { get; init; } = [];
}

public sealed class ConnectorParameterDescriptor
{
    public required string Key { get; init; }

    public required string Label { get; init; }

    public string Description { get; init; } = string.Empty;

    public bool Required { get; init; }
}

public sealed class ConnectorInvocationContext
{
    public required string InstanceId { get; init; }

    public required string InstanceName { get; init; }

    public required string ActionKey { get; init; }

    public required IReadOnlyDictionary<string, string> Configuration { get; init; }

    public required IReadOnlyDictionary<string, string> Arguments { get; init; }
}

public sealed record ConnectorInvocationResult(bool Success, string Output, string? Error = null)
{
    public static ConnectorInvocationResult Ok(string output) => new(true, output);

    public static ConnectorInvocationResult Fail(string error) => new(false, string.Empty, error);
}

public sealed record ConnectorTestResult(bool Success, string Message);

/// <summary>
/// 由业务模块实现。连接器模块只保存配置并在运行时把调用交给对应提供者。
/// </summary>
public interface IConnectorProvider
{
    ConnectorDescriptor Describe();

    Task<ConnectorInvocationResult> InvokeAsync(
        ConnectorInvocationContext context,
        CancellationToken cancellationToken);

    Task<ConnectorTestResult> TestAsync(
        IReadOnlyDictionary<string, string> configuration,
        CancellationToken cancellationToken);
}

public sealed class ConfiguredConnector
{
    public required string InstanceId { get; init; }

    public required string Name { get; init; }

    public required string ConnectorKey { get; init; }

    public required string ConnectorName { get; init; }

    public required string Description { get; init; }

    public IReadOnlyList<ConnectorActionDescriptor> Actions { get; init; } = [];
}

public interface IConnectorGateway
{
    Task<IReadOnlyList<ConnectorDescriptor>> ListCatalogAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ConfiguredConnector>> ListEnabledAsync(CancellationToken cancellationToken);

    Task<ConnectorInvocationResult> InvokeAsync(
        string instanceId,
        string actionKey,
        IReadOnlyDictionary<string, string?> arguments,
        CancellationToken cancellationToken);
}

/// <summary>
/// Agent 模板上的连接器授权。空的 <see cref="ActionKeys"/> 表示允许该实例的全部动作。
/// </summary>
public sealed class AgentConnectorBinding
{
    public string InstanceId { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public List<string> ActionKeys { get; set; } = [];
}

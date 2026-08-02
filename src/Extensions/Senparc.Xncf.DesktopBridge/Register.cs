/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Register.cs
    文件功能描述：DesktopBridge XNCF 模块注册

    创建标识：Senparc - 20260725

    修改标识：Senparc - 20260726
    修改描述：v0.1.0-preview2 同步模块功能与兼容性改进

----------------------------------------------------------------*/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Ncf.Shared.Abstractions.Synchro;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.DesktopBridge.Services;

namespace Senparc.Xncf.DesktopBridge;

[XncfRegister]
public sealed partial class Register : XncfRegisterBase, IXncfRegister
{
    public const string ModuleUid = "E9A9D870-A285-4BCE-9D4D-26A219C41252";

    public override string Name => "Senparc.Xncf.DesktopBridge";

    public override string Uid => ModuleUid;

    public override string Version => "0.2.1-preview2";

    public override string MenuName => "DesktopBridge 桌面桥接";

    public override string Icon => "fa fa-desktop";

    public override string Description => "为 NCF 桌面机器人提供受保护的本机或远程状态、设备配对和 EventBus 活动流。";

    public override Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
    {
        return Task.CompletedTask;
    }

    public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
    {
        await unsinstallFunc().ConfigureAwait(false);
    }

    public override IServiceCollection AddXncfModule(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        services.AddSingleton<DesktopActivityHub>();
        services.AddSingleton<DesktopAuthorizedSyncHub>();
        services.AddSingleton<DesktopBridgeCredentialStore>();
        services.AddSingleton<DesktopBridgeTokenValidator>();
        services.AddSingleton<ISynchroProvider, DesktopBridgeSynchroProvider>();

        // 不注册开放泛型实现：NCF EventBus 的程序集扫描会把开放泛型再次登记为
        // 含泛型参数的服务描述符，导致 WebApplicationBuilder.Build() 无法创建容器。
        // 利用 IIntegrationEventHandler<in T> 的逆变能力，将同一个非泛型观察器
        // 映射到当前已加载的每一种具体事件类型，EventBusHostedService 仍是唯一读取者。
        var eventTypes = GetIntegrationEventTypes().ToArray();
        foreach (var eventType in eventTypes)
        {
            var handlerServiceType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
            if (!services.Any(descriptor =>
                    descriptor.ServiceType == handlerServiceType &&
                    descriptor.ImplementationType == typeof(DesktopActivityEventHandler)))
            {
                services.AddScoped(handlerServiceType, typeof(DesktopActivityEventHandler));
            }
        }

        return base.AddXncfModule(services, configuration, env);
    }

    private static IEnumerable<Type> GetIntegrationEventTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            // 与 NCF Web 的 EventBus 扫描范围保持一致，避免反射系统程序集或
            // 第三方动态程序集拖慢甚至阻塞 StartWebEngine。
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.FullName))
            .Where(assembly =>
            {
                var name = assembly.GetName().Name ?? string.Empty;
                return name.Contains("Senparc.Xncf.", StringComparison.Ordinal) ||
                       name.Contains("Senparc.Areas.", StringComparison.Ordinal) ||
                       name.Contains(".Xncf.", StringComparison.Ordinal);
            })
            .SelectMany(GetLoadableTypes)
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                !type.ContainsGenericParameters &&
                typeof(IIntegrationEvent).IsAssignableFrom(type))
            .Distinct();
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
        catch
        {
            // 可选模块的类型探测失败不能阻断 NCF 启动。
            return Array.Empty<Type>();
        }
    }
}

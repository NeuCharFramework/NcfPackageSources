/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: Related implementation.

    Created by: Senparc - 20260725

    Modified by: Senparc - 20260726
    Change description: v0.1.0-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.3.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.3.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20260808
    Change description: v0.4.0-preview4 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.4.8 implement connection manager and workspace file system provider

    Modified by: Senparc - 20261111
    Change description: v0.4.10 unify XNCF Register display metadata and localization resource keys

----------------------------------------------------------------*/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Ncf.Shared.Abstractions.NeuBell;
using Senparc.Ncf.Shared.Abstractions.Security;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.DesktopBridge.Services;

namespace Senparc.Xncf.DesktopBridge;

[XncfRegister]
public sealed partial class Register : XncfRegisterBase, IXncfRegister
{
    private static readonly XncfRegisterText RegisterText =
        XncfRegisterText.Create<DesktopBridgeResource>(
            "Module.DesktopBridge",
            "DesktopBridge 桌面桥接",
            "为 NCF 桌面机器人提供受保护的本机或远程状态、设备配对和 EventBus 活动流。");

    public const string ModuleUid = "E9A9D870-A285-4BCE-9D4D-26A219C41252";

    public override string Name => "Senparc.Xncf.DesktopBridge";

    public override string Uid => ModuleUid;

    public override string Version => "0.4.8";

    public override string MenuName => RegisterText.MenuName;

    public override string Icon => "fa fa-desktop";

    public override string Description => RegisterText.Description;

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
        services.AddSingleton<IDesktopBridgeSessionValidator>(serviceProvider =>
            serviceProvider.GetRequiredService<DesktopBridgeTokenValidator>());
        services.AddSingleton<DesktopAdminAuthHandoffStore>();
        services.AddSingleton<INeuBellProvider, DesktopBridgeNeuBellProvider>();

        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
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
            //Legacy comment translated to English.
            //Legacy comment translated to English.
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
            //Legacy comment translated to English.
            return Array.Empty<Type>();
        }
    }
}

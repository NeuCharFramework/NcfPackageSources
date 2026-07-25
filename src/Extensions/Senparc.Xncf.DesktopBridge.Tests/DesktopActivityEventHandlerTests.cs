using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.DesktopBridge.Services;

namespace Senparc.Xncf.DesktopBridge.Tests;

[TestClass]
public sealed class DesktopActivityEventHandlerTests
{
    [TestMethod]
    public async Task CoreStyleAssemblyScan_AndDesktopBridgeRegistration_BuildProviderSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        RegisterHandlersLikeNcfEventBus(services, typeof(Register).Assembly);
        new Register().AddXncfModule(
            services,
            new ConfigurationBuilder().Build(),
            new TestHostEnvironment());

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        await using var scope = provider.CreateAsyncScope();

        var handler = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler<DemoRequestEvent>>()
            .OfType<DesktopActivityEventHandler>()
            .SingleOrDefault();

        Assert.IsNotNull(handler);
    }

    [TestMethod]
    public async Task RequestAndResponseEvents_UpdateAndCompleteSameActivity()
    {
        var hub = new DesktopActivityHub();
        var handler = new DesktopActivityEventHandler(hub);

        await handler.Handle(new DemoRequestEvent("request-42"), CancellationToken.None);
        var active = hub.GetActiveSnapshot();

        Assert.AreEqual(1, active.Count);
        Assert.AreEqual("request-42", active[0].ActivityId);
        Assert.AreEqual("Working", active[0].State);

        await handler.Handle(
            new DemoResponseEvent("request-42", Success: true, ErrorMessage: null),
            CancellationToken.None);

        Assert.AreEqual(0, hub.GetActiveSnapshot().Count);
    }

    [TestMethod]
    public async Task FailedResponse_IsTerminalAndDoesNotEscapeHandler()
    {
        var hub = new DesktopActivityHub();
        var handler = new DesktopActivityEventHandler(hub);

        await handler.Handle(new DemoRequestEvent("request-failed"), CancellationToken.None);
        await handler.Handle(
            new DemoResponseEvent("request-failed", Success: false, ErrorMessage: "expected failure"),
            CancellationToken.None);

        Assert.AreEqual(0, hub.GetActiveSnapshot().Count);
    }

    private static void RegisterHandlersLikeNcfEventBus(IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract &&
                type.GetInterfaces().Any(@interface =>
                    @interface.IsGenericType &&
                    @interface.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)));

        foreach (var implementationType in handlerTypes)
        {
            foreach (var serviceType in implementationType.GetInterfaces().Where(@interface =>
                         @interface.IsGenericType &&
                         @interface.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>)))
            {
                services.AddScoped(serviceType, implementationType);
            }
        }
    }

    private sealed record DemoRequestEvent(string RequestId) : IntegrationEvent;

    private sealed record DemoResponseEvent(string RequestId, bool Success, string? ErrorMessage) : IntegrationEvent;

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;

        public string ApplicationName { get; set; } = typeof(DesktopActivityEventHandlerTests).Assembly.FullName!;

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

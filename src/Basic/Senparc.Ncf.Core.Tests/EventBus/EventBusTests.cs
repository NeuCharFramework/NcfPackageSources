using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Senparc.Ncf.Core.EventBus;
using Senparc.Ncf.Shared.Abstractions.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Tests.EventBus
{
    [TestClass]
    public class EventBusTests
    {
        [TestMethod]
        public async Task InMemoryEventBus_PublishAndHandle_ShouldWork()
        {
            // Arrange
            var services = new ServiceCollection();
            
            // 注册 EventBus
            services.AddSingleton<InMemoryEventBus>();
            services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<InMemoryEventBus>());
            
            // 注册 HostedService (作为普通类测试，不一定要作为 HostedService 运行)
            // 但为了触发 ExecuteAsync 需要手动调用 StartAsync 或类似
            services.AddSingleton<EventBusHostedService>();
            
            // 注册 Handler
            var tcs = new TaskCompletionSource<string>();
            services.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>>(sp => new TestIntegrationEventHandler(tcs));
            
            // Mock Logger
            services.AddSingleton(typeof(ILogger<>), typeof(LoggerMock<>));

            var serviceProvider = services.BuildServiceProvider();

            var eventBus = serviceProvider.GetRequiredService<InMemoryEventBus>();
            var hostedService = serviceProvider.GetRequiredService<EventBusHostedService>();

            // Act
            // 启动 HostedService (后台循环开始监听)
            var cts = new CancellationTokenSource();
            var hostedServiceTask = hostedService.StartAsync(cts.Token);

            // 发布消息
            var testMessage = "Hello EventBus";
            var integrationEvent = new TestIntegrationEvent(testMessage);
            await eventBus.PublishAsync(integrationEvent);

            // 等待处理完成 (设置超时)
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(2000));
            
            // 停止 HostedService
            cts.Cancel();
            await hostedService.StopAsync(CancellationToken.None);

            // Assert
            Assert.AreEqual(tcs.Task, completedTask, "Event handler was not triggered in time.");
            Assert.AreEqual(testMessage, tcs.Task.Result);
        }

        public record TestIntegrationEvent(string Message) : IntegrationEvent;

        public class TestIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
        {
            private readonly TaskCompletionSource<string> _tcs;

            public TestIntegrationEventHandler(TaskCompletionSource<string> tcs)
            {
                _tcs = tcs;
            }

            public Task Handle(TestIntegrationEvent @event, CancellationToken cancellationToken)
            {
                _tcs.TrySetResult(@event.Message);
                return Task.CompletedTask;
            }
        }
        
        // Simple Logger Mock class to avoid Moq setup verbosity for generic logger if needed
        public class LoggerMock<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
        }
    }
}

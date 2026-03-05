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
            var tcs = new TaskCompletionSource<(int, string)>(/*TaskCreationOptions.RunContinuationsAsynchronously*/);
            services.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>>(sp => new TestIntegrationEventHandler(tcs));

            // Mock Logger
            services.AddSingleton(typeof(ILogger<>), typeof(LoggerMock<>));

            var serviceProvider = services.BuildServiceProvider();

            var eventBus = serviceProvider.GetRequiredService<InMemoryEventBus>();
            var hostedService = serviceProvider.GetRequiredService<EventBusHostedService>();

            var dt1 = SystemTime.Now;

            //ThreadPool.SetMinThreads(200, 200);


            // Act
            // 启动 HostedService (后台循环开始监听)
            var cts = new CancellationTokenSource();
            var hostedServiceTask = hostedService.StartAsync(cts.Token);

            var dt2 = SystemTime.Now;
            Console.WriteLine($"[{SystemTime.Now.ToString("mm:ss.ffffff")}] DT2");

            // 发布消息
            //var testMessage = "Hello EventBus1";
            //var integrationEvent = new TestIntegrationEvent(1, testMessage, dt2);
            //await eventBus.PublishAsync(integrationEvent);

            var batchCount = 10000;
            for (int i = 1; i < batchCount + 1; i++)
            {
                Console.WriteLine($"========== Event{i} ==========");

                var testMessage2 = "Hello EventBus" + (i);
                var integrationEvent2 = new TestIntegrationEvent(i, testMessage2, SystemTime.Now);
                await eventBus.PublishAsync(integrationEvent2);

            }

            var dt3 = SystemTime.Now;


            // 等待处理完成 (设置超时)
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(10000));

            var dt4 = SystemTime.Now;

            while (TestIntegrationEventHandler.Int < batchCount)
            {

            }

            //Console.WriteLine($"========== Event5001 ==========");

            //var testMessage5001 = "Hello EventBus 5001";
            //var integrationEvent5001 = new TestIntegrationEvent(batchCount + 3, testMessage5001, SystemTime.Now);
            //await eventBus.PublishAsync(integrationEvent5001);

            //completedTask = await Task.WhenAny(tcs.Task, Task.Delay(10000));

            // 停止 HostedService
            cts.Cancel();
            await hostedService.StopAsync(CancellationToken.None);

            // Assert
            Assert.AreEqual(tcs.Task, completedTask, "Event handler was not triggered in time.");
            Assert.AreEqual(89, tcs.Task.Result.Item1);
            //Assert.AreEqual(testMessage + " Senparc", tcs.Task.Result.Item2);
            Assert.IsTrue(tcs.Task.Result.Item2.EndsWith(" Senparc"));
            Console.WriteLine("tcs.Task.Result： " + tcs.Task.Result);

            Console.WriteLine("DT2-DT1:" + (dt2 - dt1).TotalMilliseconds);
            Console.WriteLine("DT3-DT2:" + (dt3 - dt2).TotalMilliseconds);
            Console.WriteLine("DT4-DT3:" + (dt4 - dt3).TotalMilliseconds);
            Console.WriteLine("DT4-DT2:" + (dt4 - dt2).TotalMilliseconds);


            Console.WriteLine("Total Time: " + SystemTime.DiffTotalMS(dt1));

        }

        //Senparc.Xncf.XXX.Abstractions
        public record TestIntegrationEvent(int Index, string Message, DateTimeOffset DateTime) : IntegrationEvent;

        //Senparc.Xncf.XXX
        public class TestIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
        {
            public static int Int = 0;
            public object lockObj = new object();

            private readonly TaskCompletionSource<(int, string)> _tcs;

            public TestIntegrationEventHandler(TaskCompletionSource<(int, string)> tcs)
            {
                _tcs = tcs;
            }

            public Task Handle(TestIntegrationEvent @event, CancellationToken cancellationToken)
            {


                Console.WriteLine($"[{SystemTime.Now.ToString("mm:ss.ffffff")}] [ID:{@event.Index}] Handle Time: {SystemTime.DiffTotalMS(@event.DateTime)}");
                //_tcs.TrySetResult(@event.Message + " Senparc");
                //_tcs.TrySetResult((89, @event.Message + " Senparc"));
                //Console.WriteLine("Handle Time: " + SystemTime.DiffTotalMS(@event.DateTime));
                Interlocked.Increment(ref Int); // 使用原子操作代替 lock

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

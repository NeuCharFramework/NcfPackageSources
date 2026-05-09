using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Senparc.Ncf.Core.EventBus;
using Senparc.Ncf.Shared.Abstractions.Events;
using System;
using System.Linq;
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
            Console.WriteLine("Start Waiting");
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
                Console.WriteLine($"[{SystemTime.Now.ToString("mm:ss.ffffff")}] [ID:{@event.Index}] Handle Start Time: {SystemTime.DiffTotalMS(@event.DateTime)}");
                //_tcs.TrySetResult(@event.Message + " Senparc");
                _tcs.TrySetResult((89, @event.Message + " Senparc"));
                //Console.WriteLine("Handle Time: " + SystemTime.DiffTotalMS(@event.DateTime));
                Interlocked.Increment(ref Int); // 使用原子操作代替 lock

                return Task.CompletedTask;
            }
        }

        [TestMethod]
        public async Task EventBus_CircularReferenceDetection_ShouldPreventLoop()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(typeof(ILogger<>), typeof(LoggerMock<>));
            
            // 注册 EventBus with circular reference detection enabled
            var options = new EventBusOptions
            {
                MaxEventChainDepth = 5,
                EnableCircularReferenceDetection = true,
                EnableDuplicateDetection = false // Disable duplicate detection for this test
            };
            services.AddSingleton(options);
            services.AddSingleton<InMemoryEventBus>(sp =>
            {
                var logger = sp.GetService<ILogger<InMemoryEventBus>>();
                return new InMemoryEventBus(logger);
            });
            services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<InMemoryEventBus>());
            services.AddSingleton<EventBusHostedService>();

            var serviceProvider = services.BuildServiceProvider();
            var eventBus = serviceProvider.GetRequiredService<InMemoryEventBus>();

            // Act & Assert - Test circular reference detection in event base class
            var eventA = new TestEventA("Test");
            
            // Simulate event chain: EventA → EventB → EventA (should detect circular reference)
            var eventBMetadata = eventA.DeriveMetadata();
            var eventB = new TestEventB("Test") with 
            { 
                ParentEventId = eventBMetadata.ParentEventId,
                Depth = eventBMetadata.Depth,
                EventChain = eventBMetadata.EventChain
            };
            
            // This should detect that EventA appears in the chain again
            bool hasCircular = eventB.HasCircularReference(nameof(TestEventA));
            Assert.IsTrue(hasCircular, "Should detect circular reference: TestEventA→TestEventB→TestEventA");
            
            // Test that PublishDerivedAsync throws on circular reference
            try
            {
                await eventBus.PublishDerivedAsync(eventA, eventB);
                Assert.Fail("PublishDerivedAsync should throw on circular reference");
            }
            catch (InvalidOperationException ex)
            {
                Assert.IsTrue(ex.Message.Contains("Circular reference detected"), 
                    "Exception message should mention circular reference");
            }
        }

        [TestMethod]
        public async Task EventBus_MaxDepthLimit_ShouldStopProcessing()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton(typeof(ILogger<>), typeof(LoggerMock<>));
            
            var processedEvents = new System.Collections.Concurrent.ConcurrentBag<IIntegrationEvent>();
            
            var options = new EventBusOptions
            {
                MaxEventChainDepth = 3, // Very low limit for testing
                EnableCircularReferenceDetection = true
            };
            services.AddSingleton(options);
            services.AddSingleton<InMemoryEventBus>(sp =>
            {
                var logger = sp.GetService<ILogger<InMemoryEventBus>>();
                return new InMemoryEventBus(logger);
            });
            services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<InMemoryEventBus>());
            services.AddHostedService<EventBusHostedService>();
            
            // Register handler that publishes derived events
            services.AddScoped<IIntegrationEventHandler<TestEventA>>(sp => 
                new RecursiveEventHandler<TestEventA, TestEventB>(
                    sp.GetRequiredService<IEventBus>(), 
                    processedEvents));

            var serviceProvider = services.BuildServiceProvider();
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();
            var hostedService = serviceProvider.GetService<Microsoft.Extensions.Hosting.IHostedService>();

            // Start the hosted service
            var cts = new CancellationTokenSource();
            await hostedService.StartAsync(cts.Token);

            // Act - Publish a root event
            var rootEvent = new TestEventA("Root");
            await eventBus.PublishAsync(rootEvent);

            // Wait for processing
            await Task.Delay(2000);

            // Stop the service
            cts.Cancel();
            await hostedService.StopAsync(CancellationToken.None);

            // Assert - Events beyond depth limit should not be processed
            var maxDepth = processedEvents.ToList().Max(e => e.Depth);
            Assert.IsTrue(maxDepth < options.MaxEventChainDepth, 
                $"Max depth {maxDepth} should be less than limit {options.MaxEventChainDepth}");
        }
        
        // Test event types
        public record TestEventA(string Data) : IntegrationEvent;
        public record TestEventB(string Data) : IntegrationEvent;
        
        // Recursive handler for testing depth limits
        public class RecursiveEventHandler<TEventIn, TEventOut> : IIntegrationEventHandler<TEventIn>
            where TEventIn : IntegrationEvent
            where TEventOut : IntegrationEvent
        {
            private readonly IEventBus _eventBus;
            private readonly System.Collections.Concurrent.ConcurrentBag<IIntegrationEvent> _processedEvents;
            
            public RecursiveEventHandler(
                IEventBus eventBus, 
                System.Collections.Concurrent.ConcurrentBag<IIntegrationEvent> processedEvents)
            {
                _eventBus = eventBus;
                _processedEvents = processedEvents;
            }
            
            public async Task Handle(TEventIn @event, CancellationToken cancellationToken)
            {
                _processedEvents.Add(@event);
                
                // Publish a derived event to test recursion
                var derivedEvent = (TEventOut)Activator.CreateInstance(typeof(TEventOut), "Derived");
                await _eventBus.PublishDerivedAsync(derivedEvent, @event);
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

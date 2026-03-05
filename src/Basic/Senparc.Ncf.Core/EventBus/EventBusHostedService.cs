using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.EventBus;
using System;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Ncf.Shared.Abstractions.Events;
using System.Collections.Generic;

namespace Senparc.Ncf.Core.EventBus
{
    /// <summary>
    /// 后台消息泵：负责从 Channel 读取事件并分发给 Handler
    /// </summary>
    public class EventBusHostedService : BackgroundService
    {
        private readonly InMemoryEventBus _eventBus;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventBusHostedService> _logger;

        public EventBusHostedService(
            InMemoryEventBus eventBus,
            IServiceProvider serviceProvider,
            ILogger<EventBusHostedService> logger)
        {
            _eventBus = eventBus;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Senparc NCF EventBus Service is starting.");

            Console.WriteLine($"[{DateTime.Now.ToString("mm:ss.ffffff")}] Read Start");
            var tasks = new List<Task>();
            // 关键：限制最大并发数为 20（根据你的机器性能调整）
            // 这个数字通常建议：数据库连接池大小 / 2
            //using var semaphore = new SemaphoreSlim(10);

            await foreach (var @event in _eventBus.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {

                    //var t = Task.Run(async () =>
                    // {
                    //     try
                    //     {
                             //var guid = Guid.NewGuid().ToString("N").Substring(0, 8);
                             //Console.WriteLine($"[{DateTime.Now.ToString("mm:ss.ffffff")} {guid}] Received event: {@event.GetType().Name} (Id: {@event.Id})");

                             //Console.WriteLine($"[{DateTime.Now.ToString("mm:ss.ffffff")} {guid}] Start Process");
                             //await semaphore.WaitAsync(stoppingToken);
                             ProcessEvent(@event, stoppingToken);
                             //Console.WriteLine($"[{DateTime.Now.ToString("mm:ss.ffffff")} {guid}] End Process");
                    //     }
                    //     finally {
                    //         // 2. 任务处理完（无论成功失败），释放信号量，让下一个事件进来
                    //         //semaphore.Release();
                    //     }
                    // });
                    //tasks.Add(t);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing event {EventId}", @event.Id);
                }
            }
        }

        private async Task ProcessEvent(IIntegrationEvent @event, CancellationToken ct)
        {
            // 创建 Scope 以支持 Scoped 服务注入（如 DbContext, Repository）
            using var scope = _serviceProvider.CreateScope();

            // 动态查找：IIntegrationEventHandler<MyEvent>
            var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(@event.GetType());
            var handlers = scope.ServiceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                var method = handler.GetType().GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.Handle));
                if (method != null)
                {
                    await (Task)method.Invoke(handler, new object[] { @event, ct });
                }
            }
        }
    }
}
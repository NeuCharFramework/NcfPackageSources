using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.EventBus;
using Senparc.Ncf.Shared.Abstractions.Events;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Tests.EventBus
{
    [TestClass]
    public class EventBusRequestClientTests
    {
        [TestMethod]
        public void AddSenparcEventBus_ShouldExposeSameSingletonThroughBothPublicInterfaces()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSenparcEventBus(
                options => options.MaxRequestTimeout = TimeSpan.FromSeconds(2),
                typeof(EventBusRequestClientTests).Assembly);

            using var serviceProvider = services.BuildServiceProvider();
            var eventBus = serviceProvider.GetRequiredService<IEventBus>();
            var requestClient = serviceProvider.GetRequiredService<IEventBusRequestClient>();

            Assert.AreSame<object>(eventBus, requestClient);
        }

        [TestMethod]
        public async Task RequestAsync_ShouldReturnTypedDerivedResponse()
        {
            await using var fixture = CreateFixture();
            await fixture.StartAsync();

            try
            {
                var request = new RoundTripRequest("hello");

                var response = await fixture.RequestClient.RequestAsync(
                    request,
                    TimeSpan.FromSeconds(2));

                Assert.AreEqual(request.RequestId, response.RequestId);
                Assert.AreEqual("HELLO", response.Value);
                Assert.AreEqual(request.Id, response.ParentEventId);
                Assert.AreEqual(1, response.Depth);
                Assert.AreEqual(nameof(RoundTripRequest), response.EventChain);
            }
            finally
            {
                await fixture.StopAsync();
            }
        }

        [TestMethod]
        public async Task RequestAsync_ShouldCleanUpAfterTimeoutAndAllowRequestIdReuse()
        {
            await using var fixture = CreateFixture();
            await fixture.StartAsync();

            try
            {
                var request = new UnhandledRequest();

                await Assert.ThrowsExceptionAsync<TimeoutException>(() =>
                    fixture.RequestClient.RequestAsync<UnhandledResponse>(
                        request,
                        TimeSpan.FromMilliseconds(50)));

                var reusedRequest = new UnhandledRequest { RequestId = request.RequestId };
                await Assert.ThrowsExceptionAsync<TimeoutException>(() =>
                    fixture.RequestClient.RequestAsync<UnhandledResponse>(
                        reusedRequest,
                        TimeSpan.FromMilliseconds(50)));
            }
            finally
            {
                await fixture.StopAsync();
            }
        }

        [TestMethod]
        public async Task RequestAsync_ShouldHonorCancellationAndCleanUp()
        {
            await using var fixture = CreateFixture();
            await fixture.StartAsync();

            try
            {
                var request = new UnhandledRequest();
                using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

                await Assert.ThrowsExceptionAsync<TaskCanceledException>(() =>
                    fixture.RequestClient.RequestAsync<UnhandledResponse>(
                        request,
                        TimeSpan.FromSeconds(2),
                        cancellation.Token));

                var reusedRequest = new UnhandledRequest { RequestId = request.RequestId };
                await Assert.ThrowsExceptionAsync<TimeoutException>(() =>
                    fixture.RequestClient.RequestAsync<UnhandledResponse>(
                        reusedRequest,
                        TimeSpan.FromMilliseconds(50)));
            }
            finally
            {
                await fixture.StopAsync();
            }
        }

        [TestMethod]
        public async Task RequestAsync_ShouldSupportConcurrentRequests()
        {
            await using var fixture = CreateFixture();
            await fixture.StartAsync();

            try
            {
                var requests = Enumerable.Range(1, 32)
                    .Select(index => new RoundTripRequest($"value-{index}"))
                    .ToArray();

                var responses = await Task.WhenAll(requests.Select(request =>
                    fixture.RequestClient.RequestAsync(request, TimeSpan.FromSeconds(2))));

                Assert.AreEqual(requests.Length, responses.Length);
                Assert.AreEqual(requests.Length, responses.Select(z => z.RequestId).Distinct().Count());
                CollectionAssert.AreEquivalent(
                    requests.Select(z => z.Value.ToUpperInvariant()).ToArray(),
                    responses.Select(z => z.Value).ToArray());
            }
            finally
            {
                await fixture.StopAsync();
            }
        }

        [TestMethod]
        public async Task RequestAsync_ShouldIgnoreWrongResponseTypeAndWaitForExpectedType()
        {
            await using var fixture = CreateFixture();
            await fixture.StartAsync();

            try
            {
                var request = new WrongThenCorrectRequest("safe");

                var response = await fixture.RequestClient.RequestAsync(
                    request,
                    TimeSpan.FromSeconds(2));

                Assert.AreEqual(request.RequestId, response.RequestId);
                Assert.AreEqual("safe-correct", response.Value);
            }
            finally
            {
                await fixture.StopAsync();
            }
        }

        [TestMethod]
        public async Task RequestAsync_ShouldRejectDuplicatePendingRequestIdAndUnsafeTimeouts()
        {
            await using var fixture = CreateFixture(maxRequestTimeout: TimeSpan.FromSeconds(2));
            await fixture.StartAsync();

            try
            {
                var request = new UnhandledRequest();
                var firstRequest = fixture.RequestClient.RequestAsync<UnhandledResponse>(
                    request,
                    TimeSpan.FromSeconds(1));

                await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                    fixture.RequestClient.RequestAsync<UnhandledResponse>(
                        new UnhandledRequest { RequestId = request.RequestId },
                        TimeSpan.FromSeconds(1)));

                await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() =>
                    fixture.RequestClient.RequestAsync<UnhandledResponse>(
                        new UnhandledRequest(),
                        Timeout.InfiniteTimeSpan));

                await Assert.ThrowsExceptionAsync<ArgumentOutOfRangeException>(() =>
                    fixture.RequestClient.RequestAsync<UnhandledResponse>(
                        new UnhandledRequest(),
                        TimeSpan.FromSeconds(3)));

                await Assert.ThrowsExceptionAsync<TimeoutException>(async () => await firstRequest);
            }
            finally
            {
                await fixture.StopAsync();
            }
        }

        private static EventBusFixture CreateFixture(TimeSpan? maxRequestTimeout = null)
        {
            var services = new ServiceCollection();
            var options = new EventBusOptions
            {
                EnableDuplicateDetection = true,
                EnableCircularReferenceDetection = true,
                RetryOnFailure = false,
                MaxRequestTimeout = maxRequestTimeout ?? TimeSpan.FromSeconds(5)
            };

            services.AddSingleton(options);
            services.AddSingleton(typeof(ILogger<>), typeof(TestLogger<>));
            services.AddSingleton<InMemoryEventBus>(sp => new InMemoryEventBus(
                sp.GetRequiredService<ILogger<InMemoryEventBus>>(),
                sp.GetRequiredService<EventBusOptions>()));
            services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<InMemoryEventBus>());
            services.AddSingleton<IEventBusRequestClient>(sp => sp.GetRequiredService<InMemoryEventBus>());
            services.AddSingleton<EventBusHostedService>();
            services.AddScoped<IIntegrationEventHandler<RoundTripRequest>, RoundTripRequestHandler>();
            services.AddScoped<IIntegrationEventHandler<WrongThenCorrectRequest>, WrongThenCorrectRequestHandler>();

            return new EventBusFixture(services.BuildServiceProvider());
        }

        private sealed record RoundTripRequest(string Value) : IntegrationRequest<RoundTripResponse>;

        private sealed record RoundTripResponse(Guid RequestId, string Value)
            : IntegrationResponse(RequestId);

        private sealed record UnhandledRequest : IntegrationRequest<UnhandledResponse>;

        private sealed record UnhandledResponse(Guid RequestId) : IntegrationResponse(RequestId);

        private sealed record WrongThenCorrectRequest(string Value)
            : IntegrationRequest<WrongThenCorrectResponse>;

        private sealed record WrongThenCorrectResponse(Guid RequestId, string Value)
            : IntegrationResponse(RequestId);

        private sealed record WrongResponse(Guid RequestId) : IntegrationResponse(RequestId);

        private sealed class RoundTripRequestHandler : IIntegrationEventHandler<RoundTripRequest>
        {
            private readonly IEventBus _eventBus;

            public RoundTripRequestHandler(IEventBus eventBus)
            {
                _eventBus = eventBus;
            }

            public async Task Handle(RoundTripRequest @event, CancellationToken cancellationToken)
            {
                var response = new RoundTripResponse(
                    @event.RequestId,
                    @event.Value.ToUpperInvariant());

                await _eventBus.PublishDerivedAsync(response, @event, cancellationToken);
            }
        }

        private sealed class WrongThenCorrectRequestHandler : IIntegrationEventHandler<WrongThenCorrectRequest>
        {
            private readonly IEventBus _eventBus;

            public WrongThenCorrectRequestHandler(IEventBus eventBus)
            {
                _eventBus = eventBus;
            }

            public async Task Handle(WrongThenCorrectRequest @event, CancellationToken cancellationToken)
            {
                await _eventBus.PublishDerivedAsync(
                    new WrongResponse(@event.RequestId),
                    @event,
                    cancellationToken);

                await _eventBus.PublishDerivedAsync(
                    new WrongThenCorrectResponse(@event.RequestId, $"{@event.Value}-correct"),
                    @event,
                    cancellationToken);
            }
        }

        private sealed class EventBusFixture : IAsyncDisposable
        {
            private readonly ServiceProvider _serviceProvider;
            private readonly EventBusHostedService _hostedService;
            private readonly CancellationTokenSource _stopping = new();

            public EventBusFixture(ServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
                _hostedService = serviceProvider.GetRequiredService<EventBusHostedService>();
                RequestClient = serviceProvider.GetRequiredService<IEventBusRequestClient>();
            }

            public IEventBusRequestClient RequestClient { get; }

            public Task StartAsync() => _hostedService.StartAsync(_stopping.Token);

            public async Task StopAsync()
            {
                if (!_stopping.IsCancellationRequested)
                {
                    _stopping.Cancel();
                    await _hostedService.StopAsync(CancellationToken.None);
                }
            }

            public async ValueTask DisposeAsync()
            {
                await StopAsync();
                _stopping.Dispose();
                await _serviceProvider.DisposeAsync();
            }
        }

        private sealed class TestLogger<T> : ILogger<T>
        {
            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception exception,
                Func<TState, Exception, string> formatter)
            {
            }
        }
    }
}

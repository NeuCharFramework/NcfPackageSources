using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Reflection;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Authorization;
using Senparc.Xncf.AIKernel.Domain.Models.FineTuning;
using Senparc.Xncf.AIKernel.OHS.Local.AppService;
using Senparc.Xncf.AreaBase.Admin.Filters;

namespace Senparc.Xncf.AIKernel.Tests;

[TestClass]
public class FineTuningWorkerClientTests
{
    private const string Key = "test-worker-key-at-least-32-characters";

    [TestMethod]
    public async Task TypedClientRegistration_UsesDatabaseConfigurationConstructor()
    {
        var handler = new StubHandler((request, token) =>
        {
            Assert.AreEqual("http://127.0.0.1:8091/health", request.RequestUri!.AbsoluteUri);
            Assert.AreEqual(Key, request.Headers.GetValues("X-NCF-Worker-Key").Single());
            return Task.FromResult(Json("""{"status":"ok","version":"1","capabilities":[],"activeJobs":0,"queuedJobs":0}"""));
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<FineTuningWorkerOptions>(_ => { });
        services.AddScoped<IFineTuningWorkerConfigurationProvider, TestConfigurationProvider>();
        services.AddHttpClient<FineTuningWorkerClient>().ConfigurePrimaryHttpMessageHandler(() => handler);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<FineTuningWorkerClient>();
        Assert.AreEqual("ok", (await client.HealthAsync("cpu-lab")).Status);
    }

    [TestMethod]
    public void WorkerSecrets_OnlyValidConfiguredAliasesAreSelectable()
    {
        var options = new FineTuningWorkerOptions
        {
            Enabled = true,
            AllowedHosts = ["127.0.0.1"],
            WorkerApiKeys = new()
            {
                ["cpu-lab"] = Key, ["blank"] = "", ["short"] = "short",
                ["newline"] = Key + "\n", ["leading"] = " " + Key, ["trailing"] = Key + " "
            }
        };
        CollectionAssert.AreEqual(new[] { "cpu-lab" }, options.ConfiguredWorkerAliases.ToArray());
        foreach (var alias in new[] { "blank", "short", "newline", "leading", "trailing", "missing" })
            Assert.ThrowsException<NcfExceptionBase>(() => options.GetWorkerApiKey(alias));
        Assert.AreEqual(Key, options.GetWorkerApiKey("cpu-lab"));
        Assert.ThrowsException<NcfExceptionBase>(() =>
            options.GetValidatedEndpoint("cpu-lab", "http://not-allowed.internal/", 30));
        Assert.AreEqual("127.0.0.1",
            options.GetValidatedEndpoint("cpu-lab", "http://127.0.0.1:8091", 180).Host);
    }

    private static FineTuningWorkerClient Client(HttpMessageHandler handler, bool enabled = true) =>
        new(new HttpClient(handler), Options.Create(new FineTuningWorkerOptions
        {
            Enabled = enabled,
            WorkerEndpoint = "http://127.0.0.1:8091/",
            WorkerApiKey = Key
        }), NullLogger<FineTuningWorkerClient>.Instance);

    [TestMethod]
    public async Task Disabled_FailsWithoutSendingRequest()
    {
        var handler = new StubHandler((request, token) => throw new AssertFailedException("Disabled client must not send."));
        var error = await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => Client(handler, false).HealthAsync());
        Assert.IsTrue(error.Message.Contains("FineTuning", StringComparison.OrdinalIgnoreCase)
            || error.Message.Contains("fine-tuning", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public async Task Health_UsesPrivateAuthenticationAndDeserializesCamelCase()
    {
        var handler = new StubHandler((request, token) =>
        {
            Assert.AreEqual("http://127.0.0.1:8091/health", request.RequestUri!.AbsoluteUri);
            Assert.AreEqual(Key, request.Headers.GetValues("X-NCF-Worker-Key").Single());
            return Task.FromResult(Json("""{"status":"ok","version":"1","capabilities":[{"backend":"cpu","available":true,"methods":["lora"]}],"activeJobs":1,"queuedJobs":2}"""));
        });
        var health = await Client(handler).HealthAsync();
        Assert.AreEqual(2, health.QueuedJobs);
        Assert.IsTrue(health.Capabilities[0].Available);
    }

    [TestMethod]
    public async Task CatalogPages_UseNamedWorker_ValidateBoundsAndRejectInconsistentCounts()
    {
        var handler = new StubHandler((request, token) =>
        {
            Assert.AreEqual("/jobs/page", request.RequestUri!.AbsolutePath);
            Assert.AreEqual("?offset=100&limit=10", request.RequestUri.Query);
            Assert.AreEqual(Key, request.Headers.GetValues("X-NCF-Worker-Key").Single());
            return Task.FromResult(Json("""{"items":[],"offset":100,"limit":10,"total":100}"""));
        });
        var client = new FineTuningWorkerClient(new HttpClient(handler), new TestConfigurationProvider(),
            NullLogger<FineTuningWorkerClient>.Instance);
        var page = await client.GetJobsPageAsync("cpu-lab", 100, 10);
        Assert.AreEqual(100, page.Total);
        Assert.AreEqual(0, page.Items.Count);
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => client.GetJobsPageAsync("cpu-lab", -1, 10));
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => client.GetDatasetsPageAsync("cpu-lab", 0, 201));
        var inconsistent = new StubHandler((request, token) =>
            Task.FromResult(Json("""{"items":[],"offset":0,"limit":10,"total":1}""")));
        var invalid = new FineTuningWorkerClient(new HttpClient(inconsistent), new TestConfigurationProvider(),
            NullLogger<FineTuningWorkerClient>.Instance);
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => invalid.GetJobsPageAsync("cpu-lab", 0, 10));
    }

    [TestMethod]
    public async Task CreateJob_SendsAllTrainingParametersAndReadsNullMetrics()
    {
        var handler = new StubHandler(async (request, token) =>
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.AreEqual("base-model", body.RootElement.GetProperty("modelId").GetString());
            Assert.AreEqual(16, body.RootElement.GetProperty("loraAlpha").GetInt32());
            Assert.AreEqual(42, body.RootElement.GetProperty("seed").GetInt32());
            return Json("""{"id":"job-1","name":"Test","state":"Queued","createdUtc":"2026-10-01T19:00:00Z","artifacts":[],"lastEventSequence":0,"latestMetrics":{"gpuMemoryMb":null},"request":{"modelId":"base-model"}}""");
        });
        var result = await Client(handler).CreateJobAsync(new FineTuningJobRequest
        {
            Name = "Test",
            ModelId = "base-model",
            DatasetId = "dataset-1"
        });
        Assert.AreEqual("Queued", result.State);
        Assert.IsNull(result.LatestMetrics["gpuMemoryMb"]);
    }

    [TestMethod]
    public async Task InvalidParameters_AndDatasetLeakage_AreRejectedBeforeSending()
    {
        var handler = new StubHandler((request, token) => throw new AssertFailedException("Invalid client must not send."));
        var client = Client(handler);
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => client.CreateJobAsync(new FineTuningJobRequest
        {
            Name = "test", ModelId = "model", DatasetId = "data", LearningRate = double.NaN
        }));
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => client.CreateJobAsync(new FineTuningJobRequest
        {
            Name = "test", ModelId = "model", DatasetId = "data", EvalDatasetId = "data"
        }));
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => client.CreateJobAsync(new FineTuningJobRequest
        {
            Name = "test", ModelId = "model", DatasetId = "data", Method = "qlora", Backend = "cpu"
        }));
    }

    [TestMethod]
    public async Task DatasetLimit_IsUtf8Bytes_NotCharacterCount()
    {
        var handler = new StubHandler((request, token) => throw new AssertFailedException("Oversize dataset must not send."));
        var error = await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => Client(handler).UploadDatasetAsync(
            new FineTuningDatasetRequest { Name = "big", Content = new string('\u4e2d', 700000) }));
        Assert.IsTrue(error.Message.Contains("2 MiB", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Events_HaveBoundedCursorAndPreserveMetrics()
    {
        var handler = new StubHandler((request, token) =>
        {
            Assert.AreEqual("?after=42&limit=100", request.RequestUri!.Query);
            return Task.FromResult(Json("""{"events":[{"sequence":43,"timestampUtc":"2026-10-01T19:00:00Z","kind":"metrics","metrics":{"loss":1.25,"step":2,"totalSteps":10}}],"nextCursor":43,"hasMore":true}"""));
        });
        var client = Client(handler);
        var page = await client.GetEventsAsync("job-1", 42, 100);
        Assert.AreEqual(43L, page.NextCursor);
        Assert.AreEqual(1.25, page.Events[0].Metrics["loss"]);
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => client.GetEventsAsync("job-1", -1, 100));
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => client.GetEventsAsync("job-1", 0, 201));
    }

    [TestMethod]
    public void Identifiers_RejectTraversalAndInjectedQueries()
    {
        foreach (var id in new[] { "../model", "job?after=0", "a/b", "a\\b", "", "a..b" })
            Assert.ThrowsException<NcfExceptionBase>(() => FineTuningWorkerClient.Identifier(id));
        Assert.AreEqual("model_1-2.0", FineTuningWorkerClient.Identifier("model_1-2.0"));
    }

    [TestMethod]
    public async Task WorkerError_IsExplicit_NotSuccessShaped()
    {
        var handler = new StubHandler((request, token) =>
            Task.FromResult(Json("""{"detail":"CUDA is unavailable"}""", HttpStatusCode.UnprocessableEntity)));
        var error = await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => Client(handler).HealthAsync());
        StringAssert.Contains(error.Message, "422");
        StringAssert.Contains(error.Message, "CUDA is unavailable");
    }

    [TestMethod]
    public async Task ArtifactDownload_DoesNotBufferArchive_AndCallerOwnsResponse()
    {
        var stream = new ReadTrackingStream([1, 2, 3]);
        var handler = new StubHandler((request, token) =>
        {
            Assert.AreEqual("/jobs/job-1/artifacts/adapter.zip", request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) });
        });
        using (var response = await Client(handler).DownloadArtifactAsync("job-1", "adapter.zip"))
        {
            Assert.AreEqual(0, stream.ReadCount);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, await response.Content.ReadAsByteArrayAsync());
            Assert.IsTrue(stream.ReadCount > 0);
        }
        Assert.IsTrue(stream.Disposed);
    }

    [TestMethod]
    public async Task MalformedJson_AndConnectionFailure_AreExplicit()
    {
        var malformed = new StubHandler((request, token) => Task.FromResult(Json("not json")));
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => Client(malformed).HealthAsync());
        var missingContract = new StubHandler((request, token) => Task.FromResult(Json("{}")));
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => Client(missingContract).HealthAsync());
        var unreachable = new StubHandler((request, token) => throw new HttpRequestException("Connection refused"));
        await Assert.ThrowsExceptionAsync<NcfExceptionBase>(() => Client(unreachable).HealthAsync());
    }

    [TestMethod]
    public async Task Cancellation_DoesNotTurnIntoWorkerFailure()
    {
        var handler = new StubHandler(async (request, token) =>
        {
            await Task.Delay(Timeout.Infinite, token);
            return Json("{}");
        });
        using var cancel = new CancellationTokenSource();
        var pending = Client(handler).HealthAsync(cancel.Token);
        cancel.Cancel();
        try
        {
            await pending;
            Assert.Fail("Expected cancellation.");
        }
        catch (OperationCanceledException)
        {
            Assert.IsTrue(cancel.IsCancellationRequested);
        }
    }

    [TestMethod]
    public void Configuration_RejectsMissingKeyAndNonHttpEndpoints()
    {
        Assert.ThrowsException<NcfExceptionBase>(() => new FineTuningWorkerOptions
        {
            Enabled = true, WorkerApiKey = Key, WorkerEndpoint = "file:///tmp/worker"
        }.GetValidatedEndpoint());
        Assert.ThrowsException<NcfExceptionBase>(() => new FineTuningWorkerOptions { Enabled = true }.GetValidatedEndpoint());
        Assert.ThrowsException<NcfExceptionBase>(() => new FineTuningWorkerOptions
        {
            Enabled = true, WorkerApiKey = Key, WorkerEndpoint = "http://user:password@localhost/"
        }.GetValidatedEndpoint());
    }

    [TestMethod]
    public void AdministrationApi_RequiresAdminOnlyPolicy()
    {
        var attribute = typeof(AIFineTuningAppService).GetCustomAttributes(typeof(ApiAuthorizeAttribute), true)
            .Cast<ApiAuthorizeAttribute>().Single();
        Assert.AreEqual(NcfAuthorizationPolicyNames.AdminOnly, attribute.Policy);
    }

    [TestMethod]
    public void MutationApiBinding_PutsBodyDtoFirstAndNamedWorkerInQueryPosition()
    {
        foreach (var name in new[] { nameof(AIFineTuningAppService.UploadDatasetAsync),
            nameof(AIFineTuningAppService.CreateJobAsync), nameof(AIFineTuningAppService.CancelJobAsync) })
        {
            var parameters = typeof(AIFineTuningAppService).GetMethod(name)!.GetParameters();
            Assert.AreEqual(2, parameters.Length);
            Assert.IsTrue(parameters[0].IsDefined(typeof(FromBodyAttribute), false));
            Assert.AreEqual("workerAlias", parameters[1].Name);
            Assert.AreEqual(typeof(string), parameters[1].ParameterType);
        }
        Assert.IsTrue(typeof(AIFineTuningAppService).GetMethod(nameof(AIFineTuningAppService.SaveWorkerAsync))!
            .GetParameters()[0].IsDefined(typeof(FromBodyAttribute), false));
    }

    [TestMethod]
    [DoNotParallelize]
    public async Task GeneratedMutationControllers_BindTypedJsonBodiesAndWorkerQueryParameters()
    {
        var category = "FineTuningBindingTest" + Guid.NewGuid().ToString("N");
        var names = new[] { nameof(AIFineTuningAppService.UploadDatasetAsync),
            nameof(AIFineTuningAppService.CreateJobAsync), nameof(AIFineTuningAppService.CancelJobAsync) };
        var entries = names.Select(name =>
        {
            var method = typeof(AIFineTuningAppService).GetMethod(name)!;
            var globalName = nameof(AIFineTuningAppService) + "." + name;
            var info = new ApiBindInfo(ApiBindOn.Method, category, globalName, globalName,
                typeof(ControllerBase), 0, method.GetCustomAttribute<ApiBindAttribute>()!, method);
            return new KeyValuePair<string, ApiBindInfo>(globalName, info);
        }).ToArray();
        WebApiEngine.ApiAssemblyNames[category] = category;
        try
        {
            var engine = new WebApiEngine(options =>
            {
                options.TaskCount = 1;
                options.UseLowerCaseApiName = false;
                options.AddApiControllerAttribute = true;
            });
            Assert.AreEqual(3, await engine.BuildWebApi(entries.GroupBy(_ => category).Single()));
            var controller = engine.GetApiAssembly(category).GetTypes().Single();
            foreach (var entry in entries)
            {
                var parameters = controller.GetMethod(entry.Value.MethodName)!.GetParameters();
                Assert.AreEqual(entry.Value.MethodInfo.GetParameters()[0].ParameterType, parameters[0].ParameterType);
                Assert.AreEqual(BindingSource.Body,
                    parameters[0].GetCustomAttributes().OfType<IBindingSourceMetadata>().Single().BindingSource);
                Assert.AreEqual("workerAlias", parameters[1].Name);
                Assert.AreEqual(BindingSource.Query,
                    parameters[1].GetCustomAttributes().OfType<IBindingSourceMetadata>().Single().BindingSource);
            }
        }
        finally
        {
            WebApiEngine.ApiAssemblyNames.TryRemove(category, out _);
            WebApiEngine.ApiAssemblyCollection.TryRemove(category, out _);
            WebApiEngine.ApiAssemblyVersions.TryRemove(category, out _);
        }
    }

    private static HttpResponseMessage Json(string content, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(content, Encoding.UTF8, "application/json") };

    private sealed class TestConfigurationProvider : IFineTuningWorkerConfigurationProvider
    {
        public Task<FineTuningWorkerConfiguration> GetAsync(string workerAlias, CancellationToken cancellationToken = default)
        {
            Assert.AreEqual("cpu-lab", workerAlias);
            return Task.FromResult(new FineTuningWorkerConfiguration(workerAlias,
                new Uri("http://127.0.0.1:8091/"), Key, 30));
        }

        public Task<List<Domain.Models.DatabaseModel.Dto.AIFineTuningWorkerDto>> GetWorkersAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Domain.Models.DatabaseModel.Dto.AIFineTuningWorkerDto>());
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }

    private sealed class ReadTrackingStream(byte[] bytes) : MemoryStream(bytes)
    {
        public int ReadCount { get; private set; }
        public bool Disposed { get; private set; }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return base.ReadAsync(buffer, cancellationToken);
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            ReadCount++;
            return base.CopyToAsync(destination, bufferSize, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}

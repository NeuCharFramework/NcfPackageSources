using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.AIKernel.Domain.Models.FineTuning;

namespace Senparc.Xncf.AIKernel.Tests;

[TestClass]
public class FineTuningLiveWorkerTests
{
    [TestMethod]
    [TestCategory("FineTuningIntegration")]
    public async Task LiveCpuWorker_ExercisesTypedClientTrainingEventsArtifactsAndCancellation()
    {
        var endpoint = Environment.GetEnvironmentVariable("NCF_TEST_WORKER_ENDPOINT");
        var key = Environment.GetEnvironmentVariable("NCF_TEST_WORKER_KEY");
        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(key))
        {
            Assert.Inconclusive("Opt-in test: supply NCF_TEST_WORKER_ENDPOINT and NCF_TEST_WORKER_KEY for a disposable CPU worker with tiny-gpt2.");
            return;
        }

        const string alias = "cpu_integration";
        var options = new FineTuningWorkerOptions
        {
            Enabled = true,
            AllowedHosts = [new Uri(endpoint).Host],
            WorkerApiKeys = new() { [alias] = key }
        };
        var profile = new AIFineTuningWorker(alias, "Disposable CPU test worker", endpoint, 180, true, "");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IFineTuningWorkerConfigurationProvider>(_ => new LiveProfileProvider(profile, options));
        services.AddHttpClient<FineTuningWorkerClient>(http => http.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<FineTuningWorkerClient>();
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var token = deadline.Token;
        var health = await client.HealthAsync(alias, token);
        Assert.IsFalse(string.IsNullOrWhiteSpace(health.StoreId));
        Assert.IsTrue(health.Capabilities.Any(item => item.Backend == "cpu" && item.Available));
        Assert.IsTrue((await client.GetModelsAsync(alias, token)).Any(item => item.Id == "tiny-gpt2"));
        var dataset = await client.UploadDatasetAsync(alias, new FineTuningDatasetRequest
        {
            Name = "Typed-client infrastructure test",
            Content = string.Join("\n", Enumerable.Range(0, 8).Select(index =>
                JsonSerializer.Serialize(new { prompt = $"Question {index}", completion = $"Answer {index}" })))
        }, token);
        Assert.AreEqual(8, dataset.Rows);
        Assert.IsTrue((await client.GetDatasetsPageAsync(alias, 0, 100, token)).Items.Any(item => item.Id == dataset.Id));
        Assert.AreEqual(dataset.Sha256, (await client.GetDatasetAsync(alias, dataset.Id, token)).Sha256);
        var evaluation = await client.UploadDatasetAsync(alias, new FineTuningDatasetRequest
        {
            Name = "Independent typed-client validation",
            Content = string.Join("\n", Enumerable.Range(20, 2).Select(index =>
                JsonSerializer.Serialize(new { prompt = $"Question {index}", completion = $"Answer {index}" })))
        }, token);
        var request = new FineTuningJobRequest
        {
            Name = "Typed-client CPU LoRA smoke",
            ModelId = "tiny-gpt2", DatasetId = dataset.Id, EvalDatasetId = evaluation.Id, MaxSteps = 2, MaxSequenceLength = 32,
            LoraRank = 2, LoraAlpha = 4, SaveSteps = 1, EvalSteps = 1, MaxDurationMinutes = 5
        };
        var job = await client.CreateJobAsync(alias, request, token);
        while (job.State is "Queued" or "Running" or "Cancelling")
        {
            await Task.Delay(250, token);
            job = await client.GetJobAsync(alias, job.Id, token);
        }
        Assert.AreEqual("Succeeded", job.State, job.Error);
        Assert.AreEqual(2d, job.LatestMetrics["step"]);
        Assert.IsTrue(job.LatestMetrics["evalLoss"] > 0);
        var events = await client.GetEventsAsync(alias, job.Id, 0, 200, token);
        Assert.IsTrue(events.Events.Any(item => item.Metrics.ContainsKey("trainLoss")));
        var artifact = job.Artifacts.Single(item => item.Name == "training-export.zip");
        using (var download = await client.DownloadArtifactAsync(alias, job.Id, artifact.Id, token))
        using (var bytes = new MemoryStream(await download.Content.ReadAsByteArrayAsync(token)))
        using (var archive = new ZipArchive(bytes))
        {
            Assert.IsNotNull(archive.GetEntry("manifest.json"));
            Assert.IsTrue(archive.Entries.Any(item => item.FullName.StartsWith("adapter/", StringComparison.Ordinal)));
            using var manifestStream = archive.GetEntry("manifest.json")!.Open();
            using var manifest = await JsonDocument.ParseAsync(manifestStream, cancellationToken: token);
            Assert.AreEqual("explicitHeldOut", manifest.RootElement.GetProperty("evaluationStrategy").GetString());
            Assert.AreEqual(8, manifest.RootElement.GetProperty("trainingRows").GetInt32());
            Assert.AreEqual(2, manifest.RootElement.GetProperty("evaluationRows").GetInt32());
        }
        request.Name = "Typed-client cancellation smoke";
        request.MaxSteps = 100000;
        var cancelled = await client.CreateJobAsync(alias, request, token);
        cancelled = await client.CancelJobAsync(alias, cancelled.Id, token);
        while (cancelled.State is "Queued" or "Running" or "Cancelling")
        {
            await Task.Delay(250, token);
            cancelled = await client.GetJobAsync(alias, cancelled.Id, token);
        }
        Assert.AreEqual("Cancelled", cancelled.State);
        Assert.IsTrue((await client.GetJobsPageAsync(alias, 0, 10, token)).Items.Any(item => item.Id == job.Id));
    }

    private sealed class LiveProfileProvider(AIFineTuningWorker profile, FineTuningWorkerOptions options)
        : IFineTuningWorkerConfigurationProvider
    {
        public Task<FineTuningWorkerConfiguration> GetAsync(string workerAlias, CancellationToken cancellationToken = default)
        {
            if (workerAlias != profile.Alias)
                throw new NcfExceptionBase("The requested worker is not this disposable test profile.");
            return Task.FromResult(new FineTuningWorkerConfiguration(profile.Alias,
                options.GetValidatedEndpoint(profile.Alias, profile.Endpoint, profile.RequestTimeoutSeconds),
                options.GetWorkerApiKey(profile.Alias), profile.RequestTimeoutSeconds));
        }

        public Task<List<AIFineTuningWorkerDto>> GetWorkersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<AIFineTuningWorkerDto> { new(profile, true) });
    }
}

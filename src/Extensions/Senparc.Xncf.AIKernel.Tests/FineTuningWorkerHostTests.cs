using System.Reflection;
using System.Net;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AIKernel.Domain.Models.FineTuning;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.AIKernel.OHS.Local.PL;

namespace Senparc.Xncf.AIKernel.Tests;

[TestClass]
public class FineTuningWorkerHostTests
{
    private const string Key = "unit-test-secret-value-0123456789abcd";

    [TestMethod]
    public void SecretFile_IsPrivateAndDoesNotOverrideEnvironment()
    {
        var root = NewTemp();
        try
        {
            var store = new FineTuningSecretStore(Path.Combine(root, "secrets"));
            var created = store.Generate("mlx_lab");
            var again = store.Generate("mlx_lab");
            Assert.IsTrue(created.Created);
            Assert.IsFalse(again.Created);
            Assert.AreEqual(created.Path, again.Path);
            var text = File.ReadAllText(created.Path);
            Assert.AreEqual(64, text.Length);
            Assert.IsFalse(text.Contains('\n') || text.Contains('\r'));
            Assert.IsFalse(created.Path.Contains(text, StringComparison.Ordinal));
            if (!OperatingSystem.IsWindows())
            {
                var mode = File.GetUnixFileMode(created.Path);
                Assert.IsTrue(mode.HasFlag(UnixFileMode.UserRead));
                Assert.IsTrue(mode.HasFlag(UnixFileMode.UserWrite));
                Assert.IsFalse(mode.HasFlag(UnixFileMode.GroupRead));
                Assert.IsFalse(mode.HasFlag(UnixFileMode.OtherRead));
            }

            CollectionAssert.AreEqual(new[] { "mlx_lab" }, store.ListConfiguredAliases().ToArray());
            var environmentKey = new string('b', 32);
            var resolved = store.Resolve(new FineTuningWorkerOptions
            {
                WorkerApiKeys = new Dictionary<string, string> { ["mlx_lab"] = environmentKey }
            }, "mlx_lab");
            Assert.AreEqual(environmentKey, resolved);
            Assert.AreNotEqual(text, resolved);
            Assert.ThrowsException<NcfExceptionBase>(() => store.Generate("../escape"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void LaunchPlan_KeepsTheKeyOutOfTheCommand()
    {
        var root = NewTemp();
        try
        {
            var project = Path.Combine(root, "AIKernelFineTuning");
            Directory.CreateDirectory(Path.Combine(project, "worker"));
            File.WriteAllText(Path.Combine(project, "worker", "__main__.py"), "# test worker\n");
            var models = Path.Combine(root, "models");
            var data = Path.Combine(root, "data");
            var profile = new FineTuningLaunchProfile
            {
                Managed = true,
                BindHost = "127.0.0.1",
                Port = 8091,
                ModelsRoot = models,
                DataRoot = data,
                WorkerProjectPath = project
            };
            var launch = FineTuningWorkerLaunch.Create("mlx_lab", profile, Key, []);
            CollectionAssert.AreEqual(new[] { "-m", "worker" }, launch.Arguments.ToArray());
            Assert.AreEqual(Key, launch.Variables["NCF_WORKER_KEY"]);
            Assert.IsFalse(string.Join(" ", launch.Arguments).Contains(Key, StringComparison.Ordinal));
            Assert.IsFalse(launch.PythonPath.Contains(Key, StringComparison.Ordinal));
            Assert.IsFalse(launch.WorkingDirectory.Contains(Key, StringComparison.Ordinal));
            var preview = FineTuningWorkerLaunch.Preview(profile, Path.Combine(root, "secrets", "mlx_lab.key"));
            Assert.IsTrue(preview.Contains("$(cat ", StringComparison.Ordinal));
            Assert.IsFalse(preview.Contains(Key, StringComparison.Ordinal));

            var nested = new AIFineTuningWorker_CreateOrEditRequest
            {
                Alias = "mlx_lab",
                Name = "MLX",
                Endpoint = "http://127.0.0.1:8091/",
                Managed = true,
                ModelsRoot = models,
                DataRoot = Path.Combine(models, "nested"),
                WorkerProjectPath = project
            };
            Assert.ThrowsException<NcfExceptionBase>(() => FineTuningWorkerLaunch.Prepare(nested, root, []));
            Assert.ThrowsException<NcfExceptionBase>(() => FineTuningWorkerLaunch.Prepare(new AIFineTuningWorker_CreateOrEditRequest
            {
                Alias = "mlx_lab",
                Name = "MLX",
                Endpoint = "http://0.0.0.0:8091/",
                Managed = true,
                BindHost = "0.0.0.0",
                Port = 8091,
                ModelsRoot = models,
                DataRoot = data,
                WorkerProjectPath = project
            }, root, []));

            var link = Path.Combine(root, "models-link");
            Directory.CreateDirectory(models);
            Directory.CreateSymbolicLink(link, models);
            Assert.ThrowsException<NcfExceptionBase>(() => FineTuningWorkerLaunch.Create("mlx_lab", new FineTuningLaunchProfile
            {
                Managed = true,
                BindHost = "127.0.0.1",
                Port = 8091,
                ModelsRoot = link,
                DataRoot = data,
                WorkerProjectPath = project
            }, Key, []));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void RuntimeSettings_ApplyAfterStartupAndKeepEnvironmentHosts()
    {
        var root = NewTemp();
        try
        {
            var environment = new TestEnvironment { ContentRootPath = root };
            var options = new FineTuningWorkerOptions { Enabled = false, AllowedHosts = ["from-env"] };
            var runtime = new FineTuningRuntimeSettings(Options.Create(options), new FineTuningSecretStore(Path.Combine(root, "secrets")),
                environment, NullLogger<FineTuningRuntimeSettings>.Instance);
            runtime.SetEnabled(true);
            runtime.AllowHost("10.1.1.8");
            runtime.AllowHost("127.0.0.1");
            Assert.IsTrue(options.Enabled);
            CollectionAssert.Contains(options.AllowedHosts, "10.1.1.8");
            CollectionAssert.DoesNotContain(options.AllowedHosts, "127.0.0.1");

            var restarted = new FineTuningWorkerOptions { Enabled = false, AllowedHosts = ["from-env"] };
            var again = new FineTuningRuntimeSettings(Options.Create(restarted), new FineTuningSecretStore(Path.Combine(root, "secrets")),
                environment, NullLogger<FineTuningRuntimeSettings>.Instance);
            again.ApplyStartup();
            Assert.IsTrue(restarted.Enabled);
            CollectionAssert.Contains(restarted.AllowedHosts, "from-env");
            CollectionAssert.Contains(restarted.AllowedHosts, "10.1.1.8");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void EndpointAllowlist_PermitsLoopbackWithoutAnEnvironmentEntry()
    {
        var options = new FineTuningWorkerOptions
        {
            Enabled = true,
            WorkerApiKeys = new Dictionary<string, string> { ["mlx_lab"] = Key },
            AllowedHosts = []
        };
        Assert.AreEqual(IPAddress.Loopback, IPAddress.Parse(
            options.GetValidatedEndpoint("mlx_lab", "http://127.0.0.1:8091", 30).Host));
        Assert.ThrowsException<NcfExceptionBase>(() =>
            options.GetValidatedEndpoint("mlx_lab", "http://training.internal:8091", 30));
    }

    [TestMethod]
    public async Task LaunchProfile_RoundTripsWithoutAnApiKeyColumn()
    {
        var profile = new FineTuningLaunchProfile
        {
            Managed = true,
            BindHost = "127.0.0.1",
            Port = 8092,
            ModelsRoot = "/tmp/ncf-models",
            DataRoot = "/tmp/ncf-data"
        };
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AIKernelSenparcEntities_Sqlite>().UseSqlite(connection).Options;
        await using (var context = new AIKernelSenparcEntities_Sqlite(options) { _enableMultiTenant = false })
        {
            await context.Database.EnsureCreatedAsync();
            context.AiFineTuningWorkers.Add(new AIFineTuningWorker(
                "mlx_lab", "MLX", "http://127.0.0.1:8092/", 30, true, "", profile.ToJson()));
            await context.SaveChangesAsync();
        }

        await using var reader = new AIKernelSenparcEntities_Sqlite(options) { _enableMultiTenant = false };
        var stored = await reader.AiFineTuningWorkers.IgnoreQueryFilters().SingleAsync();
        Assert.AreEqual(profile.ToJson(), stored.LaunchProfileJson);
        Assert.IsNull(typeof(AIFineTuningWorker).GetProperty("ApiKey"));
        Assert.IsFalse((stored.LaunchProfileJson ?? "").Contains(Key, StringComparison.Ordinal));
    }

    private static string NewTemp()
    {
        var path = Path.Combine(Path.GetTempPath(), "ncf-ft-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "AIKernelTests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

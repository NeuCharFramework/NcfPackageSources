/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfPreviewServiceTests.cs
    文件功能描述：XNCF 独立预览路径、命令和隔离配置测试


    创建标识：Senparc - 20260801

----------------------------------------------------------------*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Hosting;
using Senparc.Xncf.XncfBuilder.Domain.Services.Preview;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Tests.Functions
{
    [TestClass]
    public class XncfPreviewServiceTests
    {
        [TestMethod]
        public void ResolveProjectPaths_ShouldUseBuilderConvention()
        {
            var testDirectory = CreateProjectLayout("Demo.Xncf.Sample");
            try
            {
                var paths = XncfPreviewService.ResolveProjectPaths(
                    Path.Combine(testDirectory, "Demo.sln"),
                    "Demo.Xncf.Sample");

                Assert.AreEqual(
                    Path.Combine(testDirectory, "Senparc.Web", "Senparc.Web.csproj"),
                    paths.WebProjectFilePath);
                Assert.AreEqual(
                    Path.Combine(testDirectory, "Demo.Xncf.Sample", "Demo.Xncf.Sample.csproj"),
                    paths.ModuleProjectFilePath);
            }
            finally
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }

        [TestMethod]
        public void ResolveProjectPaths_ShouldRejectDirectoryTraversal()
        {
            var testDirectory = CreateProjectLayout("Demo.Xncf.Sample");
            try
            {
                Assert.ThrowsException<ArgumentException>(() =>
                    XncfPreviewService.ResolveProjectPaths(
                        Path.Combine(testDirectory, "Demo.sln"),
                        "../Demo.Xncf.Sample"));
            }
            finally
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }

        [TestMethod]
        public void ValidateHostProjectReference_ShouldRequireCurrentSourceProject()
        {
            var testDirectory = CreateProjectLayout("Demo.Xncf.Sample");
            try
            {
                var paths = XncfPreviewService.ResolveProjectPaths(
                    Path.Combine(testDirectory, "Demo.sln"),
                    "Demo.Xncf.Sample");

                Assert.ThrowsException<InvalidOperationException>(() =>
                    XncfPreviewService.ValidateHostProjectReference(paths));

                File.WriteAllText(
                    paths.WebProjectFilePath,
                    "<Project><ItemGroup><ProjectReference Include=\"..\\Demo.Xncf.Sample\\Demo.Xncf.Sample.csproj\" /></ItemGroup></Project>");

                XncfPreviewService.ValidateHostProjectReference(paths);
            }
            finally
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }

        [TestMethod]
        public void CreatePublishStartInfo_ShouldUseIsolatedOutputWithoutRestore()
        {
            var projectPath = Path.Combine(Path.GetTempPath(), "Senparc.Web", "Senparc.Web.csproj");
            var publishPath = Path.Combine(Path.GetTempPath(), "NcfPreview", "app");

            var startInfo = XncfPreviewService.CreatePublishStartInfo(projectPath, publishPath);
            var arguments = startInfo.ArgumentList.ToArray();

            Assert.AreEqual("dotnet", startInfo.FileName);
            CollectionAssert.Contains(arguments, "publish");
            CollectionAssert.Contains(arguments, "--no-restore");
            CollectionAssert.Contains(arguments, "--disable-build-servers");
            CollectionAssert.Contains(arguments, "-m:1");
            CollectionAssert.Contains(arguments, publishPath);
            Assert.IsTrue(startInfo.RedirectStandardOutput);
            Assert.IsTrue(startInfo.RedirectStandardError);
        }

        [TestMethod]
        public void CreateRestoreStartInfo_ShouldRestoreOnlyTheWebProjectSerially()
        {
            var projectPath = Path.Combine(Path.GetTempPath(), "Senparc.Web", "Senparc.Web.csproj");
            var startInfo = XncfPreviewService.CreateRestoreStartInfo(projectPath);
            var arguments = startInfo.ArgumentList.ToArray();

            CollectionAssert.AreEqual(
                new[] { "restore", projectPath, "--disable-build-servers", "-m:1" },
                arguments);
        }

        [TestMethod]
        public void RequiresRestore_ShouldDetectMissingOrStaleAssets()
        {
            var testDirectory = CreateProjectLayout("Demo.Xncf.Sample");
            try
            {
                var paths = XncfPreviewService.ResolveProjectPaths(
                    Path.Combine(testDirectory, "Demo.sln"),
                    "Demo.Xncf.Sample");
                Assert.IsTrue(XncfPreviewService.RequiresRestore(paths));

                WriteCurrentAssetsFile(paths.WebProjectFilePath);
                WriteCurrentAssetsFile(paths.ModuleProjectFilePath);
                Assert.IsFalse(XncfPreviewService.RequiresRestore(paths));

                File.SetLastWriteTimeUtc(paths.ModuleProjectFilePath, DateTime.UtcNow.AddMinutes(1));
                Assert.IsTrue(XncfPreviewService.RequiresRestore(paths));
            }
            finally
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }

        [TestMethod]
        public void CreateWebStartInfo_DefaultPreview_ShouldUseLoopbackAndIsolatedDatabase()
        {
            var publishPath = Path.Combine(Path.GetTempPath(), "NcfPreview", "app");
            var startInfo = XncfPreviewService.CreateWebStartInfo(
                publishPath,
                5088,
                XncfPreviewService.DefaultEnvironmentName);

            CollectionAssert.Contains(startInfo.ArgumentList.ToArray(), "--urls=http://127.0.0.1:5088");
            Assert.AreEqual("http://127.0.0.1:5088", startInfo.Environment["ASPNETCORE_URLS"]);
            Assert.AreEqual("XncfPreview", startInfo.Environment["ASPNETCORE_ENVIRONMENT"]);
            Assert.AreEqual("false", startInfo.Environment["DOTNET_hostBuilder__reloadConfigOnChange"]);
            Assert.AreEqual("1", startInfo.Environment["DOTNET_USE_POLLING_FILE_WATCHER"]);
            Assert.AreEqual("Local", startInfo.Environment["SenparcCoreSetting__DatabaseName"]);
            Assert.AreEqual("Sqlite", startInfo.Environment["SenparcCoreSetting__DatabaseType"]);
            Assert.AreEqual("Local", startInfo.Environment["SenparcCoreSetting__CacheType"]);
        }

        [TestMethod]
        public void CreateWebStartInfo_CustomEnvironment_ShouldNotOverrideDatabaseSelection()
        {
            var startInfo = XncfPreviewService.CreateWebStartInfo(
                Path.GetTempPath(),
                5089,
                "Development");

            Assert.IsFalse(startInfo.Environment.ContainsKey("SenparcCoreSetting__DatabaseName"));
            Assert.IsFalse(startInfo.Environment.ContainsKey("SenparcCoreSetting__DatabaseType"));
        }

        [TestMethod]
        public void SanitizePreviewEnvironment_ShouldRemoveApplicationSecrets()
        {
            IDictionary<string, string> environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["PATH"] = "/usr/bin",
                ["HOME"] = "/tmp/home",
                ["ConnectionStrings__Default"] = "secret-database",
                ["OPENAI_API_KEY"] = "secret-ai-key"
            };

            XncfPreviewService.SanitizePreviewEnvironment(environment);

            Assert.AreEqual("/usr/bin", environment["PATH"]);
            Assert.AreEqual("/tmp/home", environment["HOME"]);
            Assert.IsFalse(environment.ContainsKey("ConnectionStrings__Default"));
            Assert.IsFalse(environment.ContainsKey("OPENAI_API_KEY"));
        }

        [TestMethod]
        public void ComputeSourceFingerprint_ShouldTrackSourceButIgnoreBuildArtifacts()
        {
            var testDirectory = Path.Combine(
                Path.GetTempPath(),
                "NcfXncfPreviewFingerprintTests",
                Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(testDirectory);
                File.WriteAllText(Path.Combine(testDirectory, "Register.cs"), "version-1");

                var original = XncfPreviewService.ComputeSourceFingerprint(testDirectory);
                Directory.CreateDirectory(Path.Combine(testDirectory, "obj"));
                File.WriteAllText(Path.Combine(testDirectory, "obj", "generated.tmp"), "ignored");
                Assert.AreEqual(original, XncfPreviewService.ComputeSourceFingerprint(testDirectory));

                File.WriteAllText(Path.Combine(testDirectory, "Register.cs"), "version-2");
                Assert.AreNotEqual(original, XncfPreviewService.ComputeSourceFingerprint(testDirectory));
            }
            finally
            {
                if (Directory.Exists(testDirectory))
                {
                    Directory.Delete(testDirectory, recursive: true);
                }
            }
        }

        [TestMethod]
        public void PreviewPipelineDefinitions_ShouldBeOrderedAndReuseStageProgress()
        {
            var stages = XncfPreviewPresentation.GetPipelineStageDefinitions();

            Assert.AreEqual(XncfPreviewStage.PreparingSource.ToString(), stages[0].Name);
            Assert.AreEqual(XncfPreviewStage.Running.ToString(), stages[^1].Name);
            Assert.IsTrue(stages.All(stage => !string.IsNullOrWhiteSpace(stage.Label)));
            Assert.IsTrue(stages.Zip(stages.Skip(1), (left, right) =>
                left.ProgressPercent < right.ProgressPercent).All(result => result));
            Assert.IsTrue(stages.All(stage =>
                stage.ProgressPercent == ((XncfPreviewStage)stage.Value).GetProgressPercent()));
        }

        [TestMethod]
        public void PreviewTerminalStages_ShouldNotBeStoppable()
        {
            var terminalStages = new[]
            {
                XncfPreviewStage.Stopped,
                XncfPreviewStage.Replaced,
                XncfPreviewStage.Failed,
                XncfPreviewStage.Cancelled,
                XncfPreviewStage.Interrupted
            };

            Assert.IsTrue(terminalStages.All(stage => stage.IsTerminal()));
            Assert.IsTrue(terminalStages.All(stage => !stage.CanStop()));
            Assert.IsTrue(XncfPreviewStage.Building.CanStop());
            Assert.IsTrue(XncfPreviewStage.Running.CanStop());
        }

        [TestMethod]
        public async Task InitializePersistence_ShouldHydratePersistedInterruptedHistory()
        {
            var startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
            var interruptedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            var store = new FakePreviewStateStore(new XncfPreviewPersistenceSnapshot
            {
                SessionId = "persisted-session",
                ModuleProjectName = "Demo.Xncf.Persisted",
                SolutionFilePath = "/workspace/Demo.sln",
                Stage = XncfPreviewStage.Interrupted,
                ProgressPercent = 70,
                StatusMessage = "主站重新启动，之前未完成的预览任务已中断。",
                ErrorMessage = "主站重新启动，之前未完成的预览任务已中断。",
                RecentOutput = "persisted output",
                StartedAt = startedAt,
                UpdatedAt = interruptedAt,
                CompletedAt = interruptedAt,
                HasHost = true,
                Url = "http://127.0.0.1:50994",
                ProcessId = 12345,
                EnvironmentName = XncfPreviewService.DefaultEnvironmentName,
                HostStatus = XncfPreviewHostStatus.Interrupted,
                HostStatusMessage = "主站重新启动，无法安全重新绑定之前的预览进程。",
                ProcessStartedAt = startedAt.AddMinutes(1),
                StoppedAt = interruptedAt
            });
            var service = new XncfPreviewService(stateStore: store);

            await service.InitializePersistenceAsync(CancellationToken.None);
            var session = service.GetSession("persisted-session", includeOutput: true);

            Assert.IsNotNull(session);
            Assert.AreEqual(XncfPreviewStage.Interrupted, session.Stage);
            Assert.AreEqual(XncfPreviewHostStatus.Interrupted, session.HostStatus);
            Assert.AreEqual(70, session.ProgressPercent);
            Assert.IsFalse(session.IsRunning);
            Assert.IsFalse(session.CanStop);
            StringAssert.Contains(session.RecentOutput, "persisted output");
        }

        [TestMethod]
        public async Task InitializePersistence_MissingPersistenceTables_ShouldContinueWithMemoryState()
        {
            var service = new XncfPreviewService(
                stateStore: new FailingPreviewStateStore(
                    new InvalidOperationException("Invalid object name 'XncfBuilderXncfPreviewTask'.")));

            await service.InitializePersistenceAsync(CancellationToken.None);
            var persistenceStatus = service.GetPersistenceStatus();

            Assert.IsFalse(persistenceStatus.IsAvailable);
            StringAssert.Contains(persistenceStatus.StatusMessage, "主站将继续运行");
            StringAssert.Contains(persistenceStatus.ErrorMessage, "XncfBuilderXncfPreviewTask");
        }

        [TestMethod]
        public async Task HostedStart_ShouldDeferPersistenceUntilApplicationStarted()
        {
            using var lifetime = new TestHostApplicationLifetime();
            var stateStore = new DeferredPreviewStateStore();
            var previewService = new XncfPreviewService(stateStore: stateStore);
            var initializer = new XncfPreviewPersistenceInitializerHostedService(previewService, lifetime);

            await ((IHostedService)previewService).StartAsync(CancellationToken.None);
            await initializer.StartAsync(CancellationToken.None);
            await Task.Delay(50);
            Assert.IsFalse(stateStore.LoadStarted.Task.IsCompleted);

            try
            {
                lifetime.StartApplication();
                await stateStore.LoadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
                stateStore.CompleteLoad();
                await stateStore.LoadCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));

                Assert.IsTrue(previewService.GetPersistenceStatus().IsAvailable);
            }
            finally
            {
                await initializer.StopAsync(CancellationToken.None);
            }
        }

        [TestMethod]
        public async Task HostedStop_WhenShutdownTimesOutWaitingForPreviewOperation_ShouldCompleteAfterCleanupWithoutThrow()
        {
            var service = new XncfPreviewService();
            var operationLock = (SemaphoreSlim)typeof(XncfPreviewService)
                .GetField("_operationLock", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(service);
            await operationLock.WaitAsync();
            var lockHeld = true;

            try
            {
                using var shutdownCancellation = new CancellationTokenSource();
                var stopTask = ((IHostedService)service).StopAsync(shutdownCancellation.Token);
                await Task.Delay(50);
                Assert.IsFalse(stopTask.IsCompleted);

                shutdownCancellation.Cancel();
                operationLock.Release();
                lockHeld = false;
                await stopTask;
            }
            finally
            {
                if (lockHeld)
                {
                    operationLock.Release();
                }
            }
        }

        [TestMethod]
        public void PersistenceEntities_ShouldStoreTaskAndHostSeparately()
        {
            var snapshot = new XncfPreviewPersistenceSnapshot
            {
                SessionId = "entity-session",
                ModuleProjectName = "Demo.Xncf.Entity",
                SolutionFilePath = "/workspace/Demo.sln",
                Stage = XncfPreviewStage.Running,
                ProgressPercent = 100,
                StatusMessage = "running",
                StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                UpdatedAt = DateTimeOffset.UtcNow,
                HasHost = true,
                Url = "http://127.0.0.1:50995",
                ProcessId = 100,
                EnvironmentName = XncfPreviewService.DefaultEnvironmentName,
                PublishDirectory = "/tmp/preview/app",
                HostStatus = XncfPreviewHostStatus.Healthy,
                HostStatusMessage = "healthy",
                HealthyAt = DateTimeOffset.UtcNow
            };

            var task = new XncfPreviewTask(snapshot);
            var host = new XncfPreviewHost(snapshot);

            Assert.AreEqual(XncfPreviewStage.Running, task.Stage);
            Assert.AreEqual(snapshot.SolutionFilePath, task.SolutionFilePath);
            Assert.AreEqual(XncfPreviewHostStatus.Healthy, host.Status);
            Assert.AreEqual(snapshot.PublishDirectory, host.PublishDirectory);

            var interruptedAt = DateTimeOffset.UtcNow.AddSeconds(1);
            task.MarkInterrupted(interruptedAt, "interrupted task");
            host.MarkInterrupted(interruptedAt, "interrupted host");
            Assert.AreEqual(XncfPreviewStage.Interrupted, task.Stage);
            Assert.AreEqual(XncfPreviewHostStatus.Interrupted, host.Status);
            Assert.IsNotNull(task.CompletedAtUtc);
            Assert.IsNotNull(host.StoppedAtUtc);
        }

        private static string CreateProjectLayout(string moduleProjectName)
        {
            var testDirectory = Path.Combine(
                Path.GetTempPath(),
                "NcfXncfPreviewTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(testDirectory, "Senparc.Web"));
            Directory.CreateDirectory(Path.Combine(testDirectory, moduleProjectName));
            File.WriteAllText(Path.Combine(testDirectory, "Demo.sln"), string.Empty);
            File.WriteAllText(
                Path.Combine(testDirectory, "Senparc.Web", "Senparc.Web.csproj"),
                "<Project />");
            File.WriteAllText(
                Path.Combine(testDirectory, moduleProjectName, $"{moduleProjectName}.csproj"),
                "<Project />");
            return testDirectory;
        }

        private static void WriteCurrentAssetsFile(string projectFilePath)
        {
            var objDirectory = Path.Combine(Path.GetDirectoryName(projectFilePath), "obj");
            Directory.CreateDirectory(objDirectory);
            var assetsFilePath = Path.Combine(objDirectory, "project.assets.json");
            File.WriteAllText(assetsFilePath, "{}");
            File.SetLastWriteTimeUtc(projectFilePath, DateTime.UtcNow.AddMinutes(-1));
            File.SetLastWriteTimeUtc(assetsFilePath, DateTime.UtcNow);
        }

        private sealed class FakePreviewStateStore : IXncfPreviewStateStore
        {
            private readonly IReadOnlyList<XncfPreviewPersistenceSnapshot> _snapshots;

            public FakePreviewStateStore(params XncfPreviewPersistenceSnapshot[] snapshots)
            {
                _snapshots = snapshots;
            }

            public Task<IReadOnlyList<XncfPreviewPersistenceSnapshot>> LoadRecentAndInterruptAsync(
                int maxCount,
                DateTimeOffset interruptedAt,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_snapshots);
            }

            public Task SaveAsync(
                XncfPreviewPersistenceSnapshot snapshot,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        private sealed class DeferredPreviewStateStore : IXncfPreviewStateStore
        {
            private readonly TaskCompletionSource<IReadOnlyList<XncfPreviewPersistenceSnapshot>> _loadResult =
                new(TaskCreationOptions.RunContinuationsAsynchronously);

            public TaskCompletionSource LoadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public TaskCompletionSource LoadCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public async Task<IReadOnlyList<XncfPreviewPersistenceSnapshot>> LoadRecentAndInterruptAsync(
                int maxCount,
                DateTimeOffset interruptedAt,
                CancellationToken cancellationToken = default)
            {
                LoadStarted.TrySetResult();
                var snapshots = await _loadResult.Task.WaitAsync(cancellationToken);
                LoadCompleted.TrySetResult();
                return snapshots;
            }

            public Task SaveAsync(
                XncfPreviewPersistenceSnapshot snapshot,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public void CompleteLoad()
            {
                _loadResult.TrySetResult(Array.Empty<XncfPreviewPersistenceSnapshot>());
            }
        }

        private sealed class TestHostApplicationLifetime : IHostApplicationLifetime, IDisposable
        {
            private readonly CancellationTokenSource _applicationStarted = new();
            private readonly CancellationTokenSource _applicationStopping = new();
            private readonly CancellationTokenSource _applicationStopped = new();

            public CancellationToken ApplicationStarted => _applicationStarted.Token;

            public CancellationToken ApplicationStopping => _applicationStopping.Token;

            public CancellationToken ApplicationStopped => _applicationStopped.Token;

            public void StartApplication()
            {
                _applicationStarted.Cancel();
            }

            public void StopApplication()
            {
                _applicationStopping.Cancel();
                _applicationStopped.Cancel();
            }

            public void Dispose()
            {
                _applicationStarted.Dispose();
                _applicationStopping.Dispose();
                _applicationStopped.Dispose();
            }
        }

        private sealed class FailingPreviewStateStore : IXncfPreviewStateStore
        {
            private readonly Exception _exception;

            public FailingPreviewStateStore(Exception exception)
            {
                _exception = exception;
            }

            public Task<IReadOnlyList<XncfPreviewPersistenceSnapshot>> LoadRecentAndInterruptAsync(
                int maxCount,
                DateTimeOffset interruptedAt,
                CancellationToken cancellationToken = default)
            {
                return Task.FromException<IReadOnlyList<XncfPreviewPersistenceSnapshot>>(_exception);
            }

            public Task SaveAsync(
                XncfPreviewPersistenceSnapshot snapshot,
                CancellationToken cancellationToken = default)
            {
                return Task.FromException(_exception);
            }
        }
    }
}

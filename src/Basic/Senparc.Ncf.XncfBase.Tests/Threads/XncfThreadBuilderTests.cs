using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Threads;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Tests.Threads;

[TestClass]
[DoNotParallelize]
public sealed class XncfThreadBuilderTests
{
    [TestMethod]
    public async Task Build_WhenApplicationStops_CancelsRunningTaskAndDoesNotScheduleAnotherRun()
    {
        using var lifetime = new TestHostApplicationLifetime();
        using var serviceProvider = new ServiceCollection()
            .AddSingleton<IHostApplicationLifetime>(lifetime)
            .BuildServiceProvider();

        var taskStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var taskStopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var taskFailed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var executionCount = 0;
        var threadInfo = new ThreadInfo(
            "application-stopping-test",
            TimeSpan.Zero,
            async (_, info) =>
            {
                Interlocked.Increment(ref executionCount);
                taskStarted.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, info.StoppingToken);
                }
                finally
                {
                    taskStopped.TrySetResult();
                }
            },
            exceptionHandler: exception =>
            {
                taskFailed.TrySetResult(exception);
                return Task.CompletedTask;
            });
        var builder = new XncfThreadBuilder();
        builder.AddThreadInfo(threadInfo);

        try
        {
            InvokeBuild(builder, new ApplicationBuilder(serviceProvider), new ThreadTestRegister());
            var firstCompletion = await Task.WhenAny(
                taskStarted.Task,
                taskFailed.Task,
                Task.Delay(TimeSpan.FromSeconds(5)));
            if (firstCompletion == taskFailed.Task)
            {
                Assert.Fail(taskFailed.Task.Result.ToString());
            }

            var registered = Senparc.Ncf.XncfBase.Register.ThreadCollection.TryGetValue(threadInfo, out var nativeThread);
            Assert.AreSame(
                taskStarted.Task,
                firstCompletion,
                $"线程任务未在预期时间内启动。故事：{threadInfo.StoryHtml}；线程已登记：{registered}；原生线程状态：{nativeThread?.ThreadState}");

            lifetime.StopApplication();
            await taskStopped.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(TimeSpan.FromMilliseconds(100));

            Assert.IsTrue(threadInfo.StoppingToken.IsCancellationRequested);
            Assert.AreEqual(1, executionCount);
        }
        finally
        {
            Senparc.Ncf.XncfBase.Register.ThreadCollection.TryRemove(threadInfo, out _);
        }
    }

    private static void InvokeBuild(XncfThreadBuilder builder, IApplicationBuilder app, IXncfRegister register)
    {
        var buildMethod = typeof(XncfThreadBuilder).GetMethod(
            "Build",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(buildMethod);
        buildMethod.Invoke(builder, [app, register]);
    }

    private sealed class TestHostApplicationLifetime : IHostApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource _applicationStarted = new();
        private readonly CancellationTokenSource _applicationStopping = new();
        private readonly CancellationTokenSource _applicationStopped = new();

        public CancellationToken ApplicationStarted => _applicationStarted.Token;

        public CancellationToken ApplicationStopping => _applicationStopping.Token;

        public CancellationToken ApplicationStopped => _applicationStopped.Token;

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

    private sealed class ThreadTestRegister : TestModuleRegister, IXncfThread
    {
        public void ThreadConfig(XncfThreadBuilder xncfThreadBuilder)
        {
        }
    }
}

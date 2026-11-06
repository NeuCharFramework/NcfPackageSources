using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel.OHS.Local.AppService;
using Senparc.Xncf.AIKernel.OHS.Local.PL;

namespace Senparc.Xncf.AIKernel.Tests;

[TestClass]
public class FineTuningFunctionRequestTests
{
    [TestMethod]
    public void StartJobFunction_IsSingleRequestAndKeepsAdministrationApi()
    {
        var start = typeof(AIFineTuningAppService).GetMethod(nameof(AIFineTuningAppService.StartFineTuningJobAsync))!;
        var create = typeof(AIFineTuningAppService).GetMethod(nameof(AIFineTuningAppService.CreateJobAsync))!;
        var dataset = typeof(AIFineTuningAppService).GetMethod(nameof(AIFineTuningAppService.CreateDatasetFromTextAsync))!;
        var check = typeof(AIFineTuningAppService).GetMethod(nameof(AIFineTuningAppService.CheckFineTuningWorkerAsync))!;

        Assert.IsNotNull(start.GetCustomAttribute<FunctionRenderAttribute>());
        Assert.AreEqual(1, start.GetParameters().Length);
        Assert.AreEqual(typeof(FineTuningStartJobFunctionRequest), start.GetParameters()[0].ParameterType);
        Assert.IsTrue(typeof(IAppRequest).IsAssignableFrom(start.GetParameters()[0].ParameterType));

        Assert.IsNull(create.GetCustomAttribute<FunctionRenderAttribute>());
        Assert.AreEqual(2, create.GetParameters().Length);
        Assert.AreEqual("workerAlias", create.GetParameters()[1].Name);

        Assert.AreEqual(1, dataset.GetParameters().Length);
        Assert.AreEqual(1, check.GetParameters().Length);
    }

    [TestMethod]
    public async Task StartJobParameters_ExposeLoadedChoicesAndTrainingDefaults()
    {
        var method = typeof(AIFineTuningAppService).GetMethod(nameof(AIFineTuningAppService.StartFineTuningJobAsync))!;
        var bag = new FunctionRenderBag(
            method.DeclaringType!.FullName + "-" + method.Name,
            method,
            method.GetCustomAttribute<FunctionRenderAttribute>()!);
        var parameters = await FunctionHelper.GetFunctionParameterInfoAsync(
            new ServiceCollection().BuildServiceProvider(), bag, tryLoadData: false);

        var model = parameters.Single(item => item.Name == nameof(FineTuningStartJobFunctionRequest.ModelId));
        var dataset = parameters.Single(item => item.Name == nameof(FineTuningStartJobFunctionRequest.DatasetId));
        var backend = parameters.Single(item => item.Name == nameof(FineTuningStartJobFunctionRequest.Backend));
        var epochs = parameters.Single(item => item.Name == nameof(FineTuningStartJobFunctionRequest.Epochs));
        var learningRate = parameters.Single(item => item.Name == nameof(FineTuningStartJobFunctionRequest.LearningRate));

        Assert.AreEqual(ParameterType.DropDownList, model.ParameterType);
        Assert.IsTrue(model.IsRequired);
        Assert.AreEqual("本地模型", model.Title);
        Assert.AreEqual(ParameterType.DropDownList, dataset.ParameterType);
        Assert.AreEqual(ParameterType.DropDownList, backend.ParameterType);
        Assert.AreEqual("cpu", backend.Value);
        Assert.IsTrue(backend.SelectionList.Items.Any(item => item.Value == "cpu" && item.DefaultSelected));
        Assert.AreEqual(1, epochs.Value);
        Assert.AreEqual(0.0002d, learningRate.Value);
        Assert.IsFalse(parameters.Any(item => item.Name.EndsWith("Options", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task LoadData_SelectsTheOnlyReadyWorker()
    {
        var request = new FineTuningStartJobFunctionRequest();
        var services = new ServiceCollection();
        services.AddSingleton<IFineTuningWorkerConfigurationProvider>(new StubWorkerConfiguration());
        await request.LoadData(services.BuildServiceProvider());

        var worker = request.WorkerOptions.Items.Single();
        Assert.AreEqual("cpu-lab", worker.Value);
        Assert.IsTrue(worker.DefaultSelected);
        Assert.AreEqual(string.Empty, request.EvalDatasetOptions.Items[0].Value);
        Assert.IsTrue(request.EvalDatasetOptions.Items[0].DefaultSelected);
        Assert.AreEqual(0, request.ModelOptions.Items.Count);
    }

    [TestMethod]
    public void ToJobRequest_UnwrapsIdsAndAppliesDefaults()
    {
        var request = new FineTuningStartJobFunctionRequest
        {
            WorkerAlias = "cpu-lab",
            ModelId = "cpu-lab/base-model",
            DatasetId = "cpu-lab/training-set",
            EvalDatasetId = ""
        };

        var job = request.ToJobRequest();

        Assert.IsTrue(job.Name.StartsWith("fine-tune-", StringComparison.Ordinal));
        Assert.AreEqual("base-model", job.ModelId);
        Assert.AreEqual("training-set", job.DatasetId);
        Assert.IsNull(job.EvalDatasetId);
        Assert.AreEqual("cpu", job.Backend);
        Assert.AreEqual("lora", job.Method);
        Assert.AreEqual(1, job.Epochs);
        Assert.AreEqual(10, job.MaxSteps);
        Assert.AreEqual(0.0002d, job.LearningRate);
        Assert.AreEqual("all-linear", job.TargetModules);
        Assert.AreEqual(60, job.MaxDurationMinutes);
        Assert.IsTrue(Validator.TryValidateObject(job, new ValidationContext(job), new List<ValidationResult>(), true));
    }

    [TestMethod]
    public void ToJobRequest_RejectsResourcesFromDifferentWorkers()
    {
        var request = new FineTuningStartJobFunctionRequest
        {
            WorkerAlias = "cpu-lab",
            ModelId = "gpu-lab/base-model",
            DatasetId = "cpu-lab/training-set"
        };

        var error = Assert.ThrowsException<InvalidOperationException>(() => request.ToJobRequest());
        StringAssert.Contains(error.Message, "同一个 Worker");
    }

    private sealed class StubWorkerConfiguration : IFineTuningWorkerConfigurationProvider
    {
        public Task<FineTuningWorkerConfiguration> GetAsync(string workerAlias, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<List<AIFineTuningWorkerDto>> GetWorkersAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<AIFineTuningWorkerDto>
            {
                new()
                {
                    Alias = "cpu-lab",
                    Name = "CPU Lab",
                    Endpoint = "http://127.0.0.1:8091",
                    Enabled = true,
                    HasConfiguredSecret = true
                },
                new()
                {
                    Alias = "disabled",
                    Name = "Disabled",
                    Enabled = false,
                    HasConfiguredSecret = true
                }
            });
    }
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FineTuningFunctionRequests.cs
    文件功能描述：供 Function 与 Workflow 调用的微调请求


    创建标识：Senparc - 20261106

    修改标识：Senparc - 20261106
    修改描述：自动载入微调 Worker、模型和数据集，并为训练参数提供默认值

----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Models.FineTuning;
using Senparc.Xncf.AIKernel.Domain.Services;

namespace Senparc.Xncf.AIKernel.OHS.Local.PL;

public class FineTuningWorkerFunctionRequest : FunctionAppRequestBase
{
    [Required]
    [MaxLength(50)]
    [Description("微调 Worker||自动载入已启用且已配置密钥的 Worker。只有一个时默认选中。")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(WorkerOptions), Filterable = true, AllowCreate = true)]
    public string? WorkerAlias { get; set; }

    [JsonIgnore]
    public SelectionList WorkerOptions { get; set; } = new(SelectionType.DropDownList);

    public override async Task LoadData(IServiceProvider serviceProvider)
    {
        try
        {
            await FineTuningFunctionCatalog.LoadWorkersAsync(serviceProvider, WorkerOptions).ConfigureAwait(false);
            await base.LoadData(serviceProvider).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            FineTuningFunctionCatalog.WriteDebug("载入微调 Worker 选项失败。", exception);
        }
    }
}

public sealed class FineTuningDatasetFunctionRequest : FineTuningWorkerFunctionRequest
{
    [Required]
    [MaxLength(100)]
    [Description("数据集名称||保存到所选 Worker 的数据集名称。")]
    public string? Name { get; set; }

    [Required]
    [Description("JSONL 内容||每行一条 JSON。适合 Workflow 中的小数据集；大文件请在微调管理页上传。")]
    [FunctionParameterUi(ParameterType.TextArea)]
    public string? Content { get; set; }
}

public sealed class FineTuningStartJobFunctionRequest : FineTuningWorkerFunctionRequest
{
    [MaxLength(100)]
    [Description("任务名称||留空时自动生成 fine-tune-时间。")]
    public string? Name { get; set; }

    [Required]
    [MaxLength(128)]
    [Description("本地模型||自动载入所选 Worker 上已批准的模型。只有一个模型时默认选中。")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(ModelOptions), Filterable = true, AllowCreate = true)]
    public string? ModelId { get; set; }

    [JsonIgnore]
    public SelectionList ModelOptions { get; set; } = new(SelectionType.DropDownList);

    [Required]
    [MaxLength(128)]
    [Description("训练数据集||自动载入所选 Worker 上的 JSONL 数据集。只有一个数据集时默认选中。")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(DatasetOptions), Filterable = true, AllowCreate = true)]
    public string? DatasetId { get; set; }

    [JsonIgnore]
    public SelectionList DatasetOptions { get; set; } = new(SelectionType.DropDownList);

    [MaxLength(128)]
    [Description("验证数据集||可选。默认不使用验证集，且不能与训练数据集相同。")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(EvalDatasetOptions), Filterable = true, AllowCreate = true)]
    public string? EvalDatasetId { get; set; }

    [JsonIgnore]
    public SelectionList EvalDatasetOptions { get; set; } = new(SelectionType.DropDownList, new List<SelectionItem>
    {
        new(string.Empty, "不使用验证集", "训练时不单独计算验证损失", true)
    });

    [Required]
    [Description("训练后端||默认 CPU。CUDA 需要 NVIDIA GPU，MLX 需要 Apple Silicon。")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(BackendOptions))]
    public string Backend { get; set; } = "cpu";

    [JsonIgnore]
    public SelectionList BackendOptions { get; set; } = new(SelectionType.DropDownList, new List<SelectionItem>
    {
        new("cpu", "CPU", "默认，适合本机验证", true),
        new("cuda", "CUDA", "需要 NVIDIA GPU"),
        new("mlx", "MLX", "需要 Apple Silicon")
    });

    [Required]
    [Description("微调方式||默认 LoRA。QLoRA 不能与 CPU 后端同时使用。")]
    [FunctionParameterUi(ParameterType.DropDownList, nameof(MethodOptions))]
    public string Method { get; set; } = "lora";

    [JsonIgnore]
    public SelectionList MethodOptions { get; set; } = new(SelectionType.DropDownList, new List<SelectionItem>
    {
        new("lora", "LoRA", "默认微调方式", true),
        new("qlora", "QLoRA", "需要 CUDA，不能与 CPU 后端同时使用")
    });

    [Description("训练轮数||默认 1。")]
    public int Epochs { get; set; } = 1;

    [Description("最大步数||默认 10。设为 0 时按轮数训练。")]
    public int MaxSteps { get; set; } = 10;

    [Description("学习率||默认 0.0002。")]
    public double LearningRate { get; set; } = 0.0002;

    [Description("批大小||默认 1。")]
    public int BatchSize { get; set; } = 1;

    [Description("梯度累积||默认 1。有效批大小 = 批大小 × 梯度累积。")]
    public int GradientAccumulationSteps { get; set; } = 1;

    [Description("最大序列长度||默认 256。")]
    public int MaxSequenceLength { get; set; } = 256;

    [Description("LoRA Rank||默认 8。")]
    public int LoraRank { get; set; } = 8;

    [Description("LoRA Alpha||默认 16。")]
    public int LoraAlpha { get; set; } = 16;

    [Description("LoRA Dropout||默认 0.05。")]
    public double LoraDropout { get; set; } = 0.05;

    [Required]
    [MaxLength(512)]
    [Description("目标模块||默认 all-linear，也可填写逗号分隔的模块名。")]
    public string TargetModules { get; set; } = "all-linear";

    [Description("Warmup 比例||默认 0.03。")]
    public double WarmupRatio { get; set; } = 0.03;

    [Description("权重衰减||默认 0。")]
    public double WeightDecay { get; set; }

    [Description("日志间隔||默认每 1 步记录一次。")]
    public int LoggingSteps { get; set; } = 1;

    [Description("保存间隔||默认每 20 步保存一次。")]
    public int SaveSteps { get; set; } = 20;

    [Description("验证间隔||默认每 20 步验证一次。仅在选择验证数据集后生效。")]
    public int EvalSteps { get; set; } = 20;

    [Description("随机种子||默认 42。")]
    public int Seed { get; set; } = 42;

    [Description("最长时长（分钟）||默认 60。")]
    public int MaxDurationMinutes { get; set; } = 60;

    public override async Task LoadData(IServiceProvider serviceProvider)
    {
        try
        {
            var workers = await FineTuningFunctionCatalog.LoadWorkersAsync(serviceProvider, WorkerOptions)
                .ConfigureAwait(false);
            await FineTuningFunctionCatalog.LoadTrainingCatalogAsync(
                serviceProvider, workers, ModelOptions, DatasetOptions, EvalDatasetOptions).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            FineTuningFunctionCatalog.WriteDebug("载入微调任务选项失败。", exception);
        }
    }

    public string? ResolveWorkerAlias()
    {
        Unwrap(ModelId, out var modelWorker);
        Unwrap(DatasetId, out var datasetWorker);
        Unwrap(EvalDatasetId, out var evalWorker);
        var aliases = new[] { Normalize(WorkerAlias), modelWorker, datasetWorker, evalWorker }
            .Where(alias => !string.IsNullOrWhiteSpace(alias))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (aliases.Count > 1)
        {
            throw new InvalidOperationException("微调 Worker、模型和数据集必须属于同一个 Worker。");
        }

        return aliases.SingleOrDefault();
    }

    public FineTuningJobRequest ToJobRequest()
    {
        var workerAlias = ResolveWorkerAlias();
        var modelId = Unwrap(ModelId, out _);
        var datasetId = Unwrap(DatasetId, out _);
        var evalId = Unwrap(EvalDatasetId, out _);
        if (string.IsNullOrWhiteSpace(workerAlias))
        {
            throw new InvalidOperationException("请选择微调 Worker。当前没有可自动载入的已启用 Worker。");
        }

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("请选择本地模型。");
        }

        if (string.IsNullOrWhiteSpace(datasetId))
        {
            throw new InvalidOperationException("请选择训练数据集。");
        }

        if (!string.IsNullOrWhiteSpace(evalId) &&
            string.Equals(evalId, datasetId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("验证数据集必须与训练数据集不同。");
        }

        return new FineTuningJobRequest
        {
            Name = string.IsNullOrWhiteSpace(Name)
                ? $"fine-tune-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : Name.Trim(),
            ModelId = modelId,
            DatasetId = datasetId,
            EvalDatasetId = string.IsNullOrWhiteSpace(evalId) ? null : evalId,
            Backend = string.IsNullOrWhiteSpace(Backend) ? "cpu" : Backend.Trim(),
            Method = string.IsNullOrWhiteSpace(Method) ? "lora" : Method.Trim(),
            Epochs = Epochs,
            MaxSteps = MaxSteps,
            LearningRate = LearningRate,
            BatchSize = BatchSize,
            GradientAccumulationSteps = GradientAccumulationSteps,
            MaxSequenceLength = MaxSequenceLength,
            LoraRank = LoraRank,
            LoraAlpha = LoraAlpha,
            LoraDropout = LoraDropout,
            TargetModules = string.IsNullOrWhiteSpace(TargetModules) ? "all-linear" : TargetModules.Trim(),
            WarmupRatio = WarmupRatio,
            WeightDecay = WeightDecay,
            LoggingSteps = LoggingSteps,
            SaveSteps = SaveSteps,
            EvalSteps = EvalSteps,
            Seed = Seed,
            MaxDurationMinutes = MaxDurationMinutes
        };
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Unwrap(string? value, out string? alias)
    {
        alias = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        var separator = trimmed.IndexOf('/');
        if (separator <= 0 || separator >= trimmed.Length - 1)
        {
            return trimmed;
        }

        alias = trimmed[..separator];
        return trimmed[(separator + 1)..];
    }
}

internal static class FineTuningFunctionCatalog
{
    private static readonly TimeSpan CatalogTimeout = TimeSpan.FromSeconds(3);

    public static async Task<List<AIFineTuningWorkerDto>> LoadWorkersAsync(
        IServiceProvider serviceProvider, SelectionList target)
    {
        var configuration = serviceProvider.GetService<IFineTuningWorkerConfigurationProvider>();
        if (configuration == null || target == null)
        {
            WriteDebug("未注册 Worker 配置服务，无法自动载入 Worker。");
            return new List<AIFineTuningWorkerDto>();
        }

        var workers = await configuration.GetWorkersAsync().ConfigureAwait(false) ?? new List<AIFineTuningWorkerDto>();
        var ready = workers
            .Where(worker => worker.Enabled && worker.HasConfiguredSecret && !string.IsNullOrWhiteSpace(worker.Alias))
            .OrderBy(worker => worker.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var onlyOne = ready.Count == 1;
        target.Items = ready.Select(worker => new SelectionItem(
            worker.Alias,
            string.IsNullOrWhiteSpace(worker.Name) ? worker.Alias : $"{worker.Name} ({worker.Alias})",
            worker.Endpoint ?? string.Empty,
            onlyOne)).ToList();
        WriteDebug($"自动载入可用 Worker {ready.Count} 个。");
        return ready;
    }

    public static async Task LoadTrainingCatalogAsync(
        IServiceProvider serviceProvider,
        IReadOnlyList<AIFineTuningWorkerDto> workers,
        SelectionList modelOptions,
        SelectionList datasetOptions,
        SelectionList evalOptions)
    {
        var models = new List<SelectionItem>();
        var datasets = new List<SelectionItem>();
        var client = serviceProvider.GetService<FineTuningWorkerClient>();
        var ready = workers ?? Array.Empty<AIFineTuningWorkerDto>();
        if (client != null && ready.Count > 0)
        {
            using var timeout = new CancellationTokenSource(CatalogTimeout);
            var singleWorker = ready.Count == 1;
            foreach (var worker in ready)
            {
                if (timeout.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    var workerModels = await client.GetModelsAsync(worker.Alias, timeout.Token).ConfigureAwait(false)
                        ?? new List<FineTuningModel>();
                    foreach (var model in workerModels.Where(model => !string.IsNullOrWhiteSpace(model.Id)))
                    {
                        models.Add(new SelectionItem(
                            OptionValue(singleWorker, worker.Alias, model.Id),
                            singleWorker ? model.Name : $"{model.Name} · {worker.Alias}",
                            string.Join(", ", model.Backends ?? new List<string>())));
                    }

                    var page = await client.GetDatasetsPageAsync(worker.Alias, 0, 100, timeout.Token)
                        .ConfigureAwait(false);
                    foreach (var dataset in (page?.Items ?? new List<FineTuningDataset>())
                        .Where(dataset => !string.IsNullOrWhiteSpace(dataset.Id)))
                    {
                        datasets.Add(new SelectionItem(
                            OptionValue(singleWorker, worker.Alias, dataset.Id),
                            singleWorker
                                ? $"{dataset.Name}（{dataset.Rows} 行）"
                                : $"{dataset.Name}（{dataset.Rows} 行） · {worker.Alias}",
                            dataset.Id));
                    }

                    WriteDebug($"Worker {worker.Alias} 自动载入模型 {workerModels.Count} 个、数据集 {page?.Total ?? datasets.Count} 个。");
                }
                catch (Exception exception)
                {
                    WriteDebug($"Worker {worker.Alias} 的模型或数据集暂时无法载入：{exception.Message}", exception);
                }
            }
        }
        else
        {
            WriteDebug("没有可用的微调 Worker 客户端或已启用 Worker，模型和数据集保持为空。");
        }

        if (models.Count == 1)
        {
            models[0].DefaultSelected = true;
        }

        if (datasets.Count == 1)
        {
            datasets[0].DefaultSelected = true;
        }

        modelOptions.Items = models;
        datasetOptions.Items = datasets;
        evalOptions.Items = new List<SelectionItem>
        {
            new(string.Empty, "不使用验证集", "训练时不单独计算验证损失", true)
        };
        evalOptions.Items = evalOptions.Items.Concat(datasets.Select(dataset =>
            new SelectionItem(dataset.Value, dataset.Text, dataset.Note))).ToList();
    }

    private static string OptionValue(bool singleWorker, string alias, string id) =>
        singleWorker ? id : $"{alias}/{id}";

    internal static void WriteDebug(string message, Exception? exception = null)
    {
        System.Diagnostics.Debug.WriteLine(exception == null
            ? $"[微调 Function] {message}"
            : $"[微调 Function] {message} {exception}");
        try
        {
            if (exception == null)
            {
                SenparcTrace.SendCustomLog("微调 Function", message);
            }
            else
            {
                SenparcTrace.BaseExceptionLog(exception);
            }
        }
        catch (Exception traceException)
        {
            System.Diagnostics.Debug.WriteLine($"[微调 Function] 调试日志写入失败：{traceException.Message}");
        }
    }
}

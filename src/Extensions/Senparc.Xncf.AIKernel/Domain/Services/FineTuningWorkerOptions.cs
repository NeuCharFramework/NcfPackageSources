/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FineTuningWorkerOptions.cs
    文件功能描述：微调 Worker 配置选项


    创建标识：Senparc - 20261009

    修改标识：Senparc - 20261009
    修改描述：v0.16.4 完善 AIKernel 本地微调 Worker、数据库配置与本地化管理能力

----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Senparc.Ncf.Core.Exceptions;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public sealed class FineTuningWorkerOptions
{
    public const string SectionName = "SenparcXncfAIKernel:FineTuning";
    public bool Enabled { get; set; }
    public Dictionary<string, string> WorkerApiKeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string[] AllowedHosts { get; set; } = [];
    [Obsolete("Use database worker profiles and WorkerApiKeys instead.")]
    public string WorkerEndpoint { get; set; } = "";
    [Obsolete("Use WorkerApiKeys through a secret provider instead.")]
    public string WorkerApiKey { get; set; } = "";
    [Obsolete("Use the database worker profile timeout instead.")]
    public int RequestTimeoutSeconds { get; set; } = 30;

    public IEnumerable<string> ConfiguredWorkerAliases => WorkerApiKeys
        .Where(item => IsValidApiKey(item.Value)).Select(item => item.Key);

    private static bool IsValidApiKey(string apiKey) =>
        !string.IsNullOrWhiteSpace(apiKey) && apiKey.Length >= 32
        && apiKey == apiKey.Trim() && !apiKey.Contains('\r') && !apiKey.Contains('\n');

    public string GetWorkerApiKey(string workerAlias)
    {
        if (!Enabled)
            throw new NcfExceptionBase(AIKernelResource.Get("FineTuning.Error.Disabled",
                "Local fine-tuning is disabled by the infrastructure configuration."));
        if (!WorkerApiKeys.TryGetValue(workerAlias, out var apiKey) || !IsValidApiKey(apiKey))
            throw new NcfExceptionBase($"No valid fine-tuning worker secret is configured for '{workerAlias}'.");
        return apiKey;
    }

    public Uri GetValidatedEndpoint(string workerAlias, string workerEndpoint, int requestTimeoutSeconds)
    {
        GetWorkerApiKey(workerAlias);
        if (!Uri.TryCreate(workerEndpoint, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new NcfExceptionBase("Fine-tuning WorkerEndpoint must be an absolute HTTP(S) URL without credentials, query or fragment.");
        if (AllowedHosts.Length == 0 || !AllowedHosts.Contains(endpoint.Host, StringComparer.OrdinalIgnoreCase))
            throw new NcfExceptionBase($"Fine-tuning WorkerEndpoint host '{endpoint.Host}' is not in the infrastructure allowlist.");
        if (requestTimeoutSeconds < 1 || requestTimeoutSeconds > 300)
            throw new NcfExceptionBase("Fine-tuning RequestTimeoutSeconds must be between 1 and 300.");
        return new Uri(endpoint.AbsoluteUri.TrimEnd('/') + "/");
    }

    [Obsolete("Use GetValidatedEndpoint(workerAlias, workerEndpoint, requestTimeoutSeconds).")]
    public Uri GetValidatedEndpoint() => GetLegacyConfiguration().Endpoint;

    public FineTuningWorkerConfiguration GetLegacyConfiguration()
    {
        var originalKeys = WorkerApiKeys;
        var originalHosts = AllowedHosts;
        try
        {
            WorkerApiKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["default"] = WorkerApiKey };
            if (AllowedHosts.Length == 0 && Uri.TryCreate(WorkerEndpoint, UriKind.Absolute, out var endpoint))
                AllowedHosts = [endpoint.Host];
            return new FineTuningWorkerConfiguration("default",
                GetValidatedEndpoint("default", WorkerEndpoint, RequestTimeoutSeconds),
                GetWorkerApiKey("default"), RequestTimeoutSeconds);
        }
        finally
        {
            WorkerApiKeys = originalKeys;
            AllowedHosts = originalHosts;
        }
    }
}

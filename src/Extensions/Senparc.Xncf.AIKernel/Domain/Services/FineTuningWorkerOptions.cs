/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FineTuningWorkerOptions.cs
    File description: Related implementation.

    Created by: Senparc - 20261009

    Modified by: Senparc - 20261009
    Change description: v0.16.4 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 enhance Agent Studio, group configuration, and task management interactions

    Modified by: Senparc - 20261106
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

    /// <summary>
    /// Validates the minimum worker API-key shape without revealing its value.
    /// </summary>
    /// <param name="apiKey">The candidate API key.</param>
    /// <returns><see langword="true"/> when the key is non-empty, trimmed, newline-free, and at least 32 characters.</returns>
    public static bool IsValidApiKey(string apiKey) =>
        !string.IsNullOrWhiteSpace(apiKey) && apiKey.Length >= 32
        && apiKey == apiKey.Trim() && !apiKey.Contains('\r') && !apiKey.Contains('\n');

    /// <summary>
    /// Determines whether a host is localhost or an IP loopback address.
    /// </summary>
    /// <param name="host">The host text to inspect.</param>
    /// <returns><see langword="true"/> for localhost or an IP loopback address.</returns>
    public static bool IsLoopbackHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;
        host = host.Trim().Trim('[', ']');
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            return true;
        return IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address);
    }

    /// <summary>
    /// Retrieves a valid configured API key for a worker alias.
    /// </summary>
    /// <param name="workerAlias">The worker alias.</param>
    /// <returns>The configured API key.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when local fine-tuning is disabled or no valid key is configured.</exception>
    public string GetWorkerApiKey(string workerAlias)
    {
        if (!Enabled)
            throw new NcfExceptionBase(AIKernelResource.Get("FineTuning.Error.Disabled",
                "Local fine-tuning is disabled by the infrastructure configuration."));
        if (!WorkerApiKeys.TryGetValue(workerAlias, out var apiKey) || !IsValidApiKey(apiKey))
            throw new NcfExceptionBase($"No valid fine-tuning worker secret is configured for '{workerAlias}'.");
        return apiKey;
    }

    /// <summary>
    /// Validates a worker endpoint against HTTP(S), credential-free URI, host allowlist,
    /// and timeout constraints.
    /// </summary>
    /// <param name="workerAlias">The selected worker alias.</param>
    /// <param name="workerEndpoint">The absolute worker endpoint.</param>
    /// <param name="requestTimeoutSeconds">The request timeout in seconds.</param>
    /// <returns>A normalized endpoint URI with a trailing slash.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when enablement, URI, host, or timeout validation fails.</exception>
    public Uri GetValidatedEndpoint(string workerAlias, string workerEndpoint, int requestTimeoutSeconds)
    {
        if (!Enabled)
            throw new NcfExceptionBase(AIKernelResource.Get("FineTuning.Error.Disabled",
                "Local fine-tuning is disabled by the infrastructure configuration."));
        if (string.IsNullOrWhiteSpace(workerAlias))
            throw new NcfExceptionBase("A fine-tuning worker must be selected.");
        if (!Uri.TryCreate(workerEndpoint, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new NcfExceptionBase("Fine-tuning WorkerEndpoint must be an absolute HTTP(S) URL without credentials, query or fragment.");
        var allowedHosts = AllowedHosts ?? [];
        if (!IsLoopbackHost(endpoint.Host)
            && (allowedHosts.Length == 0 || !allowedHosts.Contains(endpoint.Host, StringComparer.OrdinalIgnoreCase)))
            throw new NcfExceptionBase($"Fine-tuning WorkerEndpoint host '{endpoint.Host}' is not in the infrastructure allowlist.");
        if (requestTimeoutSeconds < 1 || requestTimeoutSeconds > 300)
            throw new NcfExceptionBase("Fine-tuning RequestTimeoutSeconds must be between 1 and 300.");
        return new Uri(endpoint.AbsoluteUri.TrimEnd('/') + "/");
    }

    /// <summary>
    /// Validates and returns the legacy single-worker endpoint configuration.
    /// </summary>
    /// <returns>The legacy worker endpoint.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the legacy endpoint or key is invalid.</exception>
    [Obsolete("Use GetValidatedEndpoint(workerAlias, workerEndpoint, requestTimeoutSeconds).")]
    public Uri GetValidatedEndpoint() => GetLegacyConfiguration().Endpoint;

    /// <summary>
    /// Adapts legacy single-worker options into a database-provider configuration.
    /// </summary>
    /// <returns>The legacy default worker configuration.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when legacy endpoint/key validation fails.</exception>
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

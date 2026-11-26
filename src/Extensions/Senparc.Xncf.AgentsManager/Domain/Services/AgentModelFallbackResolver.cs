/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AgentModelFallbackResolver.cs
    File description: Related implementation.

    Created by: Senparc - 20260927
    Creation description: Related implementation.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 update AgentsManager with new features and improvements

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Senparc.AI;
using Senparc.AI.AgentKernel;
using Senparc.AI.Entities;
using Senparc.AI.Interfaces;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

/// <summary>
/// Detects provider authorization/configuration failures and builds narrowly scoped
/// compatibility model settings without silently changing the configured endpoint or credentials.
/// </summary>
public static class AgentModelFallbackResolver
{
    private const int MaxExceptionChainDepth = 8;
    private const int MaxApiVersionLength = 64;
    private const string MafDefaultApiVersion = "2025-04-01-preview";
    private const string NeuCharLegacyDefaultApiVersion = "2022-12-01";
    private const string NeuCharLegacyHost = "www.neuchar.com";

    /// <summary>
    /// Determines whether an exception chain contains a provider-forbidden response.
    /// </summary>
    /// <param name="exception">The exception chain to inspect.</param>
    /// <returns><see langword="true"/> when a supported 403/forbidden signature is present.</returns>
    public static bool ContainsForbiddenStatus(Exception exception)
    {
        if (exception == null)
        {
            return false;
        }

        return HasForbiddenSignature(FlattenExceptionMessages(exception))
               || HasForbiddenSignature(exception.ToString());
    }

    /// <summary>
    /// Determines whether text contains the supported 401/403 service-failure signatures.
    /// </summary>
    /// <param name="text">The provider or client error text to inspect.</param>
    /// <returns><see langword="true"/> when the text represents a recognized authorization/service failure.</returns>
    public static bool ContainsServiceFailureSignature(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim();
        var has403 = normalized.Contains("Status: 403", StringComparison.OrdinalIgnoreCase)
                     || normalized.Contains("StatusCode: 403", StringComparison.OrdinalIgnoreCase);
        var has401 = normalized.Contains("Status: 401", StringComparison.OrdinalIgnoreCase)
                     || normalized.Contains("StatusCode: 401", StringComparison.OrdinalIgnoreCase);
        var hasServiceFailed = normalized.Contains("Service request failed", StringComparison.OrdinalIgnoreCase)
                               || normalized.Contains("ClientResultException", StringComparison.OrdinalIgnoreCase);

        if (hasServiceFailed && (has403 || has401))
        {
            return true;
        }

        return normalized.StartsWith("Status: 403", StringComparison.OrdinalIgnoreCase)
               || normalized.StartsWith("Status: 401", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Flattens an exception and its inner exceptions into a bounded diagnostic string.
    /// </summary>
    /// <param name="exception">The exception chain to flatten.</param>
    /// <returns>A type-annotated message chain, or an empty string for <see langword="null"/>.</returns>
    public static string FlattenExceptionMessages(Exception exception)
    {
        if (exception == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var current = exception;
        var depth = 0;
        while (current != null && depth < MaxExceptionChainDepth)
        {
            if (depth > 0)
            {
                sb.Append(" -> ");
            }

            sb.Append('[');
            sb.Append(current.GetType().Name);
            sb.Append("] ");
            sb.Append(current.Message?.Trim());

            current = current.InnerException;
            depth++;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Creates a copy of an Azure OpenAI or NeuCharAI model whose deployment name equals
    /// the configured model identifier, when those values differ.
    /// </summary>
    /// <param name="model">The model configuration to inspect.</param>
    /// <param name="fallbackModel">Receives the copied fallback model when one can be built.</param>
    /// <returns><see langword="true"/> when a distinct fallback model was created.</returns>
    public static bool TryBuildAlternateDeploymentModel(AIModelDto? model, out AIModelDto? fallbackModel)
    {
        fallbackModel = null;
        if (model == null
            || (model.AiPlatform != AiPlatform.AzureOpenAI && model.AiPlatform != AiPlatform.NeuCharAI)
            || string.IsNullOrWhiteSpace(model.ModelId)
            || string.Equals(model.DeploymentName, model.ModelId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        fallbackModel = new AIModelDto
        {
            Id = model.Id,
            Alias = $"{model.Alias ?? "Model"}_DeploymentAsModelId",
            DeploymentName = model.ModelId,
            ModelId = model.ModelId,
            Endpoint = model.Endpoint,
            AiPlatform = model.AiPlatform,
            ConfigModelType = model.ConfigModelType,
            OrganizationId = model.OrganizationId,
            ApiKey = model.ApiKey,
            ApiVersion = model.ApiVersion,
            Note = model.Note,
            MaxToken = model.MaxToken,
            IsShared = model.IsShared,
            Show = model.Show
        };
        return true;
    }

    /// <summary>
    /// Determines whether a default Chat model setting contains enough endpoint, platform,
    /// model, and credential information to be used as a fallback.
    /// </summary>
    /// <param name="defaultSetting">The default setting to validate.</param>
    /// <returns><see langword="true"/> when the setting is usable for Chat execution.</returns>
    public static bool CanUseDefaultChatFallback(SenparcAiSetting? defaultSetting)
    {
        if (defaultSetting == null || defaultSetting.AiPlatform == AiPlatform.UnSet)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(defaultSetting.ModelName?.Chat))
        {
            return false;
        }

        if (defaultSetting.AiPlatform != AiPlatform.Ollama && string.IsNullOrWhiteSpace(defaultSetting.ApiKey))
        {
            return false;
        }

        return defaultSetting.AiPlatform switch
        {
            AiPlatform.OpenAI => true,
            AiPlatform.Ollama => !string.IsNullOrWhiteSpace(defaultSetting.OllamaEndpoint),
            _ => !string.IsNullOrWhiteSpace(defaultSetting.Endpoint)
        };
    }

    /// <summary>
    /// Converts a runtime AI setting into a Chat <see cref="AIModelDto"/> for diagnostics or fallback execution.
    /// </summary>
    /// <param name="setting">The setting to convert.</param>
    /// <param name="alias">The alias assigned to the generated DTO.</param>
    /// <returns>A model DTO, or <see langword="null"/> when <paramref name="setting"/> is <see langword="null"/>.</returns>
    public static AIModelDto? BuildModelDtoFromSetting(SenparcAiSetting? setting, string alias)
    {
        if (setting == null)
        {
            return null;
        }

        var apiVersion = setting.AiPlatform switch
        {
            AiPlatform.AzureOpenAI => setting.AzureOpenAIApiVersion,
            AiPlatform.NeuCharAI => setting.NeuCharAIApiVersion,
            _ => null
        };

        return new AIModelDto
        {
            Id = 0,
            Alias = alias,
            AiPlatform = setting.AiPlatform,
            ConfigModelType = ConfigModelType.Chat,
            ModelId = setting.ModelName?.Chat,
            DeploymentName = setting.DeploymentName ?? setting.ModelName?.Chat,
            Endpoint = setting.Endpoint,
            ApiKey = setting.ApiKey,
            ApiVersion = apiVersion
        };
    }

    /// <summary>
    /// Compares two Chat model configurations including platform, normalized endpoint,
    /// model/deployment names, and API key.
    /// </summary>
    /// <param name="left">The first model configuration.</param>
    /// <param name="right">The second model configuration.</param>
    /// <returns><see langword="true"/> when both configurations are equivalent.</returns>
    public static bool IsSameChatConfig(AIModelDto? left, AIModelDto? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return left.AiPlatform == right.AiPlatform
               && string.Equals(
                   NormalizeEndpointForDiagnostics(left.AiPlatform, left.Endpoint),
                   NormalizeEndpointForDiagnostics(right.AiPlatform, right.Endpoint),
                   StringComparison.OrdinalIgnoreCase)
               && string.Equals(left.ModelId, right.ModelId, StringComparison.OrdinalIgnoreCase)
               && string.Equals(left.DeploymentName, right.DeploymentName, StringComparison.OrdinalIgnoreCase)
               && string.Equals(left.ApiKey, right.ApiKey, StringComparison.Ordinal);
    }

    /// <summary>
    /// Builds a diagnostic description of a model while exposing only API-key presence and length.
    /// </summary>
    /// <param name="model">The model to describe.</param>
    /// <returns>A diagnostic string safe for logs compared with raw credential output.</returns>
    public static string BuildModelDiagnosticInfo(AIModelDto? model)
    {
        if (model == null)
        {
            return "模型配置为空（AIModelDto == null）";
        }

        var endpoint = NormalizeEndpointForDiagnostics(model.AiPlatform, model.Endpoint);
        var apiKeyStatus = string.IsNullOrWhiteSpace(model.ApiKey)
            ? "empty"
            : $"set(len:{model.ApiKey.Length})";

        return $"AIModelDbId={model.Id}, ConfigType={model.ConfigModelType}, ModelId={model.ModelId ?? "(null)"}, Alias={model.Alias ?? "(null)"}, Platform={model.AiPlatform}, Deployment={model.DeploymentName ?? "(null)"}, Endpoint={endpoint ?? "(null)"}, ApiVersion={model.ApiVersion ?? "(null)"}, ApiKey={apiKeyStatus}";
    }

    /// <summary>
    /// Normalizes an endpoint for comparison and diagnostics without changing non-NeuCharAI values.
    /// </summary>
    /// <param name="platform">The provider platform associated with the endpoint.</param>
    /// <param name="endpoint">The endpoint text to normalize.</param>
    /// <returns>The trimmed endpoint, with a trailing slash added for NeuCharAI when needed.</returns>
    public static string NormalizeEndpointForDiagnostics(AiPlatform platform, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return endpoint;
        }

        var normalized = endpoint.Trim();
        if (platform == AiPlatform.NeuCharAI && !normalized.EndsWith("/", StringComparison.Ordinal))
        {
            normalized += "/";
        }

        return normalized;
    }

    /// <summary>
    /// Builds ordered API-version compatibility candidates for Azure OpenAI and NeuCharAI.
    /// Duplicate, empty, and overlong values are excluded by the candidate validation rules.
    /// </summary>
    /// <param name="model">The resolved database model.</param>
    /// <param name="setting">The effective runtime setting.</param>
    /// <returns>The ordered compatibility candidates.</returns>
    public static IReadOnlyList<ApiVersionCompatibilityFallback> GetApiVersionCompatibilityFallbacks(
        AIModelDto model,
        ISenparcAiSetting setting)
    {
        var candidates = new List<ApiVersionCompatibilityFallback>();
        if (model == null
            || (model.AiPlatform != AiPlatform.AzureOpenAI && model.AiPlatform != AiPlatform.NeuCharAI))
        {
            return candidates;
        }

        AddApiVersionCandidate(candidates, model.ApiVersion, "AIModel");
        AddApiVersionCandidate(candidates, GetSettingApiVersion(model.AiPlatform, setting), "EffectiveSetting");

        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        if (model.AiPlatform == AiPlatform.NeuCharAI
            && string.Equals(GetEndpointHost(model.Endpoint), NeuCharLegacyHost, StringComparison.OrdinalIgnoreCase))
        {
            AddApiVersionCandidate(candidates, NeuCharLegacyDefaultApiVersion, "NeuCharLegacyDefault");
        }

        return candidates;
    }

    /// <summary>
    /// Returns the model API version when configured, otherwise the platform-specific setting version.
    /// </summary>
    /// <param name="model">The database model, when available.</param>
    /// <param name="setting">The effective runtime setting, when available.</param>
    /// <returns>The configured API version or <c>unset</c>.</returns>
    public static string GetConfiguredApiVersion(AIModelDto? model, ISenparcAiSetting? setting)
    {
        if (!string.IsNullOrWhiteSpace(model?.ApiVersion))
        {
            return model.ApiVersion.Trim();
        }

        return GetSettingApiVersion(model?.AiPlatform, setting) ?? "unset";
    }

    /// <summary>
    /// Reads the platform-specific API version from an effective AI setting.
    /// </summary>
    /// <param name="platform">The platform whose setting should be read.</param>
    /// <param name="setting">The effective AI setting.</param>
    /// <returns>The configured API version, or <see langword="null"/> when unavailable.</returns>
    public static string? GetSettingApiVersion(AiPlatform? platform, ISenparcAiSetting? setting)
    {
        return platform switch
        {
            AiPlatform.AzureOpenAI => setting?.AzureOpenAIApiVersion,
            AiPlatform.NeuCharAI => setting?.NeuCharAIApiVersion,
            _ => null
        };
    }

    /// <summary>
    /// Extracts the host from an absolute endpoint for diagnostics.
    /// </summary>
    /// <param name="endpoint">The endpoint text to inspect.</param>
    /// <returns>The URI host, <c>custom</c> for non-absolute endpoints, or <c>unset</c> for empty input.</returns>
    public static string GetEndpointHost(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return "unset";
        }

        return Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri)
            ? endpointUri.Host
            : "custom";
    }

    /// <summary>
    /// Determines whether diagnostic text contains a recognized forbidden/403 signature.
    /// </summary>
    /// <param name="text">The provider or exception text to inspect.</param>
    /// <returns><see langword="true"/> when a forbidden response signature is present.</returns>
    private static bool HasForbiddenSignature(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return text.Contains("Status: 403", StringComparison.OrdinalIgnoreCase)
               || text.Contains("StatusCode: 403", StringComparison.OrdinalIgnoreCase)
               || (text.Contains("403", StringComparison.OrdinalIgnoreCase)
                   && (text.Contains("forbidden", StringComparison.OrdinalIgnoreCase)
                       || text.Contains("禁止", StringComparison.OrdinalIgnoreCase)
                       || text.Contains("拒绝", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Adds a distinct, bounded API-version fallback candidate unless it is empty,
    /// the default version, too long, or already present.
    /// </summary>
    /// <param name="candidates">The candidate collection.</param>
    /// <param name="apiVersion">The candidate API version.</param>
    /// <param name="source">The diagnostic source label.</param>
    private static void AddApiVersionCandidate(
        ICollection<ApiVersionCompatibilityFallback> candidates,
        string apiVersion,
        string source)
    {
        if (string.IsNullOrWhiteSpace(apiVersion))
        {
            return;
        }

        var normalized = apiVersion.Trim();
        if (normalized.Length > MaxApiVersionLength
            || string.Equals(normalized, MafDefaultApiVersion, StringComparison.OrdinalIgnoreCase)
            || candidates.Any(z => string.Equals(z.ApiVersion, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        candidates.Add(new ApiVersionCompatibilityFallback(normalized, source));
    }
}

/// <summary>
///Legacy comment translated to English.
/// </summary>
public sealed record ApiVersionCompatibilityFallback(string ApiVersion, string Source);

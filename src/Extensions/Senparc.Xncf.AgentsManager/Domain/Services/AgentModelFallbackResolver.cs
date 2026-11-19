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
///Legacy comment translated to English.
/// </summary>
public static class AgentModelFallbackResolver
{
    private const int MaxExceptionChainDepth = 8;
    private const int MaxApiVersionLength = 64;
    private const string MafDefaultApiVersion = "2025-04-01-preview";
    private const string NeuCharLegacyDefaultApiVersion = "2022-12-01";
    private const string NeuCharLegacyHost = "www.neuchar.com";

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
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
    ///Legacy comment translated to English.
    /// </summary>
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
    ///Legacy comment translated to English.
    /// </summary>
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
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
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
    ///Legacy comment translated to English.
    /// </summary>
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
    ///Legacy comment translated to English.
    /// </summary>
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
    ///Legacy comment translated to English.
    /// </summary>
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
    ///Legacy comment translated to English.
    /// </summary>
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
    ///Legacy comment translated to English.
    /// </summary>
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
    ///Legacy comment translated to English.
    /// </summary>
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
    ///Legacy comment translated to English.
    /// </summary>
    public static string GetConfiguredApiVersion(AIModelDto? model, ISenparcAiSetting? setting)
    {
        if (!string.IsNullOrWhiteSpace(model?.ApiVersion))
        {
            return model.ApiVersion.Trim();
        }

        return GetSettingApiVersion(model?.AiPlatform, setting) ?? "unset";
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
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
    ///Legacy comment translated to English.
    /// </summary>
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

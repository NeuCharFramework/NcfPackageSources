/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptOptimizationKernelFallbackService.cs
    File description: Related implementation.

    Created by: Senparc - 20260326

    Modified by: Senparc - 20260701
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261021
    Change description: v0.19.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.19.1 add AITokenUsageRecorder and related services for token usage tracking

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Senparc.AI;
using Senparc.AI.AgentKernel;
using Senparc.AI.AgentKernel.Entities;
using Senparc.AI.AgentKernel.Handlers;
using Senparc.AI.Entities;
using Senparc.Ncf.Core.Enums;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Abstractions.Events;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.OHS.Local.PL.Request;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    /// <summary>
    /// When the Agent finishes without calling CreateOptimizedPrompt, run a single completion that returns JSON and persist a new PromptItem version.
    /// </summary>
    public class PromptOptimizationKernelFallbackService
    {
        private readonly PromptItemService _promptItemService;
        private readonly LlModelService _llModelService;
        private readonly PromptOptimizationAgentBridge _bridge;
        private readonly ILogger<PromptOptimizationKernelFallbackService> _logger;
        private readonly Lazy<AIModelService> _aiModelService;

        private const string OptimizationPromptTemplate =
@"Output a single JSON object only (no markdown fences, no commentary). Use this shape:
{
  ""optimizedContent"": ""<full new prompt text; use real newlines inside the string>"",
  ""modelId"": <optional number>,
  ""temperature"": <number>,
  ""topP"": <number>,
  ""maxTokens"": <number>,
  ""frequencyPenalty"": <number>,
  ""presencePenalty"": <number>,
  ""reason"": ""<short summary>"",
  ""predictedScore"": <optional number 0-1>
}

Prompt code (context): {{$promptCode}}

Baseline prompt:
{{$currentPrompt}}

User requirement:
{{$userRequirement}}

If you omit modelId, the server will use defaultModelId {{$defaultModelId}}.
Suggested defaults: temperature {{$defTemp}}, topP {{$defTopP}}, maxTokens {{$defMaxTokens}}, frequencyPenalty {{$defFreq}}, presencePenalty {{$defPres}}.";

        public PromptOptimizationKernelFallbackService(
            PromptItemService promptItemService,
            LlModelService llModelService,
            PromptOptimizationAgentBridge bridge,
            ILogger<PromptOptimizationKernelFallbackService> logger,
            Lazy<AIModelService> aiModelService)
        {
            _promptItemService = promptItemService;
            _llModelService = llModelService;
            _bridge = bridge;
            _logger = logger;
            _aiModelService = aiModelService;
        }

        /// <summary>
        /// Returns a response event: Success=true if a new version was created; otherwise Success=false with ErrorMessage set.
        /// </summary>
        public async Task<PromptOptimizationResponseEvent> TryKernelFallbackAsync(
            PromptOptimizationRequestEvent request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var promptResult = await _promptItemService.GetWithVersionAsync(request.PromptCode, isAvg: true);
                if (promptResult?.PromptItem == null)
                {
                    var msg = $"Kernel fallback: prompt '{request.PromptCode}' not found.";
                    _logger.LogWarning(msg);
                    return Fail(request.RequestId, msg);
                }

                var originalItem = promptResult.PromptItem;
                var aiModelDto = originalItem.AIModelDto;
                if (aiModelDto == null)
                {
                    var msg = "Kernel fallback: AIModelDto missing on prompt item.";
                    _logger.LogWarning(msg);
                    return Fail(request.RequestId, msg);
                }

                //Legacy comment translated to English.
                var avaliableModelResult = await _aiModelService.Value.GetValiableChatModel(aiModelDto);

                aiModelDto = avaliableModelResult.FinalAiModelDto;

                // build aiSettings by model
                var aiSettings = avaliableModelResult.AiSetting;
                //Legacy comment translated to English.

                //ConfigModel configModel = _llModelService.ConvertToConfigModel(aiModelDto.ConfigModelType);

                var handler = new AgentAiHandler(aiSettings);
                var iWantToRun =
                    handler.IWantTo()
                        .ConfigChatModel("PromptOptimizationKernelFallback", new ChatClientAgentOptions()
                        {
                            ChatOptions = new ChatOptions()
                            {
                                Instructions = "You are a senior prompt engineer. Improve the baseline prompt according to the user requirement.",
                                MaxOutputTokens = Math.Max(aiModelDto.MaxToken, 6000),
                                Temperature = 0.2f,
                                TopP = 0.9f
                            }
                        }).BuildKernel();

                var rangeName = request.PromptCode?.Split('-').FirstOrDefault()?.Trim() ?? originalItem.RangeName;
                var bestModelId = await SelectBestModelIdAsync(rangeName, request.Context.ModelId);

                var args = iWantToRun.CreateNewArguments().arguments;
                args["promptCode"] = request.PromptCode ?? "";
                args["currentPrompt"] = request.PromptContent ?? "";
                args["userRequirement"] = request.UserRequirement ?? "";
                args["defaultModelId"] = bestModelId.ToString(CultureInfo.InvariantCulture);
                args["defTemp"] = request.Context.CurrentTemperature.ToString(CultureInfo.InvariantCulture);
                args["defTopP"] = request.Context.CurrentTopP.ToString(CultureInfo.InvariantCulture);
                args["defMaxTokens"] = request.Context.CurrentMaxTokens.ToString(CultureInfo.InvariantCulture);
                args["defFreq"] = request.Context.CurrentFrequencyPenalty.ToString(CultureInfo.InvariantCulture);
                args["defPres"] = request.Context.CurrentPresencePenalty.ToString(CultureInfo.InvariantCulture);

                var aiRequest = iWantToRun.CreateRequest(OptimizationPromptTemplate, iWantToRun.Kernel.AgentSession);
                aiRequest.TempAiArguments = new SenparcAiArguments() { AgentKernelArguments = args };
                aiRequest.RequestContent = aiRequest.ReplacePrompt();//Legacy comment translated to English.

                _logger.LogInformation("Kernel fallback: invoking model for RequestId={RequestId}", request.RequestId);
                var runResult = await _aiModelService.Value.RunChatWithUsageAsync(
                    iWantToRun,
                    aiRequest,
                    model: aiModelDto,
                    source: "AgentsManager.PromptOptimizationFallback").ConfigureAwait(false);
                var raw = runResult.OutputString?.Trim();
                if (string.IsNullOrEmpty(raw))
                {
                    return Fail(request.RequestId, "Kernel fallback: model returned empty output.");
                }

                if (!TryParsePayloadWithRecovery(raw, out var content, out var modelId, out var temperature, out var topP, out var maxTokens,
                        out var frequencyPenalty, out var presencePenalty, out var reason, out var predictedScore))
                {
                    _logger.LogWarning("Kernel fallback: JSON parse failed. First 500 chars: {Snippet}",
                        raw.Length > 500 ? raw.Substring(0, 500) : raw);
                    return Fail(request.RequestId, "Kernel fallback: could not parse model JSON (expected optimizedContent and numeric parameters).");
                }

                if (string.IsNullOrWhiteSpace(content))
                {
                    return Fail(request.RequestId, "Kernel fallback: optimizedContent was empty.");
                }

                modelId = modelId > 0 ? modelId : bestModelId;

                var addRequest = new PromptItem_AddRequest
                {
                    Id = originalItem.Id,
                    RangeId = originalItem.RangeId,
                    ModelId = modelId,
                    Content = UnescapeJsonString(content),
                    Temperature = temperature,
                    TopP = topP,
                    MaxToken = maxTokens,
                    FrequencyPenalty = frequencyPenalty,
                    PresencePenalty = presencePenalty,
                    StopSequences = originalItem.StopSequences,
                    NumsOfResults = 1,
                    IsTopTactic = false,
                    IsNewTactic = false,
                    IsNewSubTactic = false,
                    IsNewAiming = true,
                    IsDraft = false,
                    Note = "🤖AI-Kernel-Fallback",
                    ExpectedResultsJson = originalItem.ExpectedResultsJson ?? string.Empty,
                    Prefix = originalItem.Prefix ?? string.Empty,
                    Suffix = originalItem.Suffix ?? string.Empty,
                    VariableDictJson = originalItem.VariableDictJson ?? string.Empty,
                    isAIGrade = false
                };

                if (!_bridge.TryClaimVersionInsert(request.RequestId))
                {
                    _logger.LogWarning(
                        "Kernel fallback: skip insert — version insert slot already taken (tool path likely wrote DB) RequestId={RequestId}",
                        request.RequestId);
                    return Fail(
                        request.RequestId,
                        "工具可能已写入新版本但未完成会话绑定；请刷新靶道列表。Kernel 回退未重复插入。");
                }

                try
                {
                    var newItem = await _promptItemService.AddPromptItemAsync(addRequest).ConfigureAwait(false);
                    _logger.LogInformation("Kernel fallback: created version {FullVersion} for RequestId={RequestId}",
                        newItem.FullVersion, request.RequestId);

                    var evalReason = string.IsNullOrWhiteSpace(reason)
                        ? "Agent did not call CreateOptimizedPrompt; kernel fallback created a new version."
                        : $"Agent did not call CreateOptimizedPrompt; kernel fallback: {reason}";

                    //Legacy comment translated to English.
                    //Legacy comment translated to English.

                    return new PromptOptimizationResponseEvent(
                        request.RequestId,
                        newItem.FullVersion,
                        newItem.Content,
                        new OptimizedParameters(temperature, topP, maxTokens, frequencyPenalty, presencePenalty),
                        predictedScore,
                        evalReason,
                        true,
                        null);
                }
                catch (Exception)
                {
                    _bridge.ReleaseVersionInsertClaim(request.RequestId);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kernel fallback failed for RequestId={RequestId}", request.RequestId);
                return Fail(request.RequestId, $"Kernel fallback error: {ex.Message}");
            }
        }

        /// <summary>Creates a failed optimization response event.</summary>
        /// <param name="requestId">The request correlation ID.</param>
        /// <param name="message">The failure message.</param>
        /// <returns>A failed response event.</returns>
        private static PromptOptimizationResponseEvent Fail(string requestId, string message) =>
            new(requestId, null, null, null, 0, message, false, message);

        /// <summary>
        /// Selects the model with the highest average PromptRange score for the range,
        /// falling back to the supplied model ID.
        /// </summary>
        /// <param name="rangeName">The PromptRange name.</param>
        /// <param name="defaultModelId">The fallback model identifier.</param>
        /// <returns>A task containing the selected model ID.</returns>
        private async Task<int> SelectBestModelIdAsync(string rangeName, int defaultModelId)
        {
            if (string.IsNullOrWhiteSpace(rangeName))
            {
                return defaultModelId;
            }

            var scoredItems = await _promptItemService.GetFullListAsync(
                p => p.RangeName == rangeName && p.EvalAvgScore > 0,
                p => p.Id,
                OrderingType.Ascending).ConfigureAwait(false);

            if (scoredItems == null || scoredItems.Count == 0)
            {
                return defaultModelId;
            }

            var best = scoredItems
                .GroupBy(p => p.ModelId)
                .Select(g => new { ModelId = g.Key, Avg = g.Average(x => (double)x.EvalAvgScore) })
                .OrderByDescending(x => x.Avg)
                .FirstOrDefault();

            return best?.ModelId ?? defaultModelId;
        }

        /// <summary>Removes an optional Markdown code fence around model JSON output.</summary>
        /// <param name="raw">The raw model output.</param>
        /// <returns>The unwrapped text.</returns>
        private static string StripMarkdownCodeFence(string raw)
        {
            var t = raw.Trim();
            if (!t.StartsWith("```", StringComparison.Ordinal))
            {
                return t;
            }

            var firstNl = t.IndexOf('\n');
            if (firstNl < 0)
            {
                return t;
            }

            var inner = t.Substring(firstNl + 1);
            var end = inner.LastIndexOf("```", StringComparison.Ordinal);
            if (end >= 0)
            {
                inner = inner.Substring(0, end);
            }

            return inner.Trim();
        }

        /// <summary>
        /// Parses the expected optimization JSON directly, then scans embedded JSON objects
        /// when the model added surrounding text.
        /// </summary>
        /// <param name="raw">The raw model output.</param>
        /// <param name="optimizedContent">Receives optimized prompt content.</param>
        /// <param name="modelId">Receives the optional model ID.</param>
        /// <param name="temperature">Receives temperature.</param>
        /// <param name="topP">Receives top-p.</param>
        /// <param name="maxTokens">Receives maximum tokens.</param>
        /// <param name="frequencyPenalty">Receives frequency penalty.</param>
        /// <param name="presencePenalty">Receives presence penalty.</param>
        /// <param name="reason">Receives the explanation.</param>
        /// <param name="predictedScore">Receives the predicted score.</param>
        /// <returns><see langword="true"/> when a valid payload is found.</returns>
        private static bool TryParsePayloadWithRecovery(
            string raw,
            out string optimizedContent,
            out int modelId,
            out float temperature,
            out float topP,
            out int maxTokens,
            out float frequencyPenalty,
            out float presencePenalty,
            out string reason,
            out double predictedScore)
        {
            var json = StripMarkdownCodeFence(raw);
            if (TryParsePayload(
                    json,
                    out optimizedContent,
                    out modelId,
                    out temperature,
                    out topP,
                    out maxTokens,
                    out frequencyPenalty,
                    out presencePenalty,
                    out reason,
                    out predictedScore))
            {
                return true;
            }

            var scanStart = 0;
            while (TryExtractNextJsonObject(raw, scanStart, out var candidate, out var nextScanStart))
            {
                if (TryParsePayload(
                        candidate,
                        out optimizedContent,
                        out modelId,
                        out temperature,
                        out topP,
                        out maxTokens,
                        out frequencyPenalty,
                        out presencePenalty,
                        out reason,
                        out predictedScore))
                {
                    return true;
                }

                scanStart = nextScanStart;
            }

            optimizedContent = null;
            modelId = 0;
            temperature = 0.7f;
            topP = 0.9f;
            maxTokens = 2000;
            frequencyPenalty = 0f;
            presencePenalty = 0f;
            reason = null;
            predictedScore = 0;
            return false;
        }

        /// <summary>
        /// Extracts the next balanced JSON object while respecting quoted strings and escapes.
        /// </summary>
        /// <param name="text">The text to scan.</param>
        /// <param name="startIndex">The starting scan index.</param>
        /// <param name="json">Receives the extracted JSON object.</param>
        /// <param name="nextStartIndex">Receives the next scan index.</param>
        /// <returns><see langword="true"/> when a complete object is found.</returns>
        private static bool TryExtractNextJsonObject(string text, int startIndex, out string json, out int nextStartIndex)
        {
            json = null;
            nextStartIndex = Math.Max(startIndex, 0);
            if (string.IsNullOrEmpty(text) || nextStartIndex >= text.Length)
            {
                return false;
            }

            var objectStart = text.IndexOf('{', nextStartIndex);
            while (objectStart >= 0)
            {
                var depth = 0;
                var inString = false;
                var escaped = false;

                for (var i = objectStart; i < text.Length; i++)
                {
                    var c = text[i];

                    if (inString)
                    {
                        if (escaped)
                        {
                            escaped = false;
                        }
                        else if (c == '\\')
                        {
                            escaped = true;
                        }
                        else if (c == '"')
                        {
                            inString = false;
                        }

                        continue;
                    }

                    if (c == '"')
                    {
                        inString = true;
                        continue;
                    }

                    if (c == '{')
                    {
                        depth++;
                        continue;
                    }

                    if (c == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            json = text.Substring(objectStart, i - objectStart + 1).Trim();
                            nextStartIndex = i + 1;
                            return true;
                        }
                    }
                }

                objectStart = text.IndexOf('{', objectStart + 1);
            }

            nextStartIndex = text.Length;
            return false;
        }

        /// <summary>
        /// Parses the expected optimization payload and applies safe default generation parameters.
        /// </summary>
        /// <param name="json">The candidate JSON object.</param>
        /// <param name="optimizedContent">Receives optimized prompt content.</param>
        /// <param name="modelId">Receives the optional model ID.</param>
        /// <param name="temperature">Receives temperature.</param>
        /// <param name="topP">Receives top-p.</param>
        /// <param name="maxTokens">Receives maximum tokens.</param>
        /// <param name="frequencyPenalty">Receives frequency penalty.</param>
        /// <param name="presencePenalty">Receives presence penalty.</param>
        /// <param name="reason">Receives the explanation.</param>
        /// <param name="predictedScore">Receives the predicted score.</param>
        /// <returns><see langword="true"/> when required content is valid JSON.</returns>
        private static bool TryParsePayload(
            string json,
            out string optimizedContent,
            out int modelId,
            out float temperature,
            out float topP,
            out int maxTokens,
            out float frequencyPenalty,
            out float presencePenalty,
            out string reason,
            out double predictedScore)
        {
            optimizedContent = null;
            modelId = 0;
            temperature = 0.7f;
            topP = 0.9f;
            maxTokens = 2000;
            frequencyPenalty = 0f;
            presencePenalty = 0f;
            reason = null;
            predictedScore = 0;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                optimizedContent = GetStringInsensitive(root, "optimizedContent", "content", "newPrompt");
                if (string.IsNullOrEmpty(optimizedContent))
                {
                    return false;
                }

                modelId = GetIntInsensitive(root, "modelId") ?? 0;
                temperature = GetFloatInsensitive(root, "temperature") ?? 0.7f;
                topP = GetFloatInsensitive(root, "topP", "topp") ?? 0.9f;
                maxTokens = GetIntInsensitive(root, "maxTokens", "max_token", "maxToken") ?? 2000;
                frequencyPenalty = GetFloatInsensitive(root, "frequencyPenalty") ?? 0f;
                presencePenalty = GetFloatInsensitive(root, "presencePenalty") ?? 0f;
                reason = GetStringInsensitive(root, "reason", "improvementSummary", "summary");
                predictedScore = GetDoubleInsensitive(root, "predictedScore", "score") ?? 0;

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        /// <summary>Reads a string-like JSON property using case-insensitive aliases.</summary>
        /// <param name="root">The JSON object.</param>
        /// <param name="names">Accepted property names.</param>
        /// <returns>The property text, or <see langword="null"/>.</returns>
        private static string GetStringInsensitive(JsonElement root, params string[] names)
        {
            foreach (var p in root.EnumerateObject())
            {
                foreach (var n in names)
                {
                    if (p.Name.Equals(n, StringComparison.OrdinalIgnoreCase))
                    {
                        return p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.GetRawText().Trim('"');
                    }
                }
            }

            return null;
        }

        /// <summary>Reads an integer JSON property using case-insensitive aliases.</summary>
        /// <param name="root">The JSON object.</param><param name="names">Accepted property names.</param>
        /// <returns>The parsed value, or <see langword="null"/>.</returns>
        private static int? GetIntInsensitive(JsonElement root, params string[] names)
        {
            foreach (var p in root.EnumerateObject())
            {
                foreach (var n in names)
                {
                    if (!p.Name.Equals(n, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    return p.Value.ValueKind switch
                    {
                        JsonValueKind.Number => p.Value.TryGetInt32(out var i) ? i : (int)p.Value.GetDouble(),
                        JsonValueKind.String => int.TryParse(p.Value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x) ? x : null,
                        _ => null
                    };
                }
            }

            return null;
        }

        /// <summary>Reads a floating-point JSON property using case-insensitive aliases.</summary>
        /// <param name="root">The JSON object.</param><param name="names">Accepted property names.</param>
        /// <returns>The parsed value, or <see langword="null"/>.</returns>
        private static float? GetFloatInsensitive(JsonElement root, params string[] names)
        {
            foreach (var p in root.EnumerateObject())
            {
                foreach (var n in names)
                {
                    if (!p.Name.Equals(n, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    return p.Value.ValueKind switch
                    {
                        JsonValueKind.Number => (float)p.Value.GetDouble(),
                        JsonValueKind.String => float.TryParse(p.Value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ? x : null,
                        _ => null
                    };
                }
            }

            return null;
        }

        /// <summary>Reads a double JSON property using case-insensitive aliases.</summary>
        /// <param name="root">The JSON object.</param><param name="names">Accepted property names.</param>
        /// <returns>The parsed value, or <see langword="null"/>.</returns>
        private static double? GetDoubleInsensitive(JsonElement root, params string[] names)
        {
            foreach (var p in root.EnumerateObject())
            {
                foreach (var n in names)
                {
                    if (!p.Name.Equals(n, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    return p.Value.ValueKind switch
                    {
                        JsonValueKind.Number => p.Value.GetDouble(),
                        JsonValueKind.String => double.TryParse(p.Value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ? x : null,
                        _ => null
                    };
                }
            }

            return null;
        }

        /// <summary>Unescapes common JSON string escape sequences in generated prompt content.</summary>
        /// <param name="value">The generated content.</param>
        /// <returns>The unescaped content.</returns>
        private static string UnescapeJsonString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value
                .Replace("\\\\", "\u0001")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\u0001", "\\");
        }
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatHarness.cs
    File description: Related implementation.

    Created by: Senparc - 20260906

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20260916
    Change description: v0.9.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261021
    Change description: v0.11.1 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 enhance AdminChat with recent tasks and session title generation

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Xncf.AIKernel.Domain.Models.Usage;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum AdminChatMode
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Simple = 0,

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Plan = 1,

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Harness = Plan,

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Execute = 2
    }

    /// <summary>
    /// Harness execution capabilities selected for the next request.
    /// File memory, Skills and Hosted Web Search are opt-in.
    /// </summary>
    public sealed class AdminChatHarnessOptions
    {
        public int MaxIterations { get; set; } = 32;
        public int MaxOutputTokens { get; set; } = 4000;
        public bool DisableTodoProvider { get; set; }
        public bool DisableCompaction { get; set; }
        public bool EnableFileMemory { get; set; }
        public bool EnableAgentSkillsProvider { get; set; }
        public bool EnableWebSearch { get; set; }

        public static AdminChatHarnessOptions Normalize(
            AdminChatHarnessOptions options,
            int fallbackIterations = 32,
            int fallbackOutputTokens = 4000)
        {
            options ??= new AdminChatHarnessOptions();
            return new AdminChatHarnessOptions
            {
                MaxIterations = Math.Clamp(
                    options.MaxIterations > 0 ? options.MaxIterations : fallbackIterations,
                    1,
                    64),
                MaxOutputTokens = Math.Clamp(
                    options.MaxOutputTokens > 0 ? options.MaxOutputTokens : fallbackOutputTokens,
                    256,
                    8000),
                DisableTodoProvider = options.DisableTodoProvider,
                DisableCompaction = options.DisableCompaction,
                EnableFileMemory = options.EnableFileMemory,
                EnableAgentSkillsProvider = options.EnableAgentSkillsProvider,
                EnableWebSearch = options.EnableWebSearch
            };
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public sealed class AdminChatHarnessStep
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [JsonIgnore]
        public string Prompt { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsFinal { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool TimedOut { get; set; }
    }

    /// <summary>
    /// Harness executeresult。
    /// </summary>
    public sealed class AdminChatHarnessResult
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string FinalText { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int StepsUsed { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool ReachedMaxSteps { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public IReadOnlyList<AdminChatHarnessStep> Steps { get; set; } = Array.Empty<AdminChatHarnessStep>();

        /// <summary>
        /// Durable Trajectory identifier for native MAF Harness runs.
        /// </summary>
        public int TrajectoryId { get; set; }

        public int TrajectorySequence { get; set; }

        /// <summary>
        /// Native Harness trajectory status.
        /// </summary>
        public AdminChatTrajectoryStatus Status { get; set; }

        /// <summary>
        /// Native Harness trajectory events returned for the current request.
        /// </summary>
        public IReadOnlyList<AdminChatTrajectoryEventDto> TrajectoryEvents { get; set; } = Array.Empty<AdminChatTrajectoryEventDto>();

        /// <summary>
        /// Tool approvals waiting for explicit user confirmation.
        /// </summary>
        public IReadOnlyList<AdminChatApprovalRequestDto> PendingApprovals { get; set; } = Array.Empty<AdminChatApprovalRequestDto>();
    }

    /// <summary>
    /// Safe API projection of one stored Trajectory event.
    /// </summary>
    public sealed class AdminChatTrajectoryEventDto
    {
        public int Id { get; set; }
        public int Sequence { get; set; }
        public string EventType { get; set; }
        public string Source { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public string PayloadJson { get; set; }
        public DateTime OccurredAt { get; set; }
        public string CorrelationId { get; set; }
        public bool IsReplayable { get; set; }

        public static AdminChatTrajectoryEventDto CreateFromEntity(AdminChatTrajectoryEvent entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new AdminChatTrajectoryEventDto
            {
                Id = entity.Id,
                Sequence = entity.Sequence,
                EventType = entity.EventType,
                Source = entity.Source,
                Name = entity.Name,
                Content = entity.Content,
                PayloadJson = entity.PayloadJson,
                OccurredAt = entity.OccurredAt,
                CorrelationId = entity.CorrelationId,
                IsReplayable = entity.IsReplayable
            };
        }

        /// <summary>
        /// Coalesces legacy consecutive assistant/reasoning text chunks for display and replay.
        /// New events already use one event per assistant phase.
        /// </summary>
        public static IReadOnlyList<AdminChatTrajectoryEventDto> CollapseAssistantTextChunks(
            IEnumerable<AdminChatTrajectoryEventDto> events)
        {
            var result = new List<AdminChatTrajectoryEventDto>();
            AdminChatTrajectoryEventDto current = null;

            foreach (var item in events ?? Enumerable.Empty<AdminChatTrajectoryEventDto>())
            {
                if (IsLegacyTextContentEvent(item))
                {
                    // Older runs persisted TextContent beside assistant.text for every chunk.
                    // Ignore it while keeping the current assistant phase open.
                    continue;
                }

                if (IsTextPhaseEvent(item))
                {
                    if (current != null
                        && string.Equals(current.EventType, item.EventType, StringComparison.OrdinalIgnoreCase)
                        && (string.IsNullOrEmpty(current.CorrelationId)
                            && string.IsNullOrEmpty(item.CorrelationId)
                            || !string.IsNullOrEmpty(current.CorrelationId)
                            && string.Equals(current.CorrelationId, item.CorrelationId, StringComparison.Ordinal)))
                    {
                        current.Content = GetText(current) + GetText(item);
                        current.PayloadJson = item.PayloadJson ?? current.PayloadJson;
                        current.OccurredAt = item.OccurredAt;
                        continue;
                    }

                    current = new AdminChatTrajectoryEventDto
                    {
                        Id = item.Id,
                        Sequence = item.Sequence,
                        EventType = item.EventType,
                        Source = item.Source,
                        Name = item.Name,
                        Content = GetText(item),
                        PayloadJson = item.PayloadJson,
                        OccurredAt = item.OccurredAt,
                        CorrelationId = item.CorrelationId,
                        IsReplayable = item.IsReplayable
                    };
                    result.Add(current);
                    continue;
                }

                current = null;
                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// Determines whether an event is the legacy standalone TextContent form
        /// that should not be duplicated during replay normalization.
        /// </summary>
        /// <param name="item">The trajectory event.</param>
        /// <returns><see langword="true"/> for recognized legacy text event types.</returns>
        private static bool IsLegacyTextContentEvent(AdminChatTrajectoryEventDto item)
        {
            return item != null
                && (string.Equals(item.EventType, "TextContent", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.EventType, "Microsoft.Extensions.AI.TextContent", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.EventType, "text", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Determines whether an event belongs to an assistant text/reasoning phase
        /// that can be coalesced during replay.
        /// </summary>
        /// <param name="item">The trajectory event.</param>
        /// <returns><see langword="true"/> for recognized phase event types.</returns>
        private static bool IsTextPhaseEvent(AdminChatTrajectoryEventDto item)
        {
            return item != null
                && (string.Equals(item.EventType, "assistant.text", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.EventType, "TextReasoningContent", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.EventType, "Microsoft.Extensions.AI.TextReasoningContent", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Extracts text from event content or compatible legacy payload JSON.
        /// Malformed payloads produce an empty string.
        /// </summary>
        /// <param name="item">The trajectory event.</param>
        /// <returns>The extracted text.</returns>
        private static string GetText(AdminChatTrajectoryEventDto item)
        {
            if (!string.IsNullOrEmpty(item?.Content))
            {
                return item.Content;
            }

            if (string.IsNullOrEmpty(item?.PayloadJson))
            {
                return string.Empty;
            }

            try
            {
                using var payload = JsonDocument.Parse(item.PayloadJson);
                if (payload.RootElement.ValueKind == JsonValueKind.Object
                    && (payload.RootElement.TryGetProperty("Text", out var text)
                        || payload.RootElement.TryGetProperty("text", out text))
                    && text.ValueKind == JsonValueKind.String)
                {
                    return text.GetString() ?? string.Empty;
                }
            }
            catch (JsonException)
            {
                // Keep malformed legacy payloads unchanged.
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// A pending MAF tool approval rendered for the Admin Chat user.
    /// </summary>
    public sealed class AdminChatApprovalRequestDto
    {
        public string RequestId { get; set; }
        public string ToolCallId { get; set; }
        public string ToolName { get; set; }
        public string ArgumentsJson { get; set; }
    }

    public sealed class AdminChatLiveEvent
    {
        public int TrajectoryId { get; set; }
        public string Kind { get; set; }
        public string Text { get; set; }
        public AITokenUsageSnapshot Usage { get; set; }
        public AdminChatTrajectoryEventDto TrajectoryEvent { get; set; }
        public IReadOnlyList<AdminChatApprovalRequestDto> PendingApprovals { get; set; } = Array.Empty<AdminChatApprovalRequestDto>();
    }

    /// <summary>
    /// Safe API projection of a durable Harness Trajectory.
    /// </summary>
    public sealed class AdminChatTrajectoryDto
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public AdminChatMode Mode { get; set; }
        public AdminChatTrajectoryStatus Status { get; set; }
        public string Title { get; set; }
        public int? ParentTrajectoryId { get; set; }
        public int? ForkFromSequence { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int LastSequence { get; set; }
        public string ModelIdentifier { get; set; }

        public static AdminChatTrajectoryDto CreateFromEntity(AdminChatTrajectory entity)
        {
            if (entity == null)
            {
                return null;
            }

            return new AdminChatTrajectoryDto
            {
                Id = entity.Id,
                SessionId = entity.SessionId,
                Mode = entity.Mode,
                Status = entity.Status,
                Title = entity.Title,
                ParentTrajectoryId = entity.ParentTrajectoryId,
                ForkFromSequence = entity.ForkFromSequence,
                StartedAt = entity.StartedAt,
                FinishedAt = entity.FinishedAt,
                LastSequence = entity.LastSequence,
                ModelIdentifier = entity.ModelIdentifier
            };
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// <para>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </para>
    /// </summary>
    public sealed class AdminChatHarnessExecutor
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public const string ContinueToken = "[[CONTINUE]]";

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public const string DoneToken = "[[DONE]]";

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int MaxSteps { get; set; } = 8;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(120);

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public Func<int, string, CancellationToken, Task<string>> TurnExecutor { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        public async Task<AdminChatHarnessResult> RunAsync(
            string firstTurnPrompt,
            string continuePrompt,
            Action<AdminChatHarnessStep> onStep = null,
            CancellationToken cancellationToken = default)
        {
            if (TurnExecutor == null)
            {
                throw new InvalidOperationException("AdminChatHarnessExecutor.TurnExecutor 未设置。");
            }

            if (MaxSteps < 1)
            {
                MaxSteps = 1;
            }

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(Timeout);
            var token = linkedCts.Token;

            var steps = new List<AdminChatHarnessStep>();
            string finalText = null;
            var completed = false;
            var used = 0;

            for (var i = 1; i <= MaxSteps; i++)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }

                var prompt = i == 1 ? firstTurnPrompt : continuePrompt;
                string output;
                try
                {
                    output = await TurnExecutor(i, prompt, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }

                var trimmed = (output ?? string.Empty).Trim();
                var isDone = ContainsToken(trimmed, DoneToken);
                output = StripMarkers(string.IsNullOrWhiteSpace(trimmed) ? "(模型未返回有效内容)" : trimmed);
                used = i;
                var step = new AdminChatHarnessStep
                {
                    Index = i,
                    Prompt = prompt,
                    Output = output,
                    IsFinal = isDone,
                    TimedOut = token.IsCancellationRequested
                };
                steps.Add(step);
                onStep?.Invoke(step);
                finalText = output;

                if (isDone)
                {
                    completed = true;
                    break;
                }
            }

            if (finalText == null && steps.Count > 0)
            {
                finalText = steps[steps.Count - 1].Output;
            }

            return new AdminChatHarnessResult
            {
                FinalText = finalText ?? string.Empty,
                StepsUsed = used,
                Completed = completed,
                ReachedMaxSteps = !completed && used >= MaxSteps,
                Steps = steps
            };
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static bool ContainsToken(string text, string token)
        {
            return !string.IsNullOrEmpty(text) && text.Contains(token, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static string StripMarkers(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            var cleaned = text
                .Replace("[[CONTINUE]]", " ")
                .Replace("[[continue]]", " ")
                .Replace("[[DONE]]", " ")
                .Replace("[[done]]", " ");

            cleaned = Regex.Replace(cleaned, @"[ \t]+\n", "\n");
            cleaned = Regex.Replace(cleaned, @"\n{3,}", "\n\n");
            return cleaned.Trim();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static string BuildProtocolInstructions()
        {
            return "\n\n## 长任务执行协议（Harness Mode）\n"
                + "你需要通过多步骤、自主执行的方式完成用户的长任务。每一步：\n"
                + "1) 先简要说明本步要做什么；\n"
                + "2) 如需要，调用可用的函数/工具获取数据或执行操作；\n"
                + "3) 给出本步小结。\n"
                + "在本步结尾必须单独一行输出控制标记：任务尚未完成输出 " + ContinueToken + "；任务已全部完成输出 " + DoneToken + " 并给出最终完整答案。\n";
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static string BuildStartTail()
        {
            return "\n\n请开始执行上述任务，并按“长任务执行协议”在结尾单独一行输出 " + ContinueToken + " 或 " + DoneToken + "。";
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static string BuildContinuePrompt()
        {
            return "请继续执行任务：先概述上一步的进展与当前整体状态，再执行下一步。"
                + "在本步结尾单独一行输出 " + ContinueToken + "（继续）或 " + DoneToken + "（完成并给出最终完整答案）。";
        }
    }
}

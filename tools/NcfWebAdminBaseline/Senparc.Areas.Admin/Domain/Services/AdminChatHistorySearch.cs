/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatHistorySearch.cs
    File description: Related implementation.

    Created by: Senparc - 20261110

    Modified by: Senparc - 20261110
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261110
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.12.28 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;

namespace Senparc.Areas.Admin.Domain.Services
{
    public enum AdminChatHistorySearchMatchKind
    {
        Content = 0,
        Title = 1,
        ToolCall = 2
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public sealed class AdminChatHistorySearchHit
    {
        public int SessionId { get; set; }
        public string Title { get; set; }
        public int MessageId { get; set; }
        public ChatMessageRoleType RoleType { get; set; }
        public AdminChatHistorySearchMatchKind MatchKind { get; set; }
        public DateTime OccurredAt { get; set; }
        public string SnippetBefore { get; set; }
        public string SnippetMatch { get; set; }
        public string SnippetAfter { get; set; }
        public bool TitleMatched { get; set; }
        public int TrajectoryId { get; set; }
        public int EventSequence { get; set; }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static class AdminChatHistorySearch
    {
        public const int DefaultLimit = 10;
        public const int MaxKeywordLength = 80;
        private const int SnippetRadius = 42;

        /// <summary>
        /// Trims a search keyword and caps it at the supported length.
        /// </summary>
        /// <param name="keyword">The raw search keyword.</param>
        /// <returns>The normalized keyword.</returns>
        public static string NormalizeKeyword(string keyword)
        {
            var normalized = (keyword ?? string.Empty).Trim();
            return normalized.Length <= MaxKeywordLength
                ? normalized
                : normalized.Substring(0, MaxKeywordLength);
        }

        /// <summary>
        /// Searches active owner-scoped chat sessions by message content or session title,
        /// creates bounded snippets, and returns newest matches first.
        /// </summary>
        /// <param name="sessions">The candidate chat sessions.</param>
        /// <param name="messages">The candidate chat messages.</param>
        /// <param name="userId">The owning administrator identifier.</param>
        /// <param name="keyword">The search keyword.</param>
        /// <param name="limit">The maximum result count, capped by the default limit.</param>
        /// <returns>Matching content/title hits.</returns>
        public static IReadOnlyList<AdminChatHistorySearchHit> Search(
            IEnumerable<AdminChatSession> sessions,
            IEnumerable<AdminChatMessage> messages,
            int userId,
            string keyword,
            int limit = DefaultLimit)
        {
            var normalized = NormalizeKeyword(keyword);
            var take = limit <= 0 ? 0 : Math.Min(limit, DefaultLimit);
            if (userId <= 0 || normalized.Length == 0 || take == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var activeSessions = SelectActiveSessions(sessions, userId);
            if (activeSessions.Count == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var hits = new List<AdminChatHistorySearchHit>();
            var sessionsWithContent = new HashSet<int>();
            foreach (var message in messages ?? Array.Empty<AdminChatMessage>())
            {
                if (message == null || message.Flag || string.IsNullOrEmpty(message.Content)
                    || !activeSessions.TryGetValue(message.SessionId, out var session)
                    || message.Content.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                SplitSnippet(message.Content, normalized, out var before, out var match, out var after);
                sessionsWithContent.Add(session.Id);
                hits.Add(new AdminChatHistorySearchHit
                {
                    SessionId = session.Id,
                    Title = session.Title,
                    MessageId = message.Id,
                    RoleType = message.RoleType,
                    MatchKind = AdminChatHistorySearchMatchKind.Content,
                    OccurredAt = message.AddTime,
                    SnippetBefore = before,
                    SnippetMatch = match,
                    SnippetAfter = after,
                    TitleMatched = session.Title?.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0
                });
            }

            foreach (var session in activeSessions.Values)
            {
                if (sessionsWithContent.Contains(session.Id)
                    || string.IsNullOrEmpty(session.Title)
                    || session.Title.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                SplitSnippet(session.Title, normalized, out var before, out var match, out var after);
                hits.Add(new AdminChatHistorySearchHit
                {
                    SessionId = session.Id,
                    Title = session.Title,
                    MessageId = 0,
                    MatchKind = AdminChatHistorySearchMatchKind.Title,
                    OccurredAt = session.LastMessageTime,
                    SnippetBefore = before,
                    SnippetMatch = match,
                    SnippetAfter = after,
                    TitleMatched = true
                });
            }

            return hits
                .OrderByDescending(hit => hit.OccurredAt)
                .ThenByDescending(hit => hit.MessageId)
                .Take(take)
                .ToList();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <summary>
        /// Searches active owner-scoped trajectory tool-call events by tool name
        /// and maps each match to its nearest chat message.
        /// </summary>
        /// <param name="sessions">The candidate chat sessions.</param>
        /// <param name="trajectories">The candidate trajectories.</param>
        /// <param name="events">The candidate trajectory events.</param>
        /// <param name="messages">The candidate chat messages.</param>
        /// <param name="userId">The owning administrator identifier.</param>
        /// <param name="keyword">The tool-name keyword.</param>
        /// <param name="limit">The maximum result count.</param>
        /// <returns>Matching tool-call hits.</returns>
        public static IReadOnlyList<AdminChatHistorySearchHit> SearchToolCalls(
            IEnumerable<AdminChatSession> sessions,
            IEnumerable<AdminChatTrajectory> trajectories,
            IEnumerable<AdminChatTrajectoryEvent> events,
            IEnumerable<AdminChatMessage> messages,
            int userId,
            string keyword,
            int limit = DefaultLimit)
        {
            var normalized = NormalizeKeyword(keyword);
            var take = limit <= 0 ? 0 : Math.Min(limit, DefaultLimit);
            var activeSessions = SelectActiveSessions(sessions, userId);
            if (userId <= 0 || normalized.Length == 0 || take == 0 || activeSessions.Count == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var activeTrajectories = (trajectories ?? Array.Empty<AdminChatTrajectory>())
                .Where(trajectory => trajectory != null
                    && !trajectory.Flag
                    && trajectory.UserId == userId
                    && activeSessions.ContainsKey(trajectory.SessionId))
                .GroupBy(trajectory => trajectory.Id)
                .Select(group => group.First())
                .ToDictionary(trajectory => trajectory.Id);
            if (activeTrajectories.Count == 0)
            {
                return Array.Empty<AdminChatHistorySearchHit>();
            }

            var hits = new List<AdminChatHistorySearchHit>();
            foreach (var item in events ?? Array.Empty<AdminChatTrajectoryEvent>())
            {
                if (item == null || item.Flag
                    || !string.Equals(item.EventType, "tool.call", StringComparison.Ordinal)
                    || string.IsNullOrEmpty(item.Name)
                    || item.Name.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) < 0
                    || !activeTrajectories.TryGetValue(item.TrajectoryId, out var trajectory))
                {
                    continue;
                }

                var session = activeSessions[trajectory.SessionId];
                SplitSnippet(item.Name, normalized, out var before, out var match, out var after);
                hits.Add(new AdminChatHistorySearchHit
                {
                    SessionId = session.Id,
                    Title = session.Title,
                    MessageId = LocateToolCallMessage(messages, item.TrajectoryId, item.Sequence),
                    MatchKind = AdminChatHistorySearchMatchKind.ToolCall,
                    OccurredAt = item.OccurredAt,
                    SnippetBefore = before,
                    SnippetMatch = match,
                    SnippetAfter = after,
                    TrajectoryId = item.TrajectoryId,
                    EventSequence = item.Sequence
                });
            }

            return hits
                .OrderByDescending(hit => hit.OccurredAt)
                .ThenByDescending(hit => hit.EventSequence)
                .Take(take)
                .ToList();
        }

        /// <summary>
        /// Selects distinct active, non-deleted sessions owned by the user.
        /// </summary>
        /// <param name="sessions">The candidate sessions.</param>
        /// <param name="userId">The owner identifier.</param>
        /// <returns>Sessions keyed by identifier.</returns>
        private static Dictionary<int, AdminChatSession> SelectActiveSessions(
            IEnumerable<AdminChatSession> sessions,
            int userId)
        {
            return (sessions ?? Array.Empty<AdminChatSession>())
                .Where(session => session != null
                    && session.UserId == userId
                    && !session.Flag
                    && session.Status == ChatSessionStatus.Active)
                .GroupBy(session => session.Id)
                .Select(group => group.First())
                .ToDictionary(session => session.Id);
        }

        /// <summary>
        /// Finds the nearest message covering a trajectory event sequence.
        /// </summary>
        /// <param name="messages">The candidate messages.</param>
        /// <param name="trajectoryId">The trajectory identifier.</param>
        /// <param name="sequence">The event sequence.</param>
        /// <returns>The nearest message identifier, or zero.</returns>
        private static int LocateToolCallMessage(
            IEnumerable<AdminChatMessage> messages,
            int trajectoryId,
            int sequence)
        {
            var related = (messages ?? Array.Empty<AdminChatMessage>())
                .Where(message => message != null && !message.Flag && message.TrajectoryId == trajectoryId)
                .ToList();
            var covering = related
                .Where(message => message.TrajectorySequence >= sequence)
                .OrderBy(message => message.TrajectorySequence)
                .ThenBy(message => message.Id)
                .FirstOrDefault();
            if (covering != null)
            {
                return covering.Id;
            }

            return related
                .OrderByDescending(message => message.TrajectorySequence)
                .ThenByDescending(message => message.Id)
                .FirstOrDefault()?.Id ?? 0;
        }

        /// <summary>
        /// Extracts a collapsed context snippet around a case-insensitive keyword match.
        /// </summary>
        /// <param name="text">The source text.</param>
        /// <param name="keyword">The keyword to locate.</param>
        /// <param name="before">Receives text before the match.</param>
        /// <param name="match">Receives the matched text.</param>
        /// <param name="after">Receives text after the match.</param>
        private static void SplitSnippet(string text, string keyword, out string before, out string match, out string after)
        {
            text ??= string.Empty;
            var index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                before = Collapse(text.Length > SnippetRadius * 2 ? text.Substring(0, SnippetRadius * 2) : text).Trim();
                match = string.Empty;
                after = text.Length > SnippetRadius * 2 ? "…" : string.Empty;
                return;
            }

            var matchEnd = index + keyword.Length;
            var start = Math.Max(0, index - SnippetRadius);
            var end = Math.Min(text.Length, matchEnd + SnippetRadius);
            before = Collapse(text.Substring(start, index - start));
            match = Collapse(text.Substring(index, keyword.Length));
            after = Collapse(text.Substring(matchEnd, end - matchEnd));
            if (start > 0)
            {
                before = "…" + before.TrimStart();
            }
            else
            {
                before = before.TrimStart();
            }

            after = end < text.Length ? after.TrimEnd() + "…" : after.TrimEnd();
        }

        /// <summary>
        /// Replaces whitespace runs with single spaces for compact search snippets.
        /// </summary>
        /// <param name="value">The text to collapse.</param>
        /// <returns>Whitespace-collapsed text.</returns>
        private static string Collapse(string value) =>
            string.IsNullOrEmpty(value) ? string.Empty : Regex.Replace(value, @"\s+", " ");
    }
}

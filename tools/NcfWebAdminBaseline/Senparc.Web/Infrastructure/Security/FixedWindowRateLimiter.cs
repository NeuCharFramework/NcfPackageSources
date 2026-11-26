/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FixedWindowRateLimiter.cs
    File description: Related implementation.

    Created by: Senparc - 20260906

    Modified by: Senparc - 20260915
    Change description: v0.39.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.39.9 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;

namespace Senparc.Web.Infrastructure.Security
{
    /// <summary>
    /// Tracks request counts in independent fixed time windows for a bounded
    /// in-process rate-limit decision.
    /// </summary>
    public sealed class FixedWindowRateLimiter
    {
        private readonly ConcurrentDictionary<string, (long Window, long Count)> _store = new();

        /// <summary>
        /// Records one request for <paramref name="key"/> and determines whether
        /// the key has exceeded the configured request limit in the current window.
        /// </summary>
        /// <param name="key">The identifier whose request count is incremented.</param>
        /// <param name="now">The timestamp used to select the current fixed window.</param>
        /// <param name="maxRequests">The maximum allowed requests in the current window.</param>
        /// <param name="window">The fixed-window duration.</param>
        /// <returns>
        /// <see langword="true"/> when the incremented count exceeds
        /// <paramref name="maxRequests"/>; otherwise, <see langword="false"/>.
        /// Invalid or non-positive limits are treated as unrestricted.
        /// </returns>
        public bool IsLimited(string key, DateTimeOffset now, int maxRequests, TimeSpan window)
        {
            if (string.IsNullOrEmpty(key) || maxRequests <= 0 || window <= TimeSpan.Zero)
            {
                return false;
            }

            var windowMs = Math.Max(1, (long)window.TotalMilliseconds);
            var windowIndex = now.ToUnixTimeMilliseconds() / windowMs;

            long count = 0;
            _store.AddOrUpdate(
                key,
                _ =>
                {
                    count = 1;
                    return (windowIndex, 1L);
                },
                (_, existing) =>
                {
                    if (existing.Window == windowIndex)
                    {
                        count = existing.Count + 1;
                        return (existing.Window, existing.Count + 1);
                    }

                    count = 1;
                    return (windowIndex, 1L);
                });

            return count > maxRequests;
        }

        /// <summary>
        /// Removes entries whose fixed window is older than the current window.
        /// </summary>
        /// <param name="now">The timestamp used to identify the current window.</param>
        /// <param name="window">The fixed-window duration.</param>
        /// <returns>The number of stale keys removed.</returns>
        public int Prune(DateTimeOffset now, TimeSpan window)
        {
            if (window <= TimeSpan.Zero)
            {
                return 0;
            }

            var windowMs = Math.Max(1, (long)window.TotalMilliseconds);
            var currentIndex = now.ToUnixTimeMilliseconds() / windowMs;
            var removed = 0;
            foreach (var kv in _store)
            {
                if (kv.Value.Window < currentIndex && _store.TryRemove(kv.Key, out _))
                {
                    removed++;
                }
            }

            return removed;
        }
    }
}

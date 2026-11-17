/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FixedWindowRateLimiter.cs
    File description: Related implementation.

    Created by: Senparc - 20260906

    Modified by: Senparc - 20260915
    Change description: v0.39.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;

namespace Senparc.Web.Infrastructure.Security
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public sealed class FixedWindowRateLimiter
    {
        private readonly ConcurrentDictionary<string, (long Window, long Count)> _store = new();

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
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

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CloudflareProtectMiddleware.cs
    File description: Related implementation.

    Created by: Senparc - 20260906

    Modified by: Senparc - 20260915
    Change description: v0.39.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.39.9 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Senparc.Web.Infrastructure.Security
{
    /// <summary>
    /// Applies optional security headers and fixed-window rate limiting to
    /// configured request paths.
    /// <para>
    /// Requests exceeding the configured limit receive HTTP 429 and are not
    /// passed to the downstream pipeline.
    /// </para>
    /// </summary>
    public sealed class CloudflareProtectMiddleware
    {
        private readonly RequestDelegate _next;

        private DateTime _lastPruneUtc = DateTime.MinValue;

        /// <summary>
        /// Initializes the middleware with the downstream request delegate.
        /// </summary>
        /// <param name="next">The next middleware component in the pipeline.</param>
        public CloudflareProtectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Applies configured headers and rate limiting, then invokes the downstream
        /// pipeline when the request is allowed.
        /// </summary>
        /// <param name="context">The current HTTP request context.</param>
        /// <returns>A task representing middleware execution.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var options = context.RequestServices.GetRequiredService<IOptions<CloudflareProtectOptions>>().Value;
            if (!options.Enabled)
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            if (options.AddSecurityHeaders)
            {
                ApplySecurityHeaders(context.Response);
            }

            if (IsPathProtected(context.Request.Path, options))
            {
                var limiter = context.RequestServices.GetRequiredService<FixedWindowRateLimiter>();
                var window = TimeSpan.FromSeconds(Math.Max(1, options.WindowSeconds));
                var now = DateTimeOffset.UtcNow;
                PruneIfNeeded(limiter, window, now);

                var clientKey = GetClientKey(context);
                if (limiter.IsLimited(clientKey, now, options.MaxRequestsPerWindow, window))
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.Response.Headers["Retry-After"] = Math.Max(1, options.WindowSeconds).ToString();
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "rate_limited",
                        message = "请求过于频繁，请稍后再试。"
                    }).ConfigureAwait(false);
                    return;
                }
            }

            await _next(context).ConfigureAwait(false);
        }

        /// <summary>
        /// Determines whether a request path matches the configured protected prefixes.
        /// </summary>
        /// <param name="path">The request path.</param>
        /// <param name="options">The active protection options.</param>
        /// <returns><see langword="true"/> when the path should be rate limited.</returns>
        private static bool IsPathProtected(PathString path, CloudflareProtectOptions options)
        {
            var prefixes = options.ProtectedPathPrefixes;
            if (prefixes == null || prefixes.Count == 0)
            {
                return true;
            }

            return prefixes.Any(p => !string.IsNullOrWhiteSpace(p) && path.StartsWithSegments(p));
        }

        /// <summary>
        /// Gets a stable client key from the remote connection address.
        /// </summary>
        /// <param name="context">The current HTTP request context.</param>
        /// <returns>The normalized IPv4 address, or <c>unknown</c>.</returns>
        private static string GetClientKey(HttpContext context)
        {
            var remote = context.Connection.RemoteIpAddress;
            return remote?.MapToIPv4()?.ToString() ?? "unknown";
        }

        /// <summary>
        /// Periodically removes expired limiter entries to bound in-process state.
        /// </summary>
        /// <param name="limiter">The limiter whose stale entries are removed.</param>
        /// <param name="window">The active rate-limit window.</param>
        /// <param name="now">The current timestamp.</param>
        private void PruneIfNeeded(FixedWindowRateLimiter limiter, TimeSpan window, DateTimeOffset now)
        {
            var nowUtc = now.UtcDateTime;
            if ((nowUtc - _lastPruneUtc).TotalSeconds < 1)
            {
                return;
            }

            lock (this)
            {
                if ((nowUtc - _lastPruneUtc).TotalSeconds < 1)
                {
                    return;
                }

                _lastPruneUtc = nowUtc;
            }

            limiter.Prune(now, window);
        }

        /// <summary>
        /// Adds the response headers selected by the protection options.
        /// </summary>
        /// <param name="response">The response receiving the headers.</param>
        private static void ApplySecurityHeaders(HttpResponse response)
        {
            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            response.Headers["X-XSS-Protection"] = "0";
        }
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxTtlPolicy.cs
    File description: Related implementation.

    Created by: Senparc - 20260817

    Modified by: Senparc - 20260817
    Change description: v0.2.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

----------------------------------------------------------------*/

using System.Globalization;

namespace Senparc.Xncf.Sandbox.Domain.Services;

/// <summary>
/// Resolves the application-owned lifetime of a sandbox session.
/// Docker does not enforce this TTL.
/// </summary>
public static class SandboxTtlPolicy
{
    public static readonly DateTime UnlimitedExpiresAtUtc =
        DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);

    public static DateTime ResolveExpiresAtUtc(
        DateTime utcNow,
        TimeSpan defaultTtl,
        TimeSpan maxTtl,
        int? ttlMinutes,
        bool keepAlive)
    {
        if (keepAlive)
        {
            return UnlimitedExpiresAtUtc;
        }

        var ttl = ttlMinutes.HasValue
            ? TimeSpan.FromMinutes(ttlMinutes.Value)
            : defaultTtl;

        if (ttl <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("TTL 必须大于 0 分钟。");
        }

        if (ttl > maxTtl)
        {
            throw new InvalidOperationException(
                $"TTL 不能超过 {maxTtl.TotalMinutes.ToString("0", CultureInfo.InvariantCulture)} 分钟；如需持续运行，请选择永久保持。");
        }

        return utcNow.Add(ttl);
    }

    /// <summary>
    /// Determines whether a Sandbox expiration timestamp represents unlimited retention.
    /// </summary>
    /// <param name="expiresAtUtc">The expiration timestamp.</param>
    /// <returns><see langword="true"/> when the timestamp is <see cref="DateTime.MaxValue"/>.</returns>
    public static bool IsUnlimited(DateTime expiresAtUtc)
    {
        return expiresAtUtc.Ticks == DateTime.MaxValue.Ticks;
    }
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SandboxTtlPolicy.cs
    文件功能描述：沙箱 TTL 策略
    
    
    创建标识：Senparc - 20260817
    
    修改标识：Senparc - 20260817
    修改描述：v0.2.0 新增应用侧 TTL/永久保持解析策略

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

    public static bool IsUnlimited(DateTime expiresAtUtc)
    {
        return expiresAtUtc.Ticks == DateTime.MaxValue.Ticks;
    }
}

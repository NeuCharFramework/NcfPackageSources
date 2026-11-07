/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LoginIpThrottleService.cs
    文件功能描述：登录接口按 IP 滑动窗口限流（防密码暴力破解与验证码反复刷取重试）

    创建标识：Senparc - 20261105
    创建描述：登录/验证码端点按客户端 IP 做滑动窗口计数，超阈值返回 429 + Retry-After；
              窗口状态存储抽象为 ILoginRateLimitStore，默认 CO2NET 对象缓存实现（带缓存锁）

-----------------------------------------------------------

    修改标识：Senparc - 20261108
    修改描述：v0.13.0 登录端点 IP 滑动窗口限流
----------------------------------------------------------------*/

using Microsoft.Extensions.Configuration;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using System;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// 滑动窗口限流判定结果
    /// </summary>
    public class ThrottleResult
    {
        public bool Allowed { get; }
        public int RetryAfterSeconds { get; }

        private ThrottleResult(bool allowed, int retryAfterSeconds)
        {
            Allowed = allowed;
            RetryAfterSeconds = retryAfterSeconds;
        }

        public static ThrottleResult Ok()
        {
            return new ThrottleResult(true, 0);
        }

        public static ThrottleResult Blocked(int retryAfterSeconds)
        {
            return new ThrottleResult(false, Math.Max(1, retryAfterSeconds));
        }
    }

    /// <summary>
    /// 单个限流窗口状态
    /// </summary>
    public class RateLimitWindow
    {
        public int Count { get; set; }
        public DateTime WindowStartUtc { get; set; }
    }

    /// <summary>
    /// 限流窗口存储（抽象便于单元测试；生产实现为 CO2NET 对象缓存）
    /// </summary>
    public interface ILoginRateLimitStore
    {
        /// <summary>
        /// 原子地推进计数：窗口已过期则重置为 1，否则累加，返回推进后的状态
        /// </summary>
        Task<RateLimitWindow> IncrementAsync(string key, TimeSpan window);
    }

    /// <summary>
    /// CO2NET 对象缓存实现（缓存锁内完成读-改-写，避免并发窗口漂移）
    /// </summary>
    public class Co2NetLoginRateLimitStore : ILoginRateLimitStore
    {
        private const string LockResource = "LoginThrottle";
        private static readonly TimeSpan WindowTtl = TimeSpan.FromHours(2);

        private readonly IBaseObjectCacheStrategy _cache =
            CO2NET.Cache.CacheStrategyFactory.GetObjectCacheStrategyInstance();

        public async Task<RateLimitWindow> IncrementAsync(string key, TimeSpan window)
        {
            using (var cacheLock = await _cache.BeginCacheLockAsync(LockResource, key))
            {
                var state = await _cache.GetAsync<RateLimitWindow>(key);
                var now = DateTime.UtcNow;
                if (state == null || (now - state.WindowStartUtc) >= window)
                {
                    state = new RateLimitWindow
                    {
                        Count = 0,
                        WindowStartUtc = now
                    };
                }
                state.Count++;
                await _cache.SetAsync(key, state, WindowTtl);
                return state;
            }
        }
    }

    /// <summary>
    /// 登录接口 IP 限流服务：
    /// - login 动作：同一 IP 默认 20 次 / 10 分钟（AdminLogin:Throttle:LoginMaxPerWindow / LoginWindowMinutes 可覆盖）
    /// - captcha 动作：同一 IP 默认 30 次 / 10 分钟（AdminLogin:Throttle:CaptchaMaxPerWindow / CaptchaWindowMinutes 可覆盖）
    /// 超阈值时返回 429 与 Retry-After 秒数，使“无限重试”在客户端维度被直接掐断。
    /// </summary>
    public class LoginIpThrottleService
    {
        public const string ActionLogin = "login";
        public const string ActionCaptcha = "captcha";

        private readonly ILoginRateLimitStore _store;
        private readonly IConfiguration _configuration;

        public LoginIpThrottleService(ILoginRateLimitStore store, IConfiguration configuration)
        {
            _store = store;
            _configuration = configuration;
        }

        public async Task<ThrottleResult> TryConsumeAsync(string clientKey, string action)
        {
            if (clientKey.IsNullOrEmpty())
            {
                return ThrottleResult.Ok();
            }

            var (maxCount, window) = GetPolicy(action);
            var cacheKey = $"LoginThrottle:{action}:{clientKey}";
            var state = await _store.IncrementAsync(cacheKey, window);
            if (state.Count <= maxCount)
            {
                return ThrottleResult.Ok();
            }

            var retryAfter = (int)Math.Ceiling((state.WindowStartUtc + window - DateTime.UtcNow).TotalSeconds);
            return ThrottleResult.Blocked(retryAfter);
        }

        private (int MaxCount, TimeSpan Window) GetPolicy(string action)
        {
            int maxCount;
            int windowMinutes;
            if (action == ActionCaptcha)
            {
                maxCount = _configuration.GetValue("AdminLogin:Throttle:CaptchaMaxPerWindow", 30);
                windowMinutes = _configuration.GetValue("AdminLogin:Throttle:CaptchaWindowMinutes", 10);
            }
            else
            {
                maxCount = _configuration.GetValue("AdminLogin:Throttle:LoginMaxPerWindow", 20);
                windowMinutes = _configuration.GetValue("AdminLogin:Throttle:LoginWindowMinutes", 10);
            }
            return (maxCount, TimeSpan.FromMinutes(Math.Max(1, windowMinutes)));
        }
    }
}

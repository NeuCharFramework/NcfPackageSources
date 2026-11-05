using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Areas.Admin.Domain.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class LoginIpThrottleTests
{
    private class FakeStore : ILoginRateLimitStore
    {
        private readonly ConcurrentDictionary<string, RateLimitWindow> _data = new();

        public Task<RateLimitWindow> IncrementAsync(string key, TimeSpan window)
        {
            var now = DateTime.UtcNow;
            var state = _data.TryGetValue(key, out var existing) && (now - existing.WindowStartUtc) < window
                ? existing
                : new RateLimitWindow { Count = 0, WindowStartUtc = now };
            state.Count++;
            _data[key] = state;
            return Task.FromResult(state);
        }
    }

    private static LoginIpThrottleService CreateService(FakeStore store, Dictionary<string, string> settings = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string>())
            .Build();
        return new LoginIpThrottleService(store, config);
    }

    [TestMethod]
    public async Task Login_AllowsUpToDefaultMaxThenBlocks()
    {
        var store = new FakeStore();
        var service = CreateService(store);
        for (var i = 0; i < 20; i++)
        {
            var result = await service.TryConsumeAsync("1.2.3.4", LoginIpThrottleService.ActionLogin);
            Assert.IsTrue(result.Allowed, $"第 {i + 1} 次应放行");
        }
        var blocked = await service.TryConsumeAsync("1.2.3.4", LoginIpThrottleService.ActionLogin);
        Assert.IsFalse(blocked.Allowed);
        Assert.IsTrue(blocked.RetryAfterSeconds > 0);
    }

    [TestMethod]
    public async Task Login_DifferentIpsHaveSeparateWindows()
    {
        var store = new FakeStore();
        var service = CreateService(store);
        for (var i = 0; i < 20; i++)
        {
            await service.TryConsumeAsync("1.1.1.1", LoginIpThrottleService.ActionLogin);
        }
        Assert.IsFalse((await service.TryConsumeAsync("1.1.1.1", LoginIpThrottleService.ActionLogin)).Allowed);
        Assert.IsTrue((await service.TryConsumeAsync("2.2.2.2", LoginIpThrottleService.ActionLogin)).Allowed);
    }

    [TestMethod]
    public async Task Login_ConfigOverridesAreRespected()
    {
        var store = new FakeStore();
        var service = CreateService(store, new Dictionary<string, string>
        {
            ["AdminLogin:Throttle:LoginMaxPerWindow"] = "3"
        });
        for (var i = 0; i < 3; i++)
        {
            Assert.IsTrue((await service.TryConsumeAsync("3.3.3.3", LoginIpThrottleService.ActionLogin)).Allowed);
        }
        Assert.IsFalse((await service.TryConsumeAsync("3.3.3.3", LoginIpThrottleService.ActionLogin)).Allowed);
    }

    [TestMethod]
    public async Task Login_EmptyKeyIsNotThrottled()
    {
        var store = new FakeStore();
        var service = CreateService(store);
        for (var i = 0; i < 50; i++)
        {
            Assert.IsTrue((await service.TryConsumeAsync("", LoginIpThrottleService.ActionLogin)).Allowed);
        }
    }

    [TestMethod]
    public async Task Captcha_ActionUsesItsOwnWindow()
    {
        var store = new FakeStore();
        var service = CreateService(store);
        for (var i = 0; i < 30; i++)
        {
            Assert.IsTrue((await service.TryConsumeAsync("4.4.4.4", LoginIpThrottleService.ActionCaptcha)).Allowed);
        }
        Assert.IsFalse((await service.TryConsumeAsync("4.4.4.4", LoginIpThrottleService.ActionCaptcha)).Allowed);
        Assert.IsTrue((await service.TryConsumeAsync("4.4.4.4", LoginIpThrottleService.ActionLogin)).Allowed);
    }
}

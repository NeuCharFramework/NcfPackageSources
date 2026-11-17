using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.Cache.Tests
{
    [TestClass]
    public class MethodCacheTests : TestBase
    {
        [TestMethod]
        public async Task GetMethodCacheAsync_ShouldReturnCachedValue_WhenCacheExists()
        {
            // Arrange  
            var cacheKey = "TestKey";
            var expectedValue = new TestClass { Property = "CachedValue" };

            var cacheStrategy = base._serviceProvider.GetService<IBaseObjectCacheStrategy>();
            await cacheStrategy.SetAsync(cacheKey.ToUpper(), expectedValue, TimeSpan.FromSeconds(60));

            // Act  
            var result = await MethodCache.GetMethodCacheAsync<TestClass>(cacheKey, () => Task.FromResult(new TestClass()), 60);

            // Assert  
            Assert.AreEqual(expectedValue, result);
        }

        [TestMethod]
        public async Task GetMethodCacheAsync_ShouldInvokeFuncAndCacheValue_WhenCacheDoesNotExist()
        {
            // Arrange  
            var cacheKey = "TestKey";
            var expectedValue = new TestClass { Property = "NewValue" };

            var cacheStrategy = base._serviceProvider.GetService<IBaseObjectCacheStrategy>();
            await cacheStrategy.RemoveFromCacheAsync(cacheKey.ToUpper());  // Ensure cache is empty  

            // Act  
            var result = await MethodCache.GetMethodCacheAsync<TestClass>(cacheKey, () => Task.FromResult(expectedValue), 60);

            // Assert  
            Assert.AreEqual(expectedValue, result);

            var cachedValue = await cacheStrategy.GetAsync<TestClass>(cacheKey.ToUpper());
            Assert.AreEqual(expectedValue, cachedValue);
        }

        [TestMethod]
        public async Task GetMethodCacheAsync_ShouldUseUpperCaseCacheKey()
        {
            // Arrange  
            var cacheKey = "testkey";
            var expectedValue = new TestClass { Property = "NewValue" };

            var cacheStrategy = base._serviceProvider.GetService<IBaseObjectCacheStrategy>();
            await cacheStrategy.RemoveFromCacheAsync(cacheKey.ToUpper());  // Ensure cache is empty  

            // Act  
            var result = await MethodCache.GetMethodCacheAsync<TestClass>(cacheKey, () => Task.FromResult(expectedValue), 60);

            // Assert  
            Assert.AreEqual(expectedValue, result);

            var cachedValue = await cacheStrategy.GetAsync<TestClass>(cacheKey.ToUpper());
            Assert.AreEqual(expectedValue, cachedValue);
        }

        [TestMethod]
        public async Task GetMethodCacheAsync_ShouldReturnNull_WhenCacheItemExpires()
        {
            // Arrange  
            var cacheKey = "TestKey";
            var expectedValue = new TestClass { Property = "NewValue" };
            var cacheStrategy = base._serviceProvider.GetService<IBaseObjectCacheStrategy>();

            // Settings with a short expiration time (for example, 1 second)  
            int timeoutSeconds = 1;
            await cacheStrategy.RemoveFromCacheAsync(cacheKey.ToUpper()); // Ensure cache is empty  

            // Act: Cache a value  
            var result = await MethodCache.GetMethodCacheAsync<TestClass>(
                cacheKey,
                () => Task.FromResult(expectedValue),
                timeoutSeconds
            );

            // Assert: Ensure that the value has been cached  
            Assert.AreEqual(expectedValue, result);

            // Wait time exceeded the expiration time  
            await Task.Delay((timeoutSeconds + 1) * 1000);

            // Act: Get the cache again  
            var expiredResult = await cacheStrategy.GetAsync<TestClass>(cacheKey.ToUpper());

            // Assert: Cache item should be null (expired)  
            Assert.IsNull(expiredResult);
        }


        // Helper class for testing  
        private class TestClass
        {
            public string Property { get; set; }
        }
    }

}
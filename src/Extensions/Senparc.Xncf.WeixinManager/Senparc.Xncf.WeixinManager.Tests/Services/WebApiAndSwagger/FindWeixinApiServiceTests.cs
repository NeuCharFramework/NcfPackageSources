using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.WeixinManager.Services.WebApiAndSwagger;
using Senparc.Xncf.WeixinManager.Tests;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Extensions;
using System.Linq;
using Senparc.NeuChar;
using Senparc.CO2NET.WebApi;

namespace Senparc.Xncf.WeixinManager.Services.WebApiAndSwagger.Tests
{
    [TestClass()]
    public class FindWeixinApiServiceTests : BaseTest
    {
        [TestMethod()]
        public void FindWeixinApiResultTest()
        {
            Init();

            var finWeixinApiService = base.ServiceProvider.GetService<FindApiService>();

            {
                Console.WriteLine("FindWeixinApiResultTest Test 1");

                var kw = "Token";
                PlatformType? platformType = null;
                bool? isAsync = null;

                var result = finWeixinApiService.FindWeixinApiResult(platformType.ToString(), isAsync, kw);
                Assert.AreEqual(platformType.ToString(), result.Category);
                Assert.AreEqual(isAsync, result.IsAsync);
                Assert.AreEqual(kw, result.Keyword);
                Assert.IsTrue(result.ApiItemList.Count() > 0);
                Assert.IsTrue(result.ApiItemList.Any(z => z.Summary.Contains("异步方法")));
                Assert.IsTrue(result.ApiItemList.Any(z => !z.FullMethodName.Contains("Senparc.Weixin.MP")));

                Console.WriteLine("结果数：" + result.ApiItemList.Count());
                Console.WriteLine(result.ToJson(true));
            }
            {
                Console.WriteLine("FindWeixinApiResultTest Test 2");

                var kw = "Token";
                PlatformType? platformType = null;
                bool? isAsync = false;//搜索同步方法

                var result = finWeixinApiService.FindWeixinApiResult(platformType.ToString(), isAsync, kw);

                Console.WriteLine("结果数：" + result.ApiItemList.Count());
                Console.WriteLine(result.ToJson(true));

                Assert.IsTrue(result.ApiItemList.All(z => !z.Summary.Contains("异步方法")));
                Assert.IsTrue(result.ApiItemList.Any(z => !z.FullMethodName.Contains("Senparc.Weixin.MP")));

            }

            {
                Console.WriteLine("FindWeixinApiResultTest Test 3");

                var kw = "Token";
                PlatformType? platformType = PlatformType.WeChat_OfficialAccount;//搜索 Senparc.Weixin.MP 程序集
                bool? isAsync = true;//搜索异步方法

                var result = finWeixinApiService.FindWeixinApiResult(platformType.ToString(), isAsync, kw);

                Console.WriteLine("结果数：" + result.ApiItemList.Count());
                Console.WriteLine(result.ToJson(true));

                Assert.IsTrue(result.ApiItemList.All(z => z.Summary.Contains("异步方法")));
                Assert.IsTrue(result.ApiItemList.All(z => z.FullMethodName.Contains("Senparc.Weixin.MP")));

            }

        }
    }
}
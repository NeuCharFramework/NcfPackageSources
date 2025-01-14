using Senparc.CO2NET.Extensions;
using Senparc.Xncf.SenMapic.Domain.SiteMap;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Senparc.Xncf.SenMapicTests.Domain.SenMapic
{
    [TestClass]
    public class SenMapicEngineTests : BaseSenMapicTest
    {

        [TestMethod]
        public void Build_WithValidUrl_ShouldReturnUrlData()
        {
            // Arrange
            var urls = new[] { "https://www.senparc.com/" };
            var engine = new SenMapicEngine(
                serviceProvider: _serviceProvider,
                urls: urls,
                maxThread: 2,
                maxBuildMinutesForSingleSite: 5,
                priority: "0.5",
                changefreq: "daily",
                maxDeep: 3,
                maxPageCount: 50
            );

            // Act
            var result = engine.Build();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Count > 0);
            Assert.IsInstanceOfType(result, typeof(Dictionary<string, UrlData>));
        }

        [TestMethod]
        public void Build_WithInvalidUrl_ShouldHandleError()
        {
            // Arrange
            var urls = new[] { "https://invalid-domain-123456789.com/" };
            var engine = new SenMapicEngine(
                serviceProvider: _serviceProvider,
                urls: urls,
                maxThread: 1,
                maxBuildMinutesForSingleSite: 1
            );


            var result = engine.Build();
            Console.WriteLine(result.ToJson(true));
            // Act & Assert
            Assert.IsTrue(result.First().Value.Html.IsNullOrEmpty());
        }

        [TestMethod]
        public void Build_WithMaxPageCountLimit_ShouldRespectLimit()
        {
            // Arrange
            var urls = new[] { "https://www.senparc.com/" };
            const int maxPageCount = 5;
            var engine = new SenMapicEngine(
                serviceProvider: _serviceProvider,
                urls: urls,
                maxThread: 2,
                maxBuildMinutesForSingleSite: 5,
                maxPageCount: maxPageCount
            );

            // Act
            var result = engine.Build();

            // Assert
            Assert.IsTrue(result.Count <= maxPageCount);
        }

        [TestMethod]
        public void Build_WithFilterOmitWords_ShouldFilterUrls()
        {
            // Arrange
            var urls = new[] { "https://www.senparc.com/" };
            var filterOmitWords = new List<string> { "教育背景", "盛派成功案列" };
            var engine = new SenMapicEngine(
                serviceProvider: _serviceProvider,
                urls: urls,
                maxThread: 2,
                maxBuildMinutesForSingleSite: 5,
                filterOmitWords: filterOmitWords
            );

            // Act
            var result = engine.Build();

            // Assert
            Assert.IsTrue(result.Values.All(url =>
                !url.Url.Contains("admin", StringComparison.OrdinalIgnoreCase) &&
                !url.Url.Contains("login", StringComparison.OrdinalIgnoreCase)));
        }
    }
}
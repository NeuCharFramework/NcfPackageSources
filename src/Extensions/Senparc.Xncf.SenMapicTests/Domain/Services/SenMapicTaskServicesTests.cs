using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.SenMapic.Domain.Services;
using Senparc.Xncf.SenMapic.Domain.SiteMap;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.SenMapic;

namespace Senparc.Xncf.SenMapicTests.Domain.Services
{
    [TestClass]
    public class SenMapicTaskServiceTests : BaseSenMapicTest
    {
        private SenMapicTaskService _senMapicTaskService;

        public SenMapicTaskServiceTests()
        { 
            _senMapicTaskService = _serviceProvider.GetRequiredService<SenMapicTaskService>();

        }



        [TestMethod]
        public async Task CreateTaskAsync_ValidParameters_CreatesAndSavesTask()
        {
            // Arrange
            string name = "TestTask";
            string startUrl = "https://doc.ncf.pub";
            int maxThread = 5;
            int maxBuildMinutes = 60;
            int maxDeep = 3;
            int maxPageCount = 20;

            // Act
            var result = await _senMapicTaskService.CreateTaskAsync(
                name, startUrl, maxThread, maxBuildMinutes, maxDeep, maxPageCount, 
                startImmediately: true);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(name, result.Name);
            Assert.AreEqual(startUrl, result.StartUrl);
            Assert.AreEqual(maxThread, result.MaxThread);
            Assert.AreEqual(maxBuildMinutes, result.MaxBuildMinutes);
            Assert.AreEqual(maxDeep, result.MaxDeep);
            Assert.AreEqual(maxPageCount, result.MaxPageCount);
        }

        [TestMethod]
        public async Task StartTaskAsync_ValidTask_StartsTaskAndEngine()
        {
            // Arrange
            var task = await _senMapicTaskService.CreateTaskAsync(
                "Test", "https://test.com", 5, 60, 3, 100,
                startImmediately: false);

            // Act
            await _senMapicTaskService.StartTaskAsync(task);

            // Assert
            Assert.AreEqual(SenMapicTaskStatus.Waiting, task.Status);
        }

        [TestMethod]
        public async Task CreateAndStartTask_CompleteProcess()
        {
            // Arrange
            string name = "CompleteTest";
            string startUrl = "https://test.com";

            // Act
            var task = await _senMapicTaskService.CreateTaskAsync(
                name, startUrl, 5, 60, 3, 10,
                startImmediately: true);
            await _senMapicTaskService.StartTaskAsync(task);

            // Assert
            Assert.IsNotNull(task);
            Assert.AreEqual(SenMapicTaskStatus.Running, task.Status);//正在进行中

            Assert.AreEqual(name, task.Name);
            Assert.AreEqual(startUrl, task.StartUrl);

            //TODO:可以等待完成
        }

        [TestMethod]
        public async Task CreateTaskAsync_WithRealUrl_CreatesAndSavesTask()
        {
            // Arrange
            string name = "NCF文档测试";
            string startUrl = "https://doc.ncf.pub";
            int maxThread = 5;
            int maxBuildMinutes = 60;
            int maxDeep = 3;
            int maxPageCount = 100;

            // Act
            var result = await _senMapicTaskService.CreateTaskAsync(
                name, startUrl, maxThread, maxBuildMinutes, maxDeep, maxPageCount,
                startImmediately: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(name, result.Name);
            Assert.AreEqual(startUrl, result.StartUrl);
            Assert.AreEqual(SenMapicTaskStatus.Running, result.Status); // 验证初始状态
        }

        [TestMethod]
        public async Task CreateTaskAsync_WithMultipleUrls_CreatesMultipleTasks()
        {
            // Arrange
            var urls = new[] { "https://doc.ncf.pub", "https://www.ncf.pub" };
            var tasks = new List<Task>();

            // Act
            foreach (var url in urls)
            {
                var task = _senMapicTaskService.CreateTaskAsync(
                    $"NCF测试-{url}", url, 5, 60, 3, 100,
                    startImmediately: false);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);

            // Assert
            Assert.AreEqual(2, tasks.Count);
            foreach (var task in tasks)
            {
                Assert.IsNotNull(task);
            }
        }

        [TestMethod]
        public async Task CreateTaskAsync_WithInvalidUrl_ThrowsException()
        {
            // Arrange
            string name = "Invalid URL Test";
            string invalidUrl = "invalid-url";

            // Act & Assert
            var result = _senMapicTaskService.CreateTaskAsync(name, invalidUrl, 5, 60, 3, 100,
                startImmediately: true);

            //TODO:验证存在错误信息的结果
        }
    }
}

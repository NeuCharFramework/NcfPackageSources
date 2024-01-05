using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Senparc.Xncf.PromptRange;
using Senparc.Xncf.PromptRange.Domain.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Tests
{
    [TestClass()]
    public class PromptItemTests
    {
        // 由于代码更改  目前已经废弃所测方法
        // [TestMethod()]
        // public void GenerateNewVersionTest()
        // {
        //     VersionInfo testVersion(DateTime date, int build)
        //     {
        //         int major = date.Year;
        //         int minor = date.Month;
        //         int patch = date.Day;
        //         var lastVersion = $"{major}.{minor}.{patch}.{build}";
        //
        //         var moqDto = new Mock<PromptItemDto>();
        //         moqDto.Setup(z => z.Version).Returns("");
        //
        //         var promptItem = new PromptItem(moqDto.Object);
        //         return promptItem.GenerateNewVersion(lastVersion);
        //     }
        //
        //     var today = SystemTime.Now.DateTime;
        //
        //     //测试当天时间
        //     var newVersion = testVersion(today, 3);
        //     Assert.AreEqual(today.Year, newVersion.Major);
        //     Assert.AreEqual(today.Month, newVersion.Minor);
        //     Assert.AreEqual(today.Day, newVersion.Patch);
        //     Assert.AreEqual(3 + 1, newVersion.Build);//版本号 +1
        //
        //     //测试过去时间
        //     newVersion = testVersion(today.AddDays(-1), 4);
        //     Assert.AreEqual(today.Year, newVersion.Major);
        //     Assert.AreEqual(today.Month, newVersion.Minor);
        //     Assert.AreEqual(today.Day, newVersion.Patch);
        //     Assert.AreEqual(1, newVersion.Build);//版本号从 1 开始
        // }

        [TestMethod()]
        public void GetVersionInfoTest()
        {
            var moqDto = new Mock<PromptItemDto>();
            moqDto.Setup(z => z.Version).Returns("");

            var lastVersion = "2023.10.1.2";
            var promptItem = new PromptItem(moqDto.Object);
            var versionInfo = promptItem.GetVersionInfo(lastVersion);
            Assert.AreEqual(2023, versionInfo.Major);
            Assert.AreEqual(10, versionInfo.Minor);
            Assert.AreEqual(1, versionInfo.Patch);
            Assert.AreEqual(2, versionInfo.Build);
        }

        [TestMethod()]
        public void UpdateVersionTest()
        {
            var moqDto = new Mock<PromptItemDto>(MockBehavior.Loose, new string[0]);
            moqDto.Setup(z => z.Version).Returns("2021.2.1.3");

            var promptItem = new PromptItem(moqDto.Object);
            promptItem.UpdateVersion();

            var today = SystemTime.Now.DateTime;
            var newVersion = promptItem.GetVersionInfo();
            var todayFirstVersion = new VersionInfo(today.Year, today.Month, today.Day, 1);

            Assert.IsTrue(todayFirstVersion.Equals(newVersion));
            Assert.AreEqual(todayFirstVersion, newVersion);
        }
    }
}
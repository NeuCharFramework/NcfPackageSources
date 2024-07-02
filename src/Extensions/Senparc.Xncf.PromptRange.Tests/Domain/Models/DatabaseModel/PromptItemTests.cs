using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
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

namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Tests
{
    [TestClass()]
    public class PromptItemTests
    {
        [TestMethod()]
        public void IsPromptVersionTest()
        {
            /* 判断是否为 Prompt 版本（从左到右，必须包含 -T 之前的部分，-Txxx 为可选，当，出现 -T 时，-A 为可选，但 -A 不能单独出现）。
 * 可能的格式为：
 * 2023.12.14.1-T1-A123，
 * 2023.12.14.2-T1.1-A123
 * 2023.12.14.3-T2.1-A123
 * 2023.12.14.1-T2.2-A123
 * 2023.12.14.1-T2.2.1-A123
 * ...（T 后面可以有多个小数点）
 * 2023.12.14.1
 * 2023.12.14.2-T1.1
 */

            //测试正常情况
            Assert.IsTrue(PromptItem.IsPromptVersion("2023.12.14.1-T1-A123"));
            Assert.IsTrue(PromptItem.IsPromptVersion("2023.12.14.2-T1.1-A123"));
            Assert.IsTrue(PromptItem.IsPromptVersion("2023.12.14.3-T2.1-A123"));
            Assert.IsTrue(PromptItem.IsPromptVersion("2023.12.14.1-T2.2-A123"));
            Assert.IsTrue(PromptItem.IsPromptVersion("2023.12.14.1-T2.2.1-A123"));
            Assert.IsTrue(PromptItem.IsPromptVersion("2023.12.14.1-T2.2.1.1-A123"));
            Assert.IsTrue(PromptItem.IsPromptVersion("2023.12.14.1"));
            Assert.IsTrue(PromptItem.IsPromptVersion("2023.12.14.1-T2.2.1.1"));

            //测试异常情况
            Assert.IsFalse(PromptItem.IsPromptVersion("2023.12.14.1-A123"));
            Assert.IsFalse(PromptItem.IsPromptVersion("2023.12.14.1-"));
            Assert.IsFalse(PromptItem.IsPromptVersion("2023.12.14.1-T"));
            Assert.IsFalse(PromptItem.IsPromptVersion("2023.12.14.1-T1-2"));
            Assert.IsFalse(PromptItem.IsPromptVersion("2023.12.14.1-T1-T2"));
            Assert.IsFalse(PromptItem.IsPromptVersion("2023.12.14.1-T1-A1.1"));
            Assert.IsFalse(PromptItem.IsPromptVersion("2023.12.14.1T"));
            Assert.IsFalse(PromptItem.IsPromptVersion("NickName"));

        }

        [TestMethod()]
        public void IsValidSegmentTest()
        {
            // 测试用例  
            Assert.IsTrue(PromptItem.IsValidSegment("2023.12.14.1-T1-A123", "2023.12.14.1-T1-A123")); // true  
            Assert.IsTrue(PromptItem.IsValidSegment("2023.12.14.1-T1-A123", "2023.12.14.1-T1")); // true  
            Assert.IsTrue(PromptItem.IsValidSegment("2023.12.14.1-T1.1-A123", "2023.12.14.1-T1")); // true  
            Assert.IsTrue(PromptItem.IsValidSegment("2023.12.14.2-T2.1.1-A12", "2023.12.14.2-T2.1")); // true  
            Assert.IsTrue(PromptItem.IsValidSegment("2023.12.14.2-T2.1.1-A12", "2023.12.14.2-T2.1.1")); // true  
            Assert.IsFalse(PromptItem.IsValidSegment("2023.12.14.2-T2.1.1-A12", "2023.12.14.2-T2.1.1.1")); // false  
            Assert.IsFalse(PromptItem.IsValidSegment("2023.12.14.2-T2.11.1-A12", "2023.12.14.2-T2.1")); // false  
        }
    }
}

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
//
// [TestMethod()]
// public void GetVersionInfoTest()
// {
//     var moqDto = new Mock<PromptItemDto>();
//     moqDto.Setup(z => z.Version).Returns("");
//
//     var lastVersion = "2023.10.1.2";
//     var promptItem = new PromptItem(moqDto.Object);
//     var versionInfo = promptItem.GetVersionInfo(lastVersion);
//     Assert.AreEqual(2023, versionInfo.Major);
//     Assert.AreEqual(10, versionInfo.Minor);
//     Assert.AreEqual(1, versionInfo.Patch);
//     Assert.AreEqual(2, versionInfo.Build);
// }

// [TestMethod()]
// public void UpdateVersionTest()
// {
//     var moqDto = new Mock<PromptItemDto>(MockBehavior.Loose, new string[0]);
//     moqDto.Setup(z => z.Version).Returns("2021.2.1.3");
//
//     var promptItem = new PromptItem(moqDto.Object);
//     promptItem.UpdateVersion();
//
//     var today = SystemTime.Now.DateTime;
//     var newVersion = promptItem.GetVersionInfo();
//     var todayFirstVersion = new VersionInfo(today.Year, today.Month, today.Day, 1);
//
//     Assert.IsTrue(todayFirstVersion.Equals(newVersion));
//     Assert.AreEqual(todayFirstVersion, newVersion);
// }
//}
//}
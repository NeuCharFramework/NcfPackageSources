using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.XncfBase.VersionManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.VersionManager.Tests
{
    //[TestClass]
    //public class VersionHelperTests
    //{
    //    private const string VersionRegex = @"^(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?(?:-([a-zA-Z\d\-]+))?(?:\+([a-zA-Z\d\-]+))?$";

    //    [TestMethod]
    //    public void TestVersionRegex()
    //    {
    //        var regex = new Regex(VersionRegex);

    //        // 测试基本语义化版本号  
    //        Assert.IsTrue(regex.IsMatch("1.0.0"));
    //        Assert.IsTrue(regex.IsMatch("2.3.4"));
    //        Assert.IsTrue(regex.IsMatch("99.99.99"));

    //        // 测试带有构建号的版本号  
    //        Assert.IsTrue(regex.IsMatch("1.0.0.1"));
    //        Assert.IsTrue(regex.IsMatch("2.3.4.123"));

    //        // 测试带有预发布版本标签的版本号  
    //        Assert.IsTrue(regex.IsMatch("1.0.0-alpha"));
    //        Assert.IsTrue(regex.IsMatch("1.0.0-beta.2"));
    //        Assert.IsTrue(regex.IsMatch("1.0.0-rc-1"));

    //        // 测试带有元数据标签的版本号  
    //        Assert.IsTrue(regex.IsMatch("1.0.0+build.123"));
    //        Assert.IsTrue(regex.IsMatch("1.0.0-alpha+build.123"));
    //        Assert.IsTrue(regex.IsMatch("1.0.0-rc-1+build.123"));

    //        // 测试无效的版本号  
    //        Assert.IsFalse(regex.IsMatch("1"));
    //        Assert.IsFalse(regex.IsMatch("1.0"));
    //        Assert.IsFalse(regex.IsMatch("1.0.0.0.0"));
    //        Assert.IsFalse(regex.IsMatch("1.0.0-alpha+build."));
    //        Assert.IsFalse(regex.IsMatch("1.0.0-alpha+build!123"));
    //        Assert.IsFalse(regex.IsMatch("a.b.c"));
    //    }
    //}

    /// <summary>  
    /// VersionInfoParser.Parse 方法的单元测试类。  
    /// </summary>  
    [TestClass]
    public class VersionInfoParserTests
    {

        [TestMethod]
        public void TestParseValidVersionStrings()
        {
            // 测试基本语义化版本号  
            ValidateParse("1.0.0", 1, 0, 0, null, null, null);
            ValidateParse("2.3.4", 2, 3, 4, null, null, null);
            ValidateParse("99.99.99", 99, 99, 99, null, null, null);

            // 测试带有四个数字的版本号  
            ValidateParse("99.99.99.888", 99, 99, 99, 888, null, null);

            // 测试带有预发布版本标签的版本号  
            ValidateParse("1.0.0-alpha", 1, 0, 0, null, "alpha", null);
            ValidateParse("1.0.0-beta.2", 1, 0, 0, null, "beta.2", null);
            ValidateParse("1.0.0-rc-1", 1, 0, 0, null, "rc-1", null);

            // 测试带有元数据标签的版本号  
            ValidateParse("1.0.0+build.123", 1, 0, 0, null, null, "build.123");
            ValidateParse("1.0.0-alpha+build.123", 1, 0, 0, null, "alpha", "build.123");
            ValidateParse("1.0.0-rc-1+build.123", 1, 0, 0, null, "rc-1", "build.123");
        }

        [TestMethod]
        public void TestParseInvalidVersionStrings()
        {
            // 测试无效的版本号  
            Assert.ThrowsException<ArgumentException>(() => VersionHelper.Parse("1"));
            Assert.ThrowsException<ArgumentException>(() => VersionHelper.Parse("1.0"));
            Assert.ThrowsException<ArgumentException>(() => VersionHelper.Parse("1.0.0.0.0"));
            //Assert.ThrowsException<ArgumentException>(() => VersionHelper.Parse("1.0.0-alpha+build."));
            Assert.ThrowsException<ArgumentException>(() => VersionHelper.Parse("1.0.0-alpha+build!123"));
            //Assert.ThrowsException<ArgumentException>(() => VersionHelper.Parse("a.b.c"));

            var versionInfo = VersionHelper.Parse("1.0.0-alpha+build.");
            Console.WriteLine("Test: 1.0.0-alpha+build.  :" + versionInfo.ToString());
            Console.WriteLine();

        }

        private void ValidateParse(string versionString, int major, int minor, int patch, int? build, string preRelease, string metadata)
        {
            Console.WriteLine("versionString: " + versionString);
            var versionInfo = VersionHelper.Parse(versionString);

            Assert.AreEqual(major, versionInfo.Major);
            Assert.AreEqual(minor, versionInfo.Minor);
            Assert.AreEqual(patch, versionInfo.Patch);
            Assert.AreEqual(build, versionInfo.Build);
            Assert.AreEqual(preRelease, versionInfo.PreRelease);
            Assert.AreEqual(metadata, versionInfo.Metadata);
        }
    }
}


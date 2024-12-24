using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace Senparc.Xncf.ChangeNamespace.Tests
{
    [TestClass]
    public class ChangeNameSpaceTest
    {
        //[TestMethod]
        //public void RunTest()
        //{
        //    var serviceProvider = new ServiceCollection().BuildServiceProvider();
        //    var function = new Functions.ChangeNamespace(serviceProvider);

        //    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\", @"App_Data\src");
        //    var newNameSpace = "This.Is.NewNamespace.";

        //    //TODO:测试反向输入

        //    var result = function.Run(new ChangeNamespace_Parameters()
        //    {
        //        Path = path,
        //        NewNamespace = newNameSpace
        //    });

        //    Assert.AreEqual(function.FunctionParameterType, typeof(ChangeNamespace_Parameters));

        //    System.Console.WriteLine(result);
        //}

        //[TestMethod]
        //public void RunRestoreTest()
        //{
        //    var serviceProvider = new ServiceCollection().BuildServiceProvider();
        //    var function = new Functions.RestoreNameSpace(serviceProvider);

        //    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\", @"App_Data\src");
        //    var myNameSpace = "This.Is.NewNamespace.";

        //    //TODO:测试反向输入

        //    var result = function.Run(new RestoreNameSpace_Parameters()
        //    {
        //        Path = path,
        //        MyNamespace = myNameSpace
        //    });

        //    Assert.AreEqual(function.FunctionParameterType, typeof(RestoreNameSpace_Parameters));

        //    System.Console.WriteLine(result);
        //}
    }
}

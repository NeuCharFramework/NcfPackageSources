using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.CO2NET.Extensions;
using SeSenparc.Xncf.WeixinManager.Tests;

namespace Senparc.Xncf.WeixinManager.Tests.MessageHandlers
{
  
    class MyType : Dictionary<string, string>
    {

    } 
    
    [TestClass]
    public class GenericTest
    {
        [TestMethod]
        public void MyTestMethod()
        {
            List<MyType> s = new List<MyType> { new MyType() { { "MyKey", "MyValue" } } };

            // 使用 OfType 和 ToList 方法将 List<MyType> 转换为 List<Dictionary<string, string>>  
            List<Dictionary<string, string>> t = s.OfType<Dictionary<string, string>>().ToList();

            // 测试输出  
            Console.WriteLine($"t[0]: {t[0].ToJson(true)}");
        }

        [TestMethod]
        public void MessageContextTypeTest()
        {
            //var messageHandler = new NcfMesssageHandler(null, null, null, 0, null);
            var messageHandlerType = typeof(NcfMesssageHandler);

            var genericType = messageHandlerType.BaseType.GetGenericArguments().First();

            Console.WriteLine(genericType.FullName);


        }
    }

}

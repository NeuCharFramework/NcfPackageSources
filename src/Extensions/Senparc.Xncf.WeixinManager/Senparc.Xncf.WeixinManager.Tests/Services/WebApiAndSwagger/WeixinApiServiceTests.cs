using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.XncfBase;
using Senparc.Xncf.WeixinManager.Services;
using Senparc.Xncf.WeixinManager.Tests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Senparc.Xncf.WeixinManager.Services.Tests
{
    [TestClass()]
    public class WeixinApiServiceTests : BaseTest
    {
        public WeixinApiServiceTests() : base()
        {

        }

        [TestMethod()]
        public void GetWeixinApiAssemblyTest()
        {
            //Console.WriteLine("Ready for Init()");
            //Init();
            //Console.WriteLine("Finish Init()");

            //Init();

            //程序执行会自动触发
            var weixinApis = Senparc.CO2NET.ApiBind.ApiBindInfoCollection.Instance.GetGroupedCollection();
            var webApiEngine = new WebApiEngine(null);

            Console.WriteLine( $"weixinApis item count: {weixinApis.Count()}");

            var apiGroup = weixinApis.FirstOrDefault(z => z.Key == NeuChar.PlatformType.WeChat_OfficialAccount.ToString());

            if (apiGroup == null)
            {
                throw new Exception("找不到 Platform");
            }

            var apiCount = webApiEngine.BuildWebApi(apiGroup).Result;
            var weixinApiAssembly = webApiEngine.GetApiAssembly(apiGroup.Key);
            Console.WriteLine("API Count:" + apiCount);
            Console.WriteLine("Test Platform Assembly:" + weixinApiAssembly.FullName);
        }


        [TestMethod()]
        public void GetDocMethodNameTest()
        {
            var webApiEngine = new WebApiEngine(null);

            {
                var input = new XAttribute("name", "M:Senparc.Weixin.MP.AdvancedAPIs.AnalysisApi.GetArticleSummary(System.String,System.String,System.String,System.Int32)");
                var result = webApiEngine.GetDocMethodInfo(input);
                Assert.AreEqual("Senparc.Weixin.MP.AdvancedAPIs.AnalysisApi.GetArticleSummary", result.MethodName);
                Assert.AreEqual("(System.String,System.String,System.String,System.Int32)", result.ParamsPart);
            }
            {
                var input = new XAttribute("name", "T:Senparc.Weixin.MP.AdvancedAPIs.AnalysisApi");
                var result = webApiEngine.GetDocMethodInfo(input);
                Assert.AreEqual(null, result.MethodName);
                Assert.AreEqual(null, result.ParamsPart);
            }
            {
                var input  = new XAttribute("name", "P:Senparc.Weixin.MP.AdvancedAPIs.ShakeAround.QueryLottery_Result.drawed_value");
                var result = webApiEngine.GetDocMethodInfo(input);
                Assert.AreEqual(null, result.MethodName);
                Assert.AreEqual(null, result.ParamsPart);
            }
            {
                var input  = new XAttribute("name", "F:Senparc.Weixin.MP.MemberCard_CustomField_NameType.FIELD_NAME_TYPE_UNKNOW");
                var result = webApiEngine.GetDocMethodInfo(input);
                Assert.AreEqual(null, result.MethodName);
                Assert.AreEqual(null, result.ParamsPart);
            }

            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine();
                {
                    var dt1 = SystemTime.Now;
                    var input = new XAttribute("name", "M:Senparc.Weixin.MP.AdvancedAPIs.TemplateApi.SendTemplateMessage(System.String,System.String,Senparc.Weixin.Entities.TemplateMessage.ITemplateMessageBase,Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage.TempleteModel_MiniProgram,System.Int32)");
                    var result = webApiEngine.GetDocMethodInfo(input);
                    Console.WriteLine("Method 1 Cost:" + SystemTime.DiffTotalMS(dt1) + "ms");

                    Assert.AreEqual("Senparc.Weixin.MP.AdvancedAPIs.TemplateApi.SendTemplateMessage", result.MethodName);
                    Assert.AreEqual("(System.String,System.String,Senparc.Weixin.Entities.TemplateMessage.ITemplateMessageBase,Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage.TempleteModel_MiniProgram,System.Int32)", result.ParamsPart);
                }
            }

        }
    }

}
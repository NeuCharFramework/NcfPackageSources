using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Tests;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Functions.Tests
{
    public class TestFunctionAppService : AppServiceBase
    {
        public TestFunctionAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public class SetConfigFunctionAppRequest : FunctionAppRequestBase
        {
            [Required]
            [MaxLength(300)]
            [System.ComponentModel.Description("自动备份周期（分钟）||0 则为不自动备份")]
            public int BackupCycleMinutes { get; set; }
            [Required]
            [MaxLength(300)]
            [System.ComponentModel.Description("备份路径||本地物理路径，如：E:\\Senparc\\Ncf\\NCF.bak")]
            public string BackupPath { get; set; }

            public override async Task LoadData(IServiceProvider serviceProvider)
            {
                BackupCycleMinutes = 999;
                BackupPath = "Test BackupCycleMinutes";

            }
        }

                    public class SetSelectionConfigFunctionAppRequest : FunctionAppRequestBase
                    {
                        [Required]
                        [System.ComponentModel.Description("智能体||从下拉中选择，或直接手动输入")]
                        [FunctionParameterUi(ParameterType.DropDownList, nameof(AgentOptions), Filterable = true, AllowCreate = true)]
                        public string AgentName { get; set; }

                        [JsonIgnore]
                        public SelectionList AgentOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new List<SelectionItem>());

                        public override Task LoadData(IServiceProvider serviceProvider)
                        {
                            AgentOptions.Items.Add(new SelectionItem("PromptCatalyzer", "PromptCatalyzer", defaultSelected: true));
                            AgentOptions.Items.Add(new SelectionItem("Scorer", "Scorer"));
                            return Task.CompletedTask;
                        }
                    }

        public class SetBooleanSelectionConfigFunctionAppRequest : FunctionAppRequestBase
        {
            [System.ComponentModel.Description("输出详细日志||测试兼容 1/0 的布尔参数绑定")]
            [FunctionParameterUi(ParameterType.CheckBoxList, nameof(OutputVerboseOptions))]
            public bool OutputVerbose { get; set; }

            [JsonIgnore]
            public SelectionList OutputVerboseOptions { get; set; } = new SelectionList(SelectionType.CheckBoxList, new[]
            {
                new SelectionItem("1", "使用", "", false)
            });
        }

        [FunctionRender("设置参数", "设置备份间隔时间、备份文件路径等参数", typeof(TestModuleRegister))]
        public async Task<StringAppResponse> SetConfig(SetConfigFunctionAppRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                return request.BackupPath;
            });
        }

        [FunctionRender("设置智能体", "测试简单值 + 下拉元数据", typeof(TestModuleRegister))]
        public async Task<StringAppResponse> SetSelectionConfig(SetSelectionConfigFunctionAppRequest request)
        {
            return await this.GetStringResponseAsync(async (response, logger) =>
            {
                return request.AgentName;
            });
        }
    }


    [TestClass()]
    public class FunctionHelperTests : BaseXncfBaseTest
    {
        [TestMethod()]
        public void GetFunctionParameterInfoAsyncTest()
        {
            Console.WriteLine("FunctionRenderCollection: " + Senparc.Ncf.XncfBase.Register.FunctionRenderCollection.Keys.Select(z => z.FullName).ToJson(true));

            var registerList = XncfRegisterManager.RegisterList;
            Console.WriteLine("Register List:" + registerList.Select(z => z.Name).ToJson(true));

            var functionBag = Senparc.Ncf.XncfBase.Register.FunctionRenderCollection[typeof(TestModuleRegister)].Values.First();
            var result = FunctionHelper.GetFunctionParameterInfoAsync(base._serviceProvider, functionBag, true).GetAwaiter().GetResult();

            Assert.IsTrue(result.Count > 0);
            Console.WriteLine(result.ToJson(true));
        }

        [TestMethod()]
        public void GetFunctionParameterInfoWithUiAttributeTest()
        {
            var functionBag = Senparc.Ncf.XncfBase.Register.FunctionRenderCollection[typeof(TestModuleRegister)].Values
                .First(z => z.MethodInfo.Name == nameof(TestFunctionAppService.SetSelectionConfig));

            var result = FunctionHelper.GetFunctionParameterInfoAsync(base._serviceProvider, functionBag, true).GetAwaiter().GetResult();
            var agentName = result.First(z => z.Name == nameof(TestFunctionAppService.SetSelectionConfigFunctionAppRequest.AgentName));

            Assert.AreEqual(ParameterType.DropDownList, agentName.ParameterType);
            Assert.AreEqual("String", agentName.SystemType);
            Assert.AreEqual(2, agentName.SelectionList.Items.Count);
            Assert.IsTrue(agentName.Filterable);
            Assert.IsTrue(agentName.AllowCreate);
        }

        [TestMethod()]
        public void NormalizeLegacySelectionListJsonToStringRequestTest()
        {
            var rawJson = "{\"AgentName\":{\"SelectedValues\":[\"PromptCatalyzer\"]}}";
            var normalizedJson = FunctionRequestParameterNormalizer.NormalizeJson(rawJson, typeof(TestFunctionAppService.SetSelectionConfigFunctionAppRequest));

            var result = Senparc.CO2NET.Helpers.SerializerHelper.GetObject(normalizedJson, typeof(TestFunctionAppService.SetSelectionConfigFunctionAppRequest)) as TestFunctionAppService.SetSelectionConfigFunctionAppRequest;

            Assert.AreEqual("PromptCatalyzer", result.AgentName);
        }

        [TestMethod]
        public void NormalizeLegacySelectionListJsonToBooleanRequestTest()
        {
            var testCases = new Dictionary<string, bool>
            {
                ["{\"OutputVerbose\":\"1\"}"] = true,
                ["{\"OutputVerbose\":1}"] = true,
                ["{\"OutputVerbose\":[\"1\"]}"] = true,
                ["{\"OutputVerbose\":{\"SelectedValues\":[\"1\"]}}"] = true,
                ["{\"OutputVerbose\":\"0\"}"] = false,
                ["{\"OutputVerbose\":0}"] = false,
                ["{\"OutputVerbose\":[]}"] = false,
                ["{\"OutputVerbose\":{\"SelectedValues\":[]}}"] = false
            };

            foreach (var testCase in testCases)
            {
                var normalizedJson = FunctionRequestParameterNormalizer.NormalizeJson(testCase.Key, typeof(TestFunctionAppService.SetBooleanSelectionConfigFunctionAppRequest));
                var result = Senparc.CO2NET.Helpers.SerializerHelper.GetObject(normalizedJson, typeof(TestFunctionAppService.SetBooleanSelectionConfigFunctionAppRequest)) as TestFunctionAppService.SetBooleanSelectionConfigFunctionAppRequest;

                Assert.IsNotNull(result);
                Assert.AreEqual(testCase.Value, result.OutputVerbose, $"Failed payload: {testCase.Key}");
            }
        }
    }
}

using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.UnitTestExtension;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.PromptRange.Tests;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain.Services.Tests
{
    [TestClass()]
    public class PromptItemServiceTests : BaseNcfUnitTest
    {
        PromptItemService _promptItemService;
        public PromptItemServiceTests() : base(null, SeedDataGenerator.InitPromptItem)
        {
            var mockPromptItemRepo = base.GetRespository<PromptItem>();

            var aiModelService = new AIModelService(base.GetRespositoryObject<AIModel>(), base._serviceProvider);

            var promptRangeService = new PromptRangeService(base.GetRespositoryObject<Models.DatabaseModel.PromptRange>(), base._serviceProvider);

            _promptItemService = new PromptItemService(mockPromptItemRepo.MockRepository.Object, base._serviceProvider,
                aiModelService, promptRangeService);
        }

        [TestMethod()]
        public async Task GetPromptRangeTreeTest()
        {
            //测试全部显示虚拟节点
            {
                var result = await this._promptItemService.GetPromptRangeTreeList(true, true);
                Assert.IsNotNull(result);

                Console.WriteLine("显示 PromptRange 节点 / 显示 Tratic 节点");
                Console.WriteLine(result.Select(z => z.Text).ToJson(true));

                Assert.IsTrue(result.Any(z => z.Text.Contains($"⊙")));
            }

            //测试全部不显示虚拟节点
            {
                var result = await this._promptItemService.GetPromptRangeTreeList(false, false);
                Assert.IsNotNull(result);

                Console.WriteLine("不显示 PromptRange 节点 / 不显示 Tratic 节点");
                Console.WriteLine(result.Select(z => z.Text).ToJson(true));

                Assert.IsTrue(!result.Any(z => z.Text.Contains($"⊙")));
                Assert.IsTrue(!result.Any(z => z.Text.Contains("▼")));
            }


            //测试只显示 Prompt Range 虚拟节点
            {
                var result = await this._promptItemService.GetPromptRangeTreeList(true, false);
                Assert.IsNotNull(result);

                Console.WriteLine("显示 PromptRange 节点 / 不显示 Tratic 节点");
                Console.WriteLine(result.Select(z => z.Text).ToJson(true));

                Assert.IsTrue(result.Any(z => z.Text.Contains($"⊙")));
                Assert.IsTrue(!result.Any(z => z.Text.Contains("▼")));
            }

            //测试只显示 Tactic 虚拟节点
            {
                var result = await this._promptItemService.GetPromptRangeTreeList(false, true);
                Assert.IsNotNull(result);

                Console.WriteLine("不显示 PromptRange 节点 / 显示 Tratic 节点");
                Console.WriteLine(result.Select(z => z.Text).ToJson(true));

                Assert.IsTrue(!result.Any(z => z.Text.Contains($"⊙")));
                Assert.IsTrue(result.Any(z => z.Text.Contains("▼")));
            }
        }
    }
}
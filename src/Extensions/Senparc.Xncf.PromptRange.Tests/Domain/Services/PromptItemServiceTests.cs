using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            var result = await this._promptItemService.GetPromptRangeTree();
            Assert.IsNotNull(result);
            Console.WriteLine(result);
        }
    }
}
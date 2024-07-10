using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Service;
using Senparc.Ncf.UnitTestExtension;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Service.Tests
{
    [TestClass()]
    public class ServiceDataBaseTests : BaseNcfUnitTest
    {
        [TestMethod]
        public async Task AfterSaveObjectTest()
        {
            string msg = null;

            //设置保存对象后的行为
            ServiceDataBase.AfterSaveObject = obj => msg = obj.GetType().Name;

            var promptRangeRepo = base.GetRespository<PromptRange>();
            var promptRangeService = new PromptRangeService(promptRangeRepo.MockRepository.Object, base._serviceProvider);

            var promptRange = new PromptRange("Name", "Alias");
            await promptRangeService.SaveObjectAsync(promptRange);
            Assert.AreEqual(promptRange.GetType().Name, msg);
        }

        [TestMethod()]
        public void ServiceDataBaseTest()
        {

        }

        public void CloseConnectionTest()
        {

        }
    }
}
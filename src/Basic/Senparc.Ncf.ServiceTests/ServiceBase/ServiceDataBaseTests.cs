//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Senparc.Ncf.Service;
//using Senparc.Ncf.UnitTestExtension;
//using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
//using Senparc.Xncf.PromptRange.Domain.Services;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Senparc.Ncf.Service.Tests
//{
//    [TestClass()]
//    public class ServiceDataBaseTests : BaseNcfUnitTest
//    {
//        /// <summary>
//        /// Test saving a single object
//        /// </summary>
//        /// <returns></returns>
//        [TestMethod]
//        public async Task AfterSaveObjectTest()
//        {
//            string msg = null;
//            string dataDb = null;
//            string senparcEntities = null;

//            //Set the behavior after saving the object
//            ServiceDataBase.AfterSaveObject = (dataBase, obj) =>
//            {
//                dataDb = dataBase.BaseDB.GetType().Name;
//                senparcEntities = dataBase.BaseDB.BaseDataContext.GetType().Name;
//                msg = obj.GetType().Name;

//                if (obj is PromptRange promptRange)
//                {
//                    msg += "+" + promptRange.RangeName;
//                }
//            };

//            var promptRangeRepo = base.GetRespository<PromptRange>();
//            var promptRangeService = new PromptRangeService(promptRangeRepo.MockRepository.Object, base._serviceProvider);

//            var promptRange = new PromptRange("Name", "Alias");
//            await promptRangeService.SaveObjectAsync(promptRange);

//            Assert.AreEqual("PromptRange+Name", msg);
//            Assert.AreEqual("NcfUnitTestDataDb", dataDb);
//            Assert.AreEqual("NcfUnitTestEntities", senparcEntities);
//        }

//        /// <summary>
//        /// Test batch saving objects
//        /// </summary>
//        /// <returns></returns>
//        [TestMethod]
//        public async Task AfterSaveObjectLListTest()
//        {
//            List<string> result = new List<string>();

//            //Set the behavior after saving the object
//            ServiceDataBase.AfterSaveObject = (dataBase, obj) =>
//            {
//                if (obj is PromptRange promptRange)
//                {
//                    result.Add(promptRange.RangeName);
//                }
//            };

//            var promptRangeRepo = base.GetRespository<PromptRange>();
//            var promptRangeService = new PromptRangeService(promptRangeRepo.MockRepository.Object, base._serviceProvider);

//            List<PromptRange> promptRanges = new();
//            for (int i = 0; i < 5; i++)
//            {
//                var promptRange = new PromptRange($"Name-{i}", $"Alias-{i}");
//                promptRanges.Add(promptRange);
//            }

//            await promptRangeService.SaveObjectListAsync(promptRanges);

//            for (int i = 0; i < 5; i++)
//            {
//                Assert.AreEqual($"Name-{i}", promptRanges[i].RangeName);
//                Assert.AreEqual($"Alias-{i}", promptRanges[i].Alias);
//            }
//        }

//        /// <summary>
//        /// Test save all changes
//        /// </summary>
//        /// <returns></returns>
//        [TestMethod]
//        public async Task AfterSaveChangesTest()
//        {
//            string dataDb = null;
//            string senparcEntities = null;

//            //Set the behavior after saving the object
//            ServiceDataBase.AfterSaveChanges = (dataBase) =>
//            {
//                dataDb = dataBase.BaseDB.GetType().Name;
//                senparcEntities = dataBase.BaseDB.BaseDataContext.GetType().Name;
//            };

//            var promptRangeRepo = base.GetRespository<PromptRange>();
//            var promptRangeService = new PromptRangeService(promptRangeRepo.MockRepository.Object, base._serviceProvider);

//            var promptRange = new PromptRange("Name", "Alias");
//            await promptRangeService.SaveChangesAsync();

//            Assert.AreEqual("NcfUnitTestDataDb", dataDb);
//            Assert.AreEqual("NcfUnitTestEntities", senparcEntities);
//        }

//        [TestMethod()]
//        public void ServiceDataBaseTest()
//        {

//        }

//        public void CloseConnectionTest()
//        {

//        }
//    }
//}
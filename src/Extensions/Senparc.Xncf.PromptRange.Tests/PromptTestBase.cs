//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Identity.Client;
//using Moq;
//using Senparc.AI.Interfaces;
//using Senparc.AI.Kernel;
//using Senparc.CO2NET.Extensions;
//using Senparc.Ncf.Core.Tests;
//using Senparc.Ncf.Repository;
//using Senparc.Ncf.UnitTestExtension.Entities;
//using Senparc.Xncf.AIKernel.Domain.Services;
//using Senparc.Xncf.AIKernel.Models;
//using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
//using Senparc.Xncf.PromptRange.Domain.Services;
//using System;
//using System.Linq.Expressions;

//namespace Senparc.Xncf.PromptRange.Tests
//{
//    internal class MockObjects
//    {
//        //public IRepositoryBase<PromptItem> PromptItemRepository { get; set; }
//    }

//    [TestClass]
//    public class PromptTestBase : TestBase
//    {
//        protected IServiceProvider _serviceProvder;

//        static Action<DataList> InitSeedData = dataList =>
//        {
//            SeedDataGenerator.InitPromptItem(dataList);//会同时初始化 PromptRange
//        };

//        internal MockObjects MockObjects { get; set; }

//        public PromptTestBase(Action<IServiceCollection> servicesRegister = null, Action<Dictionary<Type, List<object>>> initSeedData = null) : base(servicesRegister, initSeedData ?? InitSeedData)
//        {
//            base.registerService.UseSenparcAI();

//            base.ServiceCollection.AddScoped<PromptService>(z => this.GetPromptService());
//            base.ServiceCollection.AddScoped<PromptItemService>(z => this.GetPromptItemService());
//            base.ServiceCollection.AddScoped<PromptRangeService>(z => this.GetPromptRangeService());

//            base.ServiceCollection.AddScoped<IAiHandler, SemanticAiHandler>();

//            _serviceProvder = base.ServiceCollection.BuildServiceProvider();

//            //Mock Arrange
//            MockObjects = new MockObjects();
//        }

//        protected override void ActionInServiceCollection()
//        {
//            var senparcAiSetting = new Senparc.AI.Kernel.SenparcAiSetting();
//            base.Configuration.GetSection("SenparcAiSetting").Bind(senparcAiSetting);
//            base.ServiceCollection.AddSenparcAI(base.Configuration, senparcAiSetting);

//            base.ActionInServiceCollection();
//        }

//        [TestMethod]
//        public void SenparcAiSettingTest()
//        {
//            var senparcAiSetting = Senparc.AI.Config.SenparcAiSetting;
//            Assert.IsNotNull(senparcAiSetting);
//            Assert.AreEqual(true, senparcAiSetting.IsDebug);
//            Console.WriteLine(senparcAiSetting.ToJson(true));
//        }

//        #region 快速初始化 Service

//        public AIModelService GetAIModelService()
//        {
//            var service = new AIModelService(base.GetRespositoryObject<AIModel>(), base._serviceProvider);
//            return service;
//        }

//        public PromptItemService GetPromptItemService()
//        {
//            var aiModelService = this.GetAIModelService();
//            var promptRangeService = this.GetPromptRangeService();
//            var service = new PromptItemService(base.GetRespositoryObject<PromptItem>(), base._serviceProvider, aiModelService, promptRangeService);
//            return service;
//        }

//        public PromptRangeService GetPromptRangeService()
//        {
//            var service = new PromptRangeService(base.GetRespositoryObject<Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.PromptRange>(), base._serviceProvider);
//            return service;
//        }

//        public PromptService GetPromptService()
//        {
//            var service = new PromptService(this.GetPromptRangeService(), this.GetPromptItemService(), null);
//            return service;
//        }

//        #endregion
//    }
//}
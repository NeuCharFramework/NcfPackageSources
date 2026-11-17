/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: PromptServiceTests.cs

    File description: Related implementation.

    

    

    Created by: Senparc - 20231001

    

    Modified by: Senparc - 20260702

    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling



----------------------------------------------------------------*/

//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Microsoft.Extensions.DependencyInjection;
//using Senparc.AI.AgentKernel.Handlers;
//using Senparc.Xncf.PromptRange.Domain.Services;
//using Senparc.Xncf.PromptRange.Tests;
//using Moq;
//using Senparc.Ncf.Repository;
//using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
//using System.Linq.Expressions;

//namespace Senparc.Xncf.PromptRange.Domain.Services.Tests
//{
//    [TestClass()]
//    public class PromptServiceTests : PromptTestBase
//    {

//        protected PromptService _service;
//        //protected PromptItemService _PromptItemService;
//        //protected PromptRangeService _PromptRangeService;

//        public PromptServiceTests()
//        {
//            _service = base.GetPromptService();
//        }

//        #region XncfBuilderPrompt test

//        #region filetype Prompt

//        /// <summary>
//Legacy comment translated to English.
//        /// </summary>
//        /// <returns></returns>
//        [TestMethod()]
//        public async Task GenerateEntityClass_PromptGroupTest()
//        {
//            Assert.IsNotNull(_service);
//            var dt1 = SystemTime.Now;

//            //PromptItem
//Legacy comment translated to English.

//            var plugins = new Dictionary<string, List<string>>()
//            {
//                { "XncfBuilderPlugin",new() { "GenerateEntityClass" } }
//            };

//            var result = await _service.GetPromptResultAsync<string>(null, prompt, null, plugins);

//            Assert.IsNotNull(result);
//            await Console.Out.WriteLineAsync(result);
//            await Console.Out.WriteLineAsync();
//Legacy comment translated to English.
//        }


//        /// <summary>
//Legacy comment translated to English.
//        /// </summary>
//        /// <returns></returns>
//        [TestMethod()]
//        public async Task GenerateEntityClass_PromptItemTest()
//        {
//            Assert.IsNotNull(_service);
//            var dt1 = SystemTime.Now;

//            //PromptItem
//Legacy comment translated to English.

//            var plugins = new Dictionary<string, List<string>>()
//            {
//                { "XncfBuilderPlugin",new() { "GenerateEntityClass" } }
//            };

//            var result = await _service.GetPromptResultAsync<string>(null, prompt, null, plugins);


//            Assert.IsNotNull(result);
//            await Console.Out.WriteLineAsync(result);
//            await Console.Out.WriteLineAsync();
//Legacy comment translated to English.
//        }

//        #endregion


//        #region databasetype Prompt

//        [TestMethod()]
//        public async Task GetPromptResultAsyncTest()
//        {
//            var service = base.GetPromptItemService();
//            // Act  
//            var result = await service.ForUnitTest(1);
//            Assert.AreEqual(base.dataLists.GetList<PromptItem>().OrderBy(z=>z.Id).First(), result);
//        }

//        #endregion

//        #endregion
//    }
//}
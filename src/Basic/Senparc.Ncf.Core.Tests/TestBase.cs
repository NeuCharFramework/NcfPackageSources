using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Bson;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Ncf.UnitTestExtension;
using Senparc.Ncf.UnitTestExtension.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Tests
{
    [TestClass]
    public abstract class TestBase : BaseNcfUnitTest
    {
        public TestBase(Action<IServiceCollection> servicesRegister = null, UnitTestSeedDataBuilder seedDataBuilder = null) : base(servicesRegister, seedDataBuilder)
        {
   
        }
    }
}

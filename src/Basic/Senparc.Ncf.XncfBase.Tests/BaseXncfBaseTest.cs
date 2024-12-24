using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Tests;

namespace Senparc.Ncf.XncfBase.Tests
{
    public class BaseXncfBaseTest : TestBase
    {
        public BaseXncfBaseTest()
        {
            //base.ServiceCollection.StartNcfEngine(base.Configuration, base.Env, null);
        }

        protected override void RegisterServiceCollectionFinished(IServiceCollection services)
        {
            base.RegisterServiceCollectionFinished(services);
        }
    }
}

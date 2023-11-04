using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.XncfBase.VersionManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.VersionManager.Tests
{
    [TestClass()]
    public class VersionHelperTests
    {
        #region Prase() 方法测试

        [TestMethod]
        public void TestParseValidVersionStrings()
        {
            // 测试基本语义化版本号  
            ValidateParse("1.0.0", 1, 0, 0, null, null, null);
            ValidateParse("2.3.4", 2, 3, 4, null, null, null);
            ValidateParse("99.99.99", 99, 99, 99, null, null, null);

            // 测试带有四个数字的版本号  
            ValidateParse("99.99.99.888", 99, 99, 99, 888, null, null);

            // 测试带有预发布版本标签的版本号  
            ValidateParse("1.0.0-alpha", 1, 0, 0, null, "alpha", null);
            ValidateParse("1.0.0-beta.2", 1, 0, 0, null, "beta.2", null);
            ValidateParse("1.0.0-rc-1", 1, 0, 0, null, "rc-1", null);

            // 测试带有元数据标签的版本号  
            ValidateParse("1.0.0+build.123", 1, 0, 0, null, null, "build.123");
            ValidateParse("1.0.0-alpha+build.123", 1, 0, 0, null, "alpha", "build.123");
            ValidateParse("1.0.0-rc-1+build.123", 1, 0, 0, null, "rc-1", "build.123");
        }

        [TestMethod]
        public void TestParseInvalidVersionStrings()
        {
            // 测试无效的版本号  
            Assert.ThrowsException<ArgumentException>(() => VersionHelper.Parse("1"));
            Assert.ThrowsException<ArgumentException>(() => VersionHelper.Parse("1.0"));
            Assert.ThrowsException<ArgumentException>(() => VersionHelper.Parse("1.0.0.0.0"));
            //Assert.ThrowsException<ArgumentException>(() => VersionHelper.Parse("1.0.0-alpha+build."));
            Assert.ThrowsException<ArgumentException>(() => VersionHelper.Parse("1.0.0-alpha+build!123"));
            //Assert.ThrowsException<ArgumentException>(() => VersionHelper.Parse("a.b.c"));

            var versionInfo = VersionHelper.Parse("1.0.0-alpha+build.");
            Console.WriteLine("Test: 1.0.0-alpha+build.  :" + versionInfo.ToString());
            Console.WriteLine();

        }

        private void ValidateParse(string versionString, int major, int minor, int patch, int? build, string preRelease, string metadata)
        {
            Console.WriteLine("versionString: " + versionString);
            var versionInfo = VersionHelper.Parse(versionString);

            Assert.AreEqual(major, versionInfo.Major);
            Assert.AreEqual(minor, versionInfo.Minor);
            Assert.AreEqual(patch, versionInfo.Patch);
            Assert.AreEqual(build, versionInfo.Build);
            Assert.AreEqual(preRelease, versionInfo.PreRelease);
            Assert.AreEqual(metadata, versionInfo.Metadata);
        }

        #endregion

        [TestMethod()]
        public void ParseFromCodeTest()
        {
            string code = @"using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Senparc.Ncf.Database;
using Senparc.Xncf.XncfBuilder.Domain.Models.Services;
using Senparc.Ncf.XncfBase.Database;
using Microsoft.Extensions.Hosting;
using Senparc.Xncf.XncfBuilder.Domain.Services;
using Senparc.AI.Kernel;

namespace Senparc.Xncf.XncfBuilder
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => ""Senparc.Xncf.XncfBuilder"";

        public override string Uid => ""C2E1F87F-2DCE-4921-87CE-36923ED0D6EA"";//必须确保全局唯一，生成后必须固定

        public override string Version => ""0.10.1"";//必须填写版本号

        public override string MenuName => ""XNCF 模块生成器"";

        public override string Icon => ""fa fa-plus"";

        public override string Description => ""快速生成 XNCF 模块基础程序代码，或 Sample 演示，可基于基础代码扩展自己的应用"";

        //public override IList<Type> Functions => new Type[] {
        //    typeof(BuildXncf),
        //    typeof(AddMigration),
        //};

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //更新数据库
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            XncfBuilderEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as XncfBuilderEntities;
            var xncfDbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(this.GetType());

            //指定需要删除的数据实体
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(xncfDbContextType).Keys.ToArray();
            //删除数据库表
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            await base.UninstallAsync(serviceProvider, unsinstallFunc).ConfigureAwait(false);
        }

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            //services.AddScoped<PromptRange.Domain.Services.PromptService>();
            //services.AddScoped<AI.Interfaces.IAiHandler>(s => new SemanticAiHandler());

            services.AddScoped<ConfigService>();
            services.AddScoped<PromptBuilderService>();

            return base.AddXncfModule(services, configuration, env);
        }

        #endregion
    }
}
";
            string exceptedCode = @"using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Senparc.Ncf.Database;
using Senparc.Xncf.XncfBuilder.Domain.Models.Services;
using Senparc.Ncf.XncfBase.Database;
using Microsoft.Extensions.Hosting;
using Senparc.Xncf.XncfBuilder.Domain.Services;
using Senparc.AI.Kernel;

namespace Senparc.Xncf.XncfBuilder
{
    [XncfRegister]
    public partial class Register : XncfRegisterBase, IXncfRegister
    {
        #region IXncfRegister 接口

        public override string Name => ""Senparc.Xncf.XncfBuilder"";

        public override string Uid => ""C2E1F87F-2DCE-4921-87CE-36923ED0D6EA"";//必须确保全局唯一，生成后必须固定

        public override string Version => ""0.11.1"";//必须填写版本号

        public override string MenuName => ""XNCF 模块生成器"";

        public override string Icon => ""fa fa-plus"";

        public override string Description => ""快速生成 XNCF 模块基础程序代码，或 Sample 演示，可基于基础代码扩展自己的应用"";

        //public override IList<Type> Functions => new Type[] {
        //    typeof(BuildXncf),
        //    typeof(AddMigration),
        //};

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //更新数据库
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            XncfBuilderEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as XncfBuilderEntities;
            var xncfDbContextType = MultipleDatabasePool.Instance.GetXncfDbContextType(this.GetType());

            //指定需要删除的数据实体
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(xncfDbContextType).Keys.ToArray();
            //删除数据库表
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            await base.UninstallAsync(serviceProvider, unsinstallFunc).ConfigureAwait(false);
        }

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            //services.AddScoped<PromptRange.Domain.Services.PromptService>();
            //services.AddScoped<AI.Interfaces.IAiHandler>(s => new SemanticAiHandler());

            services.AddScoped<ConfigService>();
            services.AddScoped<PromptBuilderService>();

            return base.AddXncfModule(services, configuration, env);
        }

        #endregion
    }
}
";

            VersionInfo oldVersionInfo = VersionHelper.ParseFromCode(code);

            // 修改版本号  
            VersionInfo newVersionInfo = new VersionInfo
            {
                Major = oldVersionInfo.Major,
                Minor = oldVersionInfo.Minor + 1,
                Patch = oldVersionInfo.Patch,
                PreRelease = oldVersionInfo.PreRelease,
                Metadata = oldVersionInfo.Metadata
            };

            string replacedCode = VersionHelper.ReplaceVersionInCode(code, oldVersionInfo, newVersionInfo);
            Assert.AreEqual(exceptedCode, replacedCode);
            Console.WriteLine(replacedCode); // 输出：public override string Version => "0.6.1";  
        }
    }
}


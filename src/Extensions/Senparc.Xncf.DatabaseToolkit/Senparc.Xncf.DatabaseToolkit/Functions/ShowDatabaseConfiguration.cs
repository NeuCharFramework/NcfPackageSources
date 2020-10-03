using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.DatabaseToolkit.Functions
{
    /// <summary>
    /// 显示当前的数据库配置类型
    /// </summary>
    public class ShowDatabaseConfiguration : FunctionBase
    {
        public class Parameters : IFunctionParameter
        {

        }

        public ShowDatabaseConfiguration(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override string Name => "查看数据库配置类型";

        public override string Description => "查看实现 IDatabaseConfiguration 接口的数据库配置类型";

        public override Type FunctionParameterType => typeof(Parameters);

        public override FunctionResult Run(IFunctionParameter param)
        {
            return FunctionHelper.RunFunction<Parameters>(param, (typeParam, sb, result) =>
            {
                var databaseConfigurationFactory = DatabaseConfigurationFactory.Instance;
                var currentDatabaseConfiguration = DatabaseConfigurationFactory.Instance.CurrentDatabaseConfiguration;
                result.Message = $"当前 DatabaseConfiguration：{currentDatabaseConfiguration.GetType().Name}";
            });
        }
    }
}

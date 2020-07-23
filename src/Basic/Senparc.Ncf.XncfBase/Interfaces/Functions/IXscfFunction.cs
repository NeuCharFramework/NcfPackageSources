
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// 扩展方法接口
    /// </summary>
    public interface IXscfFunction
    {
        string Name { get; }
        string Description { get; }

        Type FunctionParameterType { get; }

        /// <summary>
        /// 生成参数定义对象的实例
        /// </summary>
        /// <returns></returns>
        IFunctionParameter GenerateParameterInstance();

        /// <summary>
        /// ServiceProvider 实例
        /// </summary>
        IServiceProvider ServiceProvider { get; set; }

        /// <summary>
        /// 执行程序
        /// </summary>
        /// <param name="param">参数</param>
        /// <returns></returns>
        FunctionResult Run(IFunctionParameter param);
    }
}

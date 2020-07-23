using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Functions
{
    /// <summary>
    /// 接口：需要默认载入数据的 FunctionParameter
    /// </summary>
    public interface IFunctionParameterLoadDataBase: IFunctionParameter
    {
        Task LoadData(IServiceProvider serviceProvider);
    }

    /// <summary>
    /// 需要默认载入数据的 FunctionParameter 
    /// </summary>
    public abstract class FunctionParameterLoadDataBase : IFunctionParameterLoadDataBase
    {
        /// <summary>
        /// 载入数据
        /// </summary>
        /// <returns></returns>
        public abstract Task LoadData(IServiceProvider serviceProvider);
    }
}

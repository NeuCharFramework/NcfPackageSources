/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：StartupHelper.cs
    文件功能描述：StartupHelper 相关实现
    
    
    创建标识：Senparc - 20240107
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;
using Senparc.Ncf.Core.Models;

namespace Senparc.Ncf.XncfBase.Helpers
{
    public class StartupHelper
    {
        /// <summary>
        /// 获取模块版本号
        /// </summary>
        /// <typeparam name="TXncfDatabaseRegister"></typeparam>
        /// <returns></returns>
        internal static string GetXncfVersion<TXncfDatabaseRegister>()
          where TXncfDatabaseRegister : class, IXncfDatabase, new()
        {
            try
            {
                var register = System.Activator.CreateInstance<TXncfDatabaseRegister>() as TXncfDatabaseRegister;
                if (register is IXncfRegister xncfRegister)
                {
                    return $" / XNCF {xncfRegister.Name} {xncfRegister.Version}";
                }
                return "Dev";
            }
            catch
            {
                return "Dev / Not IXncfRegister";
            }
        }
    }
}

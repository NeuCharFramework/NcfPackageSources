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

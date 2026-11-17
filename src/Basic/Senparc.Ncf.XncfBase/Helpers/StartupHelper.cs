/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: StartupHelper.cs

    File description: StartupHelper related implementation

    

    

    Created by: Senparc - 20240107

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
        ///Legacy comment translated to English.
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

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: NcfWebApiHelper.cs

    File description: NcfWebApiHelper related implementation

    

    

    Created by: Senparc - 20241119

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.WebApi
{
    public static class NcfWebApiHelper
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static string GetNcfApiClientPath(string xncfName, string appServiceName, string methodName, string showStaticApiState = null)
        {
            var globalName = ApiBindAttribute.GetGlobalName(xncfName, $"{appServiceName}.{methodName}");

            var indexOfApiGroupDot = globalName.IndexOf(".");
            var apiName = globalName.Substring(indexOfApiGroupDot + 1, globalName.Length - indexOfApiGroupDot - 1);

            var apiPath = WebApiEngine.GetApiPath(xncfName, appServiceName, apiName, showStaticApiState);
            return apiPath;
        }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="uniqueCode"></param>
        /// <returns></returns>
        public static string GetXncfProjectName(string name, string uniqueCode = null)
        {
            name = name.Replace(".", "-").Replace("_", "-");
            if (!uniqueCode.IsNullOrEmpty())
            {
                name += $"_{uniqueCode}";
            }
            return name;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="projectType"></param>
        /// <param name="uniqueCode"></param>
        /// <returns></returns>
        public static string GetXncfProjectName(Type projectType, string uniqueCode = null)
        {
            var name = projectType.Name;
            return GetXncfProjectName(name, uniqueCode);
        }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="uniqueCode"></param>
        /// <returns></returns>
        public static string GetXncfProjectName<T>(string uniqueCode = null)
        {
            var name = typeof(T).Name;
            return GetXncfProjectName(name, uniqueCode);
        }
    }
}

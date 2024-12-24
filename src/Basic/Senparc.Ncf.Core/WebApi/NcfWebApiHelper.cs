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
        /// 获取 XNCF 及 [ApiBind] 特性生成的 web API 地址
        /// </summary>
        /// <param name="xncfName">XNCF 程序集名称（通常和 XNCF 的名称相同）</param>
        /// <param name="appServiceName">AppService 名称或者 Controller 名称（[ApiBind] 中称之为 category）</param>
        /// <param name="methodName">方法名称或定义的接口名称（[ApiBind] 中称之为 name）</param>
        /// <param name="showStaticApiState">URL 中的静态后缀，一般情况下请留空</param>
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
        /// 获取 Aspire 使用的统一默认 XNCF 名称
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
        /// 获取 Aspire 使用的统一默认 XNCF 名称
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
        /// 获取 Aspire 使用的统一默认 XNCF 名称
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

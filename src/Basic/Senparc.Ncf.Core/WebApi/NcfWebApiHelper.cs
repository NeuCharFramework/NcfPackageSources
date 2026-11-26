/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfWebApiHelper.cs
    File description: NcfWebApiHelper related implementation

    Created by: Senparc - 20241119

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

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
        /// Builds the public client path for an XNCF application-service method.
        /// </summary>
        /// <param name="xncfName">The registered XNCF name.</param>
        /// <param name="appServiceName">The application-service name.</param>
        /// <param name="methodName">The method name.</param>
        /// <param name="showStaticApiState">Optional static API-state suffix.</param>
        /// <returns>The resolved client API path.</returns>
        public static string GetNcfApiClientPath(string xncfName, string appServiceName, string methodName, string showStaticApiState = null)
        {
            var globalName = ApiBindAttribute.GetGlobalName(xncfName, $"{appServiceName}.{methodName}");

            var indexOfApiGroupDot = globalName.IndexOf(".");
            var apiName = globalName.Substring(indexOfApiGroupDot + 1, globalName.Length - indexOfApiGroupDot - 1);

            var apiPath = WebApiEngine.GetApiPath(xncfName, appServiceName, apiName, showStaticApiState);
            return apiPath;
        }


        /// <summary>
        /// Converts a module name into a filesystem/project-safe XNCF project name.
        /// </summary>
        /// <param name="name">The logical project name.</param>
        /// <param name="uniqueCode">Optional uniqueness suffix.</param>
        /// <returns>The normalized project name.</returns>
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
        /// Converts a project type name into a filesystem/project-safe XNCF project name.
        /// </summary>
        /// <param name="projectType">The type whose name supplies the project name.</param>
        /// <param name="uniqueCode">Optional uniqueness suffix.</param>
        /// <returns>The normalized project name.</returns>
        public static string GetXncfProjectName(Type projectType, string uniqueCode = null)
        {
            var name = projectType.Name;
            return GetXncfProjectName(name, uniqueCode);
        }


        /// <summary>
        /// Converts a generic type name into a filesystem/project-safe XNCF project name.
        /// </summary>
        /// <typeparam name="T">The type whose name supplies the project name.</typeparam>
        /// <param name="uniqueCode">Optional uniqueness suffix.</param>
        /// <returns>The normalized project name.</returns>
        public static string GetXncfProjectName<T>(string uniqueCode = null)
        {
            var name = typeof(T).Name;
            return GetXncfProjectName(name, uniqueCode);
        }
    }
}

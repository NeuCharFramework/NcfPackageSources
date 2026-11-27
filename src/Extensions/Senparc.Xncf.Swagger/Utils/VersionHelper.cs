/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: VersionHelper.cs
    File description: VersionHelper related implementation

    Created by: Senparc - 20210614

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.12 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.20.12.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.AssembleScan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Senparc.Xncf.Swagger.Utils
{
    public class VersionHelper
    {
        /// <summary>
        /// Scans registered assemblies for MVC API version attributes and returns distinct
        /// major/minor version strings in ascending order.
        /// </summary>
        /// <returns>The discovered version labels.</returns>
        public static List<string> GetApiVersions()
        {
            var versions = new List<ApiVersion>();

            AssembleScanHelper.AddAssembleScanItem(assembly =>
            {
                //Legacy comment translated to English.
                try
                {
                    var allTypes = assembly.GetTypes();
                    foreach (var t in allTypes)
                    {
                        var classVersion = t.GetCustomAttributes(typeof(ApiVersionAttribute), true).OfType<ApiVersionAttribute>()
                            .SelectMany(attr => attr.Versions).Distinct();
                        versions.AddRange(classVersion);

                        var methods = t.GetMethods().Where(w => w.GetCustomAttributes(typeof(ApiVersionAttribute), true).Length > 0);
                        foreach (var method in methods)
                        {
                            var methodVersion = method.GetCustomAttributes(typeof(ApiVersionAttribute), true).OfType<ApiVersionAttribute>()
                                .SelectMany(attr => attr.Versions).Distinct();
                            versions.AddRange(methodVersion);
                        }
                    }
                    //var subTypes = allTypes.Where(w => w.GetCustomAttributes(typeof(ApiVersionAttribute), true).Length > 0);
                    //foreach (var t in subTypes)
                    //{
                    //    var methods = t.GetMethods().Where(w => w.GetCustomAttributes(typeof(ApiVersionAttribute), true).Length > 0);
                    //    foreach (var method in methods)
                    //    {
                    //        var methodVersion = method.GetCustomAttributes(typeof(ApiVersionAttribute), true).OfType<ApiVersionAttribute>()
                    //            .SelectMany(attr => attr.Versions).Distinct();
                    //        versions.AddRange(methodVersion);
                    //    }
                    //}
                }
                catch (Exception)
                {
                }

            },true);

            return versions.Distinct().Select(s => $"v{s.MajorVersion}.{s.MinorVersion}".Trim('.')).OrderBy(o => o).ToList();
        }
    }
}

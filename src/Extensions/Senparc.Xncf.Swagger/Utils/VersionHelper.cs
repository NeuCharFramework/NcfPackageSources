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
        /// Scan the entire application for methods that implement ApiVersion to obtain the used version
        /// Low efficiency. Does ApiVersion provide all used version interfaces? ? ?
        /// </summary>
        /// <returns></returns>
        public static List<string> GetApiVersions()
        {
            var versions = new List<ApiVersion>();

            AssembleScanHelper.AddAssembleScanItem(assembly =>
            {
                //ApiVersion on class
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
                    ////apiversion on method
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

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AssembleScanHelper.cs
    File description: AssembleScanHelper related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Senparc.Ncf.Core.AssembleScan
{
    public static class AssembleScanHelper
    {
        /// <summary>
        /// Gets the callbacks registered for assembly scanning.
        /// </summary>
        public static List<AssembleScanItem> ScanAssamblesActions { get; set; } = new List<AssembleScanItem>();

        private static object _scanLock = new object();

        /// <summary>
        /// Registers an assembly-scan callback and optionally executes all pending scans immediately.
        /// </summary>
        /// <param name="action">The callback invoked once for each selected assembly.</param>
        /// <param name="runScanNow">Whether to invoke <see cref="RunScan(string[])"/> immediately.</param>
        /// <param name="dllFilePatterns">Additional case-insensitive filename fragments accepted during dynamic loading.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is <see langword="null"/>.</exception>
        public static void AddAssembleScanItem(Action<Assembly> action, bool runScanNow, string[] dllFilePatterns = null)
        {
            ScanAssamblesActions.Add(new AssembleScanItem(action));

            if (runScanNow)
            {
                RunScan(dllFilePatterns);//SCAN NOW
            }
        }


        /// <summary>
        /// Executes all unfinished assembly-scan callbacks against the currently loaded assemblies
        /// and eligible extension DLLs.
        /// </summary>
        /// <param name="dllFilePatterns">Additional case-insensitive filename fragments accepted during dynamic loading.</param>
        public static void RunScan(string[] dllFilePatterns)
        {
            var dt1 = SystemTime.Now;

            lock (_scanLock)
            {
                //Find all extension caches B
                var scanTypesCount = 0;

                var assemblies = GetAssembiles(dynamicLoadAllDlls: true, dllFilePatterns: dllFilePatterns);
                var toScanItems = ScanAssamblesActions.Where(z => z.ScanFinished == false).ToList();

                //searchall Unreferenced Items

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        scanTypesCount++;
                        foreach (var scanItem in toScanItems)
                        {
                            scanItem.Run(assembly);//Execute the scanning process
                        }
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.SendCustomLog("ScanAssambles() 自动扫描程序集报告（非程序异常）：" + assembly.FullName, ex.ToString());
                    }
                }

                toScanItems.ForEach(z => z.Finished());//Mark end

                var dt2 = SystemTime.Now;
                SenparcTrace.SendCustomLog("ScanAssambles", $"RegisterAllAreas 用时：{(dt2 - dt1).TotalMilliseconds}ms");
            }


        }

        #region Get Assembly


        private static List<Assembly> AllAssemblies = null;
        private static object AllAssembliesLock = new object();

        /// <summary>
        /// Returns the application assemblies and, when requested, dynamically loads eligible extension DLLs.
        /// Results can be reused from the helper's process-wide assembly cache.
        /// </summary>
        /// <param name="dynamicLoadAllDlls">Whether to inspect the application base directory for additional DLLs.</param>
        /// <param name="useCachedData">Whether an existing cached assembly list may be returned.</param>
        /// <param name="forceUpdateCache">Whether to rebuild the cached list before returning.</param>
        /// <param name="dllFilePatterns">Additional case-insensitive filename fragments accepted during dynamic loading.</param>
        /// <returns>The assemblies available for scanning.</returns>
        public static List<Assembly> GetAssembiles(bool dynamicLoadAllDlls = true, bool useCachedData = true, bool forceUpdateCache = true, string[] dllFilePatterns = null)
        {
            lock (AllAssembliesLock)
            {
                if (!forceUpdateCache && useCachedData && AllAssemblies != null)
                {
                    return AllAssemblies;
                }

                var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();

                #region Legacy section

                if (dynamicLoadAllDlls)
                {
                    //Legacy comment translated to English.
                    string directoryPath = AppDomain.CurrentDomain.BaseDirectory;
                    //Legacy comment translated to English.
                    // string directoryPath = Environment.GetEnvironmentVariable("MY_APP_PATH");

                    //Legacy comment translated to English.
                    var loadedAssemblies = assemblies.Select(a => a.GetName().Name).ToList();

                    foreach (var filePath in Directory.GetFiles(directoryPath, "*.dll"))
                    {
                        try
                        {
                            var fileName = Path.GetFileName(filePath);

                            List<string> fileNamePatternList = new List<string>() { ".Xncf." };
                            if (dllFilePatterns != null)
                            {
                                fileNamePatternList.AddRange(dllFilePatterns);
                            }

                            if (fileNamePatternList.Any(z => fileName.Contains(z, StringComparison.OrdinalIgnoreCase)))
                            {
                                var assemblyName = Path.GetFileNameWithoutExtension(fileName);
                                if (!loadedAssemblies.Contains(assemblyName))
                                {
                                    Assembly assembly = Assembly.LoadFrom(filePath);
                                    assemblies.Add(assembly);
                                    Console.WriteLine($"Dynamic Loaded: {assembly.FullName}");
                                }
                                else
                                {
                                    //getcurrentfiledllversion
                                    // getfileversioninformation  
                                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(filePath);

                                    //Legacy comment translated to English.
                                    string version = fvi.FileVersion;

                                    Console.WriteLine($"Already loaded: {assemblyName} ({version})");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error loading assembly from {filePath}: {ex.Message}");
                        }
                    }
                }


                #endregion

                //Update cache
                if (forceUpdateCache
                    || AllAssemblies == null
                    || assemblies.Count() > AllAssemblies.Count())
                {
                    AllAssemblies = assemblies;
                }

                if (SiteConfig.NcfCoreState.DllFilePatterns == null)
                {
                    SiteConfig.NcfCoreState.DllFilePatterns = dllFilePatterns?.ToList();
                }

                return assemblies;
            }
        }

        #endregion
    }
}

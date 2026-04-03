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
        /// Collection of all scan methods
        /// </summary>
        public static List<AssembleScanItem> ScanAssamblesActions { get; set; } = new List<AssembleScanItem>();

        private static object _scanLock = new object();

        /// <summary>
        /// Add scan item
        /// </summary>
        /// <param name="action">Scan process</param>
        /// <param name="runScanNow">Whether to scan immediately</param>
        /// <param name="dllFilePatterns">Included dll file names; ".Xncf." is always included</param>
        public static void AddAssembleScanItem(Action<Assembly> action, bool runScanNow, string[] dllFilePatterns = null)
        {
            ScanAssamblesActions.Add(new AssembleScanItem(action));

            if (runScanNow)
            {
                RunScan(dllFilePatterns);//Scan immediately
            }
        }


        /// <summary>
        /// Execute scan
        /// </summary>
        /// <param name="dllFilePatterns">Included dll file names; ".Xncf." is always included</param>
        public static void RunScan(string[] dllFilePatterns)
        {
            var dt1 = SystemTime.Now;

            lock (_scanLock)
            {
                //Find all extension cache B
                var scanTypesCount = 0;

                var assemblies = GetAssembiles(dynamicLoadAllDlls: true, dllFilePatterns: dllFilePatterns);
                var toScanItems = ScanAssamblesActions.Where(z => z.ScanFinished == false).ToList();

                //Search all unreferenced items

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        scanTypesCount++;
                        foreach (var scanItem in toScanItems)
                        {
                            scanItem.Run(assembly);//Execute scan process
                        }
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.SendCustomLog("ScanAssambles() auto assembly scan report (non-program exception): " + assembly.FullName, ex.ToString());
                    }
                }

                toScanItems.ForEach(z => z.Finished());//Mark as finished

                var dt2 = SystemTime.Now;
                SenparcTrace.SendCustomLog("ScanAssambles", $"RegisterAllAreas elapsed: {(dt2 - dt1).TotalMilliseconds}ms");
            }


        }

        #region 获取程序集


        private static List<Assembly> AllAssemblies = null;
        private static object AllAssembliesLock = new object();

        /// <summary>
        /// Get assemblies
        /// </summary>
        /// <param name="dynamicLoadAllDlls">Whether to load unreferenced assemblies from dll directory, default true</param>
        /// <param name="useCachedData">Whether to use cached data, default true</param>
        /// <param name="forceUpdateCache">Force refresh cache; ignores <paramref name="useCachedData"/> when true</param>
        /// <param name="dllFilePatterns">Included dll file names; ".Xncf." is always included</param>
        public static List<Assembly> GetAssembiles(bool dynamicLoadAllDlls = true, bool useCachedData = true, bool forceUpdateCache = true, string[] dllFilePatterns = null)
        {
            lock (AllAssembliesLock)
            {
                if (!forceUpdateCache && useCachedData && AllAssemblies != null)
                {
                    return AllAssemblies;
                }

                var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();

                #region Complete unreferenced assemblies

                if (dynamicLoadAllDlls)
                {
                    // Use AppDomain or environment variables to dynamically obtain paths
                    string directoryPath = AppDomain.CurrentDomain.BaseDirectory;
                    // If needed, environment variables can also be used
                    // string directoryPath = Environment.GetEnvironmentVariable("MY_APP_PATH");

                    // The remaining steps are the same as before: iterate and try loading DLLs
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
                                    //Get current DLL version for this file
                                    // Get file version info
                                    FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(filePath);

                                    // Get version number
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

                // Update cache
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

using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Senparc.Ncf.Core.AssembleScan
{
    public static class AssembleScanHelper
    {
        /// <summary>
        /// 所有扫描方法的集合
        /// </summary>
        public static List<AssembleScanItem> ScanAssamblesActions { get; set; } = new List<AssembleScanItem>();

        private static object _scanLock = new object();

        /// <summary>
        /// 添加扫描项目
        /// </summary>
        /// <param name="action">扫描过程</param>
        /// <param name="runScanNow">是否立即扫描</param>
        /// <param name="dllFilePatterns">被包含的 dll 的文件名，“.Xncf.”会被必定包含在里面</param>
        public static void AddAssembleScanItem(Action<Assembly> action, bool runScanNow, string[] dllFilePatterns = null)
        {
            ScanAssamblesActions.Add(new AssembleScanItem(action));

            if (runScanNow)
            {
                RunScan(dllFilePatterns);//立即扫描
            }
        }


        /// <summary>
        /// 执行扫描
        /// </summary>
        /// <param name="dllFilePatterns">被包含的 dll 的文件名，“.Xncf.”会被必定包含在里面</param>
        public static void RunScan(string[] dllFilePatterns)
        {
            var dt1 = SystemTime.Now;

            lock (_scanLock)
            {
                //查找所有扩展缓存B
                var scanTypesCount = 0;

                var assemblies = GetAssembiles(dynamicLoadAllDlls: true, dllFilePatterns: dllFilePatterns);
                var toScanItems = ScanAssamblesActions.Where(z => z.ScanFinished == false).ToList();

                //搜索所有未被引用的项目

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        scanTypesCount++;
                        foreach (var scanItem in toScanItems)
                        {
                            scanItem.Run(assembly);//执行扫描过程
                        }
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.SendCustomLog("ScanAssambles() 自动扫描程序集报告（非程序异常）：" + assembly.FullName, ex.ToString());
                    }
                }

                toScanItems.ForEach(z => z.Finished());//标记结束

                var dt2 = SystemTime.Now;
                SenparcTrace.SendCustomLog("ScanAssambles", $"RegisterAllAreas 用时：{(dt2 - dt1).TotalMilliseconds}ms");
            }


        }

        #region 获取程序集


        private static List<Assembly> AllAssemblies = null;
        private static object AllAssembliesLock = new object();

        /// <summary>
        /// 获取程序集
        /// </summary>
        /// <param name="dynamicLoadAllDlls">是否从 dll 目录加载未被程序引用的其他程序集，默认为 true</param>
        /// <param name="useCachedData">是否使用已缓存的数据，默认为true</param>
        /// <param name="forceUpdateCache">强制重新获取并更新缓存，此时会忽略 <paramref name="useCachedData"/> 的设置</param>
        /// <param name="dllFilePatterns">被包含的 dll 的文件名，“.Xncf.”会被必定包含在里面</param>
        public static List<Assembly> GetAssembiles(bool dynamicLoadAllDlls = true, bool useCachedData = true, bool forceUpdateCache = true, string[] dllFilePatterns = null)
        {
            lock (AllAssembliesLock)
            {
                if (!forceUpdateCache && useCachedData && AllAssemblies != null)
                {
                    return AllAssemblies;
                }

                var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();

                #region 补全未被引用的程序集

                if (dynamicLoadAllDlls)
                {
                    // 使用 AppDomain 或环境变量来动态获取路径
                    string directoryPath = AppDomain.CurrentDomain.BaseDirectory;
                    // 如果需要，也可以考虑使用环境变量
                    // string directoryPath = Environment.GetEnvironmentVariable("MY_APP_PATH");

                    // 其余的步骤与之前相同，遍历和尝试加载 DLL
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
                                    Console.WriteLine($"Already loaded: {assemblyName}");
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

                //更新缓存
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

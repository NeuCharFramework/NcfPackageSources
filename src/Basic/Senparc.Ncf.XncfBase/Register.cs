using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase.Threads;
using Senparc.Ncf.XncfBase.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Xscf 全局注册类
    /// </summary>
    public static class Register
    {
        /// <summary>
        /// 模块和方法集合。 TODO：可放置到缓存中
        /// </summary>
        public static List<IXscfRegister> RegisterList { get; set; } = new List<IXscfRegister>();
        /// <summary>
        /// 带有数据库的模块 TODO：可放置到缓存中
        /// </summary>
        public static List<IXscfDatabase> XscfDatabaseList => RegisterList.Where(z => z is IXscfDatabase).Select(z => z as IXscfDatabase).ToList();
        /// <summary>
        /// 所有线程的集合
        /// </summary>
        public static ConcurrentDictionary<ThreadInfo, Thread> ThreadCollection = new ConcurrentDictionary<ThreadInfo, Thread>();

        /// <summary>
        /// 所有自动注册 Xscf 的数据库的 ConfigurationMapping 对象
        /// </summary>
        public static List<object> XscfAutoConfigurationMappingList = new List<object>();
        //public static List<IEntityTypeConfiguration<EntityBase>> XscfAutoConfigurationMappingList = new List<IEntityTypeConfiguration<EntityBase>>();

        /// <summary>
        /// 扫描程序集分类
        /// </summary>
        enum ScanTypeKind
        {
            IXscfRegister,
            IXscfFunction,
            XscfAutoConfigurationMappingAttribute
        }

        /// <summary>
        /// 启动 XSCF 模块引擎，包括初始化扫描和注册等过程
        /// </summary>
        /// <returns></returns>
        public static string StartEngine(this IServiceCollection services, IConfiguration configuration)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[{SystemTime.Now}] 开始初始化扫描 XscfModules");
            var scanTypesCount = 0;
            var hideTypeCount = 0;
            ConcurrentDictionary<Type, ScanTypeKind> types = new ConcurrentDictionary<Type, ScanTypeKind>();

            //所有 XSCF 模块，包括被忽略的。
            //var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
            //using (cache.BeginCacheLock("Senparc.Ncf.XncfBase.Register", "Scan")) //在注册阶段还未完成缓存配置
            {

                try
                {
                    //遍历所有程序集
                    foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        scanTypesCount++;
                        var aTypes = a.GetTypes();
                        foreach (var t in aTypes)
                        {
                            if (t.IsAbstract)
                            {
                                continue;
                            }

                            if (t.GetInterfaces().Contains(typeof(IXscfRegister)))
                            {
                                types[t] = ScanTypeKind.IXscfRegister;
                            }
                            else if (t.GetInterfaces().Contains(typeof(IXscfFunction)))
                            {
                                types[t] = ScanTypeKind.IXscfFunction; /* 暂时不收录处理 */
                            }
                            else if (t.GetCustomAttributes(true).FirstOrDefault(z => z is XscfAutoConfigurationMappingAttribute) != null
                                /*&& t.GetInterfaces().Contains(typeof(IEntityTypeConfiguration<>))*/)
                            {
                                types[t] = ScanTypeKind.XscfAutoConfigurationMappingAttribute;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"扫描程集异常退出，可能无法获得完整程序集信息：{ex.Message}");
                }

                sb.AppendLine($"[{SystemTime.Now}] 满足条件对象：{types.Count()}");

                //先注册 XscfRegister
                {
                    //筛选
                    var allTypes = types.Where(z => z.Value == ScanTypeKind.IXscfRegister/* && z.Key.GetInterfaces().Contains(typeof(IXscfRegister))*/)
                        .Select(z => z.Key);
                    //按照优先级进行排序
                    var orderedTypes = allTypes.OrderByDescending(z =>
                    {
                        var orderAttribute = z.GetCustomAttributes(true).FirstOrDefault(attr => attr is XscfOrderAttribute) as XscfOrderAttribute;
                        if (orderAttribute != null)
                        {
                            return orderAttribute.Order;
                        }
                        return 0;
                    });


                    foreach (var type in orderedTypes)
                    {
                        sb.AppendLine($"[{SystemTime.Now}] 扫描到 IXscfRegister：{type.FullName}");

                        var register = type.Assembly.CreateInstance(type.FullName) as IXscfRegister;

                        if (!RegisterList.Contains(register))
                        {
                            if (RegisterList.Exists(z => z.Uid.Equals(register.Uid, StringComparison.OrdinalIgnoreCase)))
                            {
                                throw new XscfFunctionException("已经存在相同 Uid 的模块：" + register.Uid);
                            }

                            if (register.IgnoreInstall)
                            {
                                hideTypeCount++;
                            }
                            RegisterList.Add(register);//只有允许安装的才进行注册，否则执行完即结束
                            services.AddScoped(type);//DI 中注册
                            foreach (var functionType in register.Functions)
                            {
                                services.AddScoped(functionType);//DI 中注册
                            }
                        }
                    }

                    #region 暂时不收录 IXscfFunction

                    /* 暂时不收录 */
                    ////再扫描具体方法
                    //foreach (var type in types.Where(z => z != null && z.GetInterfaces().Contains(typeof(IXscfFunction))))
                    //{
                    //    sb.AppendLine($"[{SystemTime.Now}] 扫描到 IXscfFunction：{type.FullName}");

                    //    if (!ModuleFunctionCollection.ContainsKey(type))
                    //    {
                    //        throw new SCFExceptionBase($"{type.FullName} 未能提供正确的注册方法！");
                    //    }

                    //    var function = type as IXscfFunction;
                    //    ModuleFunctionCollection[type].Add(function);
                    //}

                    #endregion
                }

                //处理 XscfAutoConfigurationMappingAttribute
                {
                    var allTypes = types.Where(z => z.Value == ScanTypeKind.XscfAutoConfigurationMappingAttribute).Select(z => z.Key);
                    foreach (var type in allTypes)
                    {
                        var obj = type.Assembly.CreateInstance(type.FullName);
                        XscfAutoConfigurationMappingList.Add(obj /*as IEntityTypeConfiguration<EntityBase>*/);
                    }
                }
            }

            var scanResult = "初始化扫描结束，共扫描 {scanTypesCount} 个程序集";
            if (hideTypeCount > 0)
            {
                scanResult += $"。其中 {hideTypeCount} 个程序集为非安装程序集，不会被缓存";
            }
            sb.AppendLine($"[{SystemTime.Now}] {scanResult}");


            //Repository & Service
            services.AddScoped(typeof(Senparc.Ncf.Repository.IRepositoryBase<>), typeof(Senparc.Ncf.Repository.RepositoryBase<>));
            services.AddScoped(typeof(ServiceBase<>));
            services.AddScoped(typeof(IServiceBase<>), typeof(ServiceBase<>));

            //ConfigurationMapping
            services.AddScoped(typeof(ConfigurationMappingBase<>));
            services.AddScoped(typeof(ConfigurationMappingWithIdBase<,>));

            //微模块进行 Service 注册
            foreach (var xscfRegister in RegisterList)
            {
                xscfRegister.AddXscfModule(services, configuration);
            }
            sb.AppendLine($"[{SystemTime.Now}] 完成模块 services.AddXscfModule()：共扫描 {scanTypesCount} 个程序集");

            //支持 AutoMapper
            //引入当前系统
            services.AddAutoMapper(z => z.AddProfile<Core.AutoMapper.SystemProfile>());
            //引入所有模块
            services.AddAutoMapper(z => z.AddProfile<AutoMapper.XscfModuleProfile>());

            return sb.ToString();
        }

        /// <summary>
        /// 扫描并安装
        /// </summary>
        /// <param name="xscfModuleDtos">现有已安装的模块</param>
        /// <param name="serviceProvider">IServiceProvider</param>
        /// <param name="afterInstalledOrUpdated">安装或更新后执行</param>
        /// <param name="justScanThisUid">只扫描并更新特定的Uid</param>
        /// <returns></returns>
        public static async Task<string> ScanAndInstall(IList<CreateOrUpdate_XscfModuleDto> xscfModuleDtos,
            IServiceProvider serviceProvider,
            Func<IXscfRegister, InstallOrUpdate, Task> afterInstalledOrUpdated = null,
            string justScanThisUid = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[{SystemTime.Now}] 开始扫描 XscfModules");

            //先注册
            var updatedCount = 0;
            var cache = CacheStrategyFactory.GetObjectCacheStrategyInstance();
            using (await cache.BeginCacheLockAsync("Senparc.Ncf.XncfBase.Register", "Scan").ConfigureAwait(false))
            {
                foreach (var register in RegisterList)
                {
                    sb.AppendLine($"[{SystemTime.Now}] 扫描到 IXscfRegister：{register.GetType().FullName}");
                    if (register.IgnoreInstall)
                    {
                        sb.AppendLine($"[{SystemTime.Now}] 当前模块要求忽略安装 uid：[{justScanThisUid}]，此模块跳过");
                        continue;
                    }

                    if (justScanThisUid != null && register.Uid != justScanThisUid)
                    {
                        sb.AppendLine($"[{SystemTime.Now}] 由于只要求更新 uid：[{justScanThisUid}]，此模块跳过");
                        continue;
                    }
                    else
                    {
                        sb.AppendLine($"[{SystemTime.Now}] 符合尝试安装/更新要求，继续执行");
                    }

                    var xscfModuleStoredDto = xscfModuleDtos.FirstOrDefault(z => z.Uid == register.Uid);
                    var xscfModuleAssemblyDto = new UpdateVersion_XscfModuleDto(register.Name, register.Uid, register.MenuName, register.Version, register.Description);

                    //检查更新，并安装到数据库
                    var xscfModuleService = serviceProvider.GetService<XscfModuleService>();
                    var installOrUpdate = await xscfModuleService.CheckAndUpdateVersionAsync(xscfModuleStoredDto, xscfModuleAssemblyDto).ConfigureAwait(false);
                    sb.AppendLine($"[{SystemTime.Now}] 是否更新版本：{installOrUpdate?.ToString() ?? "未安装"}");

                    if (installOrUpdate.HasValue)
                    {
                        updatedCount++;

                        //执行安装程序
                        await register.InstallOrUpdateAsync(serviceProvider, installOrUpdate.Value).ConfigureAwait(false);

                        await afterInstalledOrUpdated?.Invoke(register, installOrUpdate.Value);
                    }
                }
            }

            sb.AppendLine($"[{SystemTime.Now}] 扫描结束，共新增或更新 {updatedCount} 个程序集");
            return sb.ToString();
        }

        /// <summary>
        /// 通常在 Startup.cs 中的 Configure() 方法中执行
        /// </summary>
        /// <param name="app"></param>
        /// <param name="registerService">CO2NET 注册对象</param>
        /// <returns></returns>
        public static IApplicationBuilder UseXscfModules(IApplicationBuilder app, IRegisterService registerService)
        {
            foreach (var register in RegisterList)
            {
                try
                {
                    register.UseXscfModule(app, registerService);
                }
                catch
                {
                }

                //执行中间件
                if (register is IXscfMiddleware middlewareRegister)
                {
                    try
                    {
                        middlewareRegister.UseMiddleware(app);
                    }
                    catch
                    {
                    }
                }

                //执行线程
                if (register is IXscfThread threadRegister)
                {
                    try
                    {
                        XscfThreadBuilder xscfThreadBuilder = new XscfThreadBuilder();
                        threadRegister.ThreadConfig(xscfThreadBuilder);
                        xscfThreadBuilder.Build(app, register);
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.BaseExceptionLog(ex);
                    }
                }
            }
            return app;
        }

        /// <summary>
        /// 所有已经使用的 [AutoConfigurationMapping] 对应的实体类型
        /// </summary>
        public static List<Type> ApplyedAutoConfigurationMappingTypes = new List<Type>();
        /// <summary>
        /// 自动添加所有 XSCF 模块中标记了 [XscfAutoConfigurationMapping] 特性的对象
        /// </summary>
        public static void ApplyAllAutoConfigurationMapping(ModelBuilder modelBuilder)
        {
            var entityTypeConfigurationMethod = typeof(ModelBuilder).GetMethods()
                .FirstOrDefault(z => z.Name == "ApplyConfiguration" && z.ContainsGenericParameters && z.GetParameters().SingleOrDefault()?.ParameterType.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

            //所有模块中数据库实体中自动获取所有的 DbSet 下的实体类型
            foreach (var databaseRegister in Register.XscfDatabaseList)
            {
                if (databaseRegister.XscfDatabaseDbContextType != null)
                {
                    var setKeyInfoList = EntitySetKeys.GetEntitySetInfo(databaseRegister.XscfDatabaseDbContextType).Values;
                    foreach (var setKeyInfo in setKeyInfoList)
                    {
                        //数据库实体类型
                        var entityType = setKeyInfo.DbSetType;
                        //默认空 ConfigurationMapping 对象的泛型类型
                        var blankEntityTypeConfigurationType = typeof(BlankEntityTypeConfiguration<>).MakeGenericType(entityType);
                        //创建一个新的实例
                        var blankEntityTypeConfiguration = Activator.CreateInstance(blankEntityTypeConfigurationType);
                        //最佳到末尾，这样可以优先执行用户自定义的代码
                        XscfAutoConfigurationMappingList.Add(blankEntityTypeConfiguration);
                    }
                }
            }

            foreach (var autoConfigurationMapping in XscfAutoConfigurationMappingList)
            {
                if (autoConfigurationMapping == null)
                {
                    continue;
                }

                SenparcTrace.SendCustomLog("监测到 ApplyAllAutoConfigurationMapping 执行", autoConfigurationMapping.GetType().FullName);

                //获取配置实体类型，如：DbConfig_WeixinUserConfigurationMapping
                Type mappintConfigType = autoConfigurationMapping.GetType();
                //获取 IEntityTypeConfiguration<Entity> 接口
                var interfaceType = mappintConfigType.GetInterfaces().FirstOrDefault(z => z.Name.StartsWith("IEntityTypeConfiguration"));
                if (interfaceType == null)
                {
                    continue;
                }
                //实体类型，如：DbConfig
                Type entityType = interfaceType.GenericTypeArguments[0];
                if (ApplyedAutoConfigurationMappingTypes.Contains(entityType))
                {
                    continue;//如果已经添加过则跳过。作此判断因为：原始的 XscfAutoConfigurationMappingList 数据可能和上一步自动添加 DataSet 中的对象有重复
                }

                entityTypeConfigurationMethod.MakeGenericMethod(entityType)
                                .Invoke(modelBuilder, new object[1]
                                {
                                    autoConfigurationMapping
                 });

                ApplyedAutoConfigurationMappingTypes.Add(entityType);

                //entityTypeConfigurationMethod.Invoke(modelBuilder, new[] { autoConfigurationMapping });
            }

            //TODO：添加 IQueryTypeConfiguration<>


        }
    }
}

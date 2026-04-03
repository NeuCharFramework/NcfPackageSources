using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using ModelContextProtocol.Protocol;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.CO2NET.ApiBind;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Trace;
using Senparc.CO2NET.Utilities;
using Senparc.CO2NET.WebApi;
using Senparc.CO2NET.WebApi.WebApiEngines;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using Senparc.NeuChar;
using Senparc.Weixin.AspNet.MCP;
using Senparc.Weixin.AspNet.RegisterServices;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP.AdvancedAPIs.UserTag;
using Senparc.Weixin.MP.Containers;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.WeixinManager.Domain.Models.AutoMapper;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Services;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager
{
    [XncfRegister]
    [XncfOrder(5880)]
    public partial class Register : XncfRegisterBase, IXncfRegister //Register XNCF basic module interface (required)
    {
        #region IXncfRegister 接口

        public override string Name => "Senparc.Xncf.WeixinManager";

        public override string Uid => "EB84CB21-AC22-406E-0001-000000000001";


        public override string Version => "0.21.1";


        public override string MenuName => "微信管理";


        public override string Icon => "fa fa-weixin";


        public override string Description => @"XNCF 模块：盛派官方发布的微信管理后台
使用此插件可以在 NCF 中快速集成微信公众号、小程序的部分基础管理功能，欢迎大家一起扩展！
微信 SDK 基于 Senparc.Weixin SDK 开发。开源地址：https://https://github.com/JeffreySu/WeiXinMPSDK";

        //public override IList<Type> Functions => new Type[] { };

        public override bool EnableMcpServer => true;

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            //services.AddScoped<PostModel>(ServiceProvider =>
            //{
            //    //Generate different PostModels based on conditions
            //});
            services.AddScoped<IAiHandler, SemanticAiHandler>();
            services.AddScoped<ISenparcAiSetting, SenparcAiSetting>();
            services.AddAutoMapper(z => z.AddProfile<WeixinManagerProfile>());
            services.AddScoped<MpAccountService>();
            services.AddScoped<PromptItemService>();

            var autoCreateApi = false;//Whether to automatically generate API
            services.AddSenparcWeixin(configuration, env, autoCreateApi);

            return base.AddXncfModule(services, configuration, env);//If you override this method, you must call the base class method
        }

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //Update database when installing or upgrading a version
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            //TODO: You can give the option whether to delete it in the basic module

            #region 删除数据库（演示）

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            WeixinSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as WeixinSenparcEntities;

            //Specify the data entity to be deleted

            //Note: As a demonstration, all tables created by this module are deleted when uninstalling the module. During actual operation, please operate with caution and sort the entities in the order of deletion!
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            //Sort by deletion order
            var types = new[] { typeof(UserTag_WeixinUser), typeof(UserTag), typeof(WeixinUser), typeof(MpAccount) };
            types.ToList().AddRange(dropTableKeys);
            types = types.Distinct().ToArray();
            //Specify the data entity to be deleted
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, types);

            #endregion

            await base.UninstallAsync(serviceProvider, unsinstallFunc).ConfigureAwait(false);
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            //Wait for database registration to complete before running
            _ = Task.Factory.StartNew(async () =>
            {
                //Register WeChat
                Senparc.Weixin.WeixinRegister.UseSenparcWeixin(null, null, senparcSetting: null);
                
                //Wait for database registration to complete before running
                while (Ncf.Database.Register.UseNcfDatabaseSetted is false)
                {
                    await Task.Delay(1000);
                }
                
                //Start querying the database
                try
                {
                    //An error may occur if the database table is not installed, so try
                    using (var scope = app.ApplicationServices.CreateScope())
                    {
                        var mpAccountService = scope.ServiceProvider.GetRequiredService<MpAccountService>();
                        var allMpAccount = mpAccountService.GetAllMpAccounts();

                        //Automatically register public accounts in batches
                        allMpAccount.AsParallel().ForAll(mpAccount =>
                        {
                            AccessTokenContainer.RegisterAsync(mpAccount.AppId, mpAccount.AppSecret, $"{mpAccount.Name}-{mpAccount.Id}");
                            //TODO: More dynamic registration during execution
                        });
                    }
                }
                catch (Exception ex)
                {
                    SenparcTrace.BaseExceptionLog(ex);
                }

            });

            //app.UseSwagger();
            //app.UseSwaggerUI(c =>
            //{
            //    //c.DocumentTitle = "Senparc Weixin SDK Demo API";
            //    c.InjectJavascript("/lib/jquery/dist/jquery.min.js");
            //    c.InjectJavascript("/js/swagger.js");
            //    //c.InjectJavascript("/js/tongji.js");
            //    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);

            //    foreach (var neucharApiDocAssembly in WeixinApiService.WeixinApiAssemblyCollection)
            //    {

            //        //TODO: real dynamic version number
            //        var verion = WeixinApiService.WeixinApiAssemblyVersions[neucharApiDocAssembly.Key]; //neucharApiDocAssembly.Value.ImageRuntimeVersion;
            //        var docName = WeixinApiService.GetDocName(neucharApiDocAssembly.Key);

            //        //Console.WriteLine($"\tAdd {docName}");

            //        c.SwaggerEndpoint($"/swagger/{docName}/swagger.json", $"{neucharApiDocAssembly.Key} v{verion}");
            //    }

            //});

            //var apiList = Senparc.CO2NET.WebApi.FindApiService.ApiItemList;

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapMcp("WeChatMcp");
            //});

            return base.UseXncfModule(app, registerService);
        }
        #endregion

        class RemoveVerbsFilter : IDocumentFilter
        {
            //public void Apply(SwaggerDocument swaggerDoc, SchemaRegistry schemaRegistry, IApiExplorer apiExplorer)
            //{
            //    foreach (PathItem path in swaggerDoc.paths.Values)
            //    {
            //        path.delete = null;
            //        //path.get = null; // leaving GET in
            //        path.head = null;
            //        path.options = null;
            //        path.patch = null;
            //        path.post = null;
            //        path.put = null;
            //    }
            //}

            public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
            {
                //Every time you switch definitions, it takes a long time to get here.

                return;
                string platformType;
                var title = swaggerDoc.Info.Title;

                if (title.Contains(PlatformType.WeChat_OfficialAccount.ToString()))
                {
                    platformType = PlatformType.WeChat_OfficialAccount.ToString();
                }
                else if (title.Contains(PlatformType.WeChat_Work.ToString()))
                {
                    platformType = PlatformType.WeChat_Work.ToString();
                }
                else if (title.Contains(PlatformType.WeChat_Open.ToString()))
                {
                    platformType = PlatformType.WeChat_Open.ToString();
                }
                else if (title.Contains(PlatformType.WeChat_MiniProgram.ToString()))
                {
                    platformType = PlatformType.WeChat_MiniProgram.ToString();
                }
                //else if (title.Contains(PlatformType.General.ToString()))
                //{
                //    platformType = PlatformType.General.ToString();
                //}
                else
                {
                    throw new NotImplementedException($"未提供的 PlatformType 类型，Title：{title}");
                }

                var pathList = swaggerDoc.Paths.Keys.ToList();

                foreach (var path in pathList)
                {
                    if (!path.Contains(platformType))
                    {
                        //Remove API objects that are not in the current module
                        swaggerDoc.Paths.Remove(path);
                    }
                }

                //SwaggerOperationAttribute
                //Remove Schema object
                //var toRemoveSchema = context.SchemaRepository.Schemas.Where(z => !z.Key.Contains(platformType)).ToList();//The result is all deletion, only testing
                //foreach (var schema in toRemoveSchema)
                //{
                //    context.SchemaRepository.Schemas.Remove(schema.Key);
                //}
            }
        }

        public override void AddMcpServer(IServiceCollection services, IXncfRegister xncfRegister)
        {
            //base.AddMcpServer(services, xncfRegister);

            var serverName = GetMcpServerName();

            var mcpServerBuilder = services.AddMcpServer(opt =>
            {
                opt.ServerInfo = new Implementation()
                {
                    Name = serverName,
                    Version = this.Version,
                };
            })
            .WithHttpTransport()
            //.WithTools(new[] {typeof(WeChatMcpRouter) })
            .WithToolsFromAssembly(xncfRegister.GetType().Assembly);

            XncfRegisterManager.McpServerInfoCollection[serverName] = new Ncf.XncfBase.MCP.McpServerInfo()
            {
                ServerName = serverName,
                XncfName = Name,
                XncfUid = Uid
            };
        }

        public override void UseMcpServer(IApplicationBuilder app, IRegisterService registerService)
        {
            base.UseMcpServer(app, registerService);
        }

        //public class AuthResponsesOperationFilter : IOperationFilter
        //{
        //    public void Apply(OpenApiOperation operation, OperationFilterContext context)
        //    {
        //        //Get whether to add login features
        //        var authAttributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
        //         .Union(context.MethodInfo.GetCustomAttributes(true))
        //         .OfType<AuthorizeAttribute>().Any();

        //        if (authAttributes)
        //        {
        //            operation.Responses.Add("401", new OpenApiResponse { Description = "No access rights yet" });
        //            operation.Responses.Add("403", new OpenApiResponse { Description = "Access Forbidden" });
        //            operation.Security = new List<OpenApiSecurityRequirement>
        //            {
        //                new OpenApiSecurityRequirement { { new OpenApiSecurityScheme() {  Name= "oauth2" }, new[] { "swagger_api" } }}
        //            };
        //        }
        //    }
        //}

    }
}



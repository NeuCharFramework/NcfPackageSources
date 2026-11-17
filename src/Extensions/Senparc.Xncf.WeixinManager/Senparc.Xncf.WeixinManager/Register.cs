/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: module registration and initialization logic

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260717
    Change description: v0.23.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20260731
    Change description: v0.24.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.24.16 Remove DatabaseSample and related components from MCP extension

    Modified by: Senparc - 20261111
    Change description: v0.24.18 unify XNCF Register display metadata and localization resource keys

----------------------------------------------------------------*/

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.OpenApi;
using ModelContextProtocol.Protocol;
using Senparc.AI.Interfaces;
using Senparc.AI.AgentKernel;
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
using Senparc.Xncf.Connectors.Abstractions;
using Senparc.Xncf.WeixinManager.Connectors;
using Senparc.Xncf.WeixinManager.WeixinClaw;
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
    public partial class Register : XncfRegisterBase, IXncfRegister //Legacy comment translated to English.
    {
        private static readonly XncfRegisterText RegisterText = XncfRegisterText.Create<WeixinManagerResource>(
            "Module.WeixinManager",
            "微信管理",
            "盛派官方微信管理后台，可快速集成微信公众号和小程序的基础管理功能。基于 Senparc.Weixin SDK：https://github.com/JeffreySu/WeiXinMPSDK");

        #region Legacy section

        public override string Name => "Senparc.Xncf.WeixinManager";

        public override string Uid => "EB84CB21-AC22-406E-0001-000000000001";


        public override string Version => "0.24.13";


        public override string MenuName => RegisterText.MenuName;


        public override string Icon => "fa fa-weixin";


        public override string Description => RegisterText.Description;

        //public override IList<Type> Functions => new Type[] { };

        public override bool EnableMcpServer => true;

        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            //services.AddScoped<PostModel>(ServiceProvider =>
            //{
            //Legacy comment translated to English.
            //});
            services.AddScoped<IAiHandler, AgentAiHandler>();
            services.AddScoped<ISenparcAiSetting, SenparcAiSetting>();
            services.AddAutoMapper(z => z.AddProfile<WeixinManagerProfile>());
            services.AddDataProtection();
            services.AddScoped<MpAccountService>();
            services.AddScoped<WeixinClawAccountService>();
            services.AddScoped<WeixinClawMessageReceiptService>();
            services.AddScoped<WeixinClawMessageRecordService>();
            services.AddScoped<WeixinClawMediaStorageService>();
            services.AddScoped<WeixinClawMediaPlaybackService>();
            services.Configure<WeixinClawHostedServiceOptions>(
                configuration.GetSection(WeixinClawHostedServiceOptions.SectionName));
            services.AddScoped<WeixinClawFileManagerBridge>();
            services.AddScoped<WeixinClawMediaService>();
            services.AddScoped<WeixinClawBindingProfileService>();
            services.AddScoped<IWeixinClawBindingProfileResolver>(
                serviceProvider => serviceProvider.GetRequiredService<WeixinClawBindingProfileService>());
            services.AddScoped<WeixinClawMessageService>();
            services.AddScoped<IWeixinClawMessageSender>(serviceProvider =>
                serviceProvider.GetRequiredService<WeixinClawMessageService>());
            services.AddScoped<WeixinClawMessageDispatcher>();
            services.AddScoped<IWeixinClawMessageHandler, WeixinClawAiMessageHandler>();
            services.AddSingleton<WeixinClawLoginService>();
            services.AddSingleton<IHttpMessageHandlerBuilderFilter, WeixinClawHttpMessageHandlerBuilderFilter>();
            services.AddHttpClient<WeixinClawApi>();
            services.AddHostedService<WeixinClawHostedService>();
            services.AddScoped<PromptItemService>();
            services.AddSingleton<IConnectorProvider, WeixinWorkCheckinConnector>();

            var autoCreateApi = false;//whetherautomaticgenerateAPI
            services.AddSenparcWeixin(configuration, env, autoCreateApi);

            return base.AddXncfModule(services, configuration, env);//Legacy comment translated to English.
        }

        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //update the database when installing or upgrading a version
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            //Legacy comment translated to English.

            #region Legacy section

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            WeixinSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as WeixinSenparcEntities;

            //specify the data entities to delete

            //Note: this is a demonstration that deletes all tables created by this module during uninstallation. In production, proceed carefully and order entities by their deletion dependencies.
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            //Legacy comment translated to English.
            var types = new[]
            {
                typeof(WeixinClawMessageReceipt),
                typeof(WeixinClawMessageRecord),
                typeof(WeixinClawBindingProfile),
                typeof(UserTag_WeixinUser),
                typeof(UserTag),
                typeof(WeixinUser),
                typeof(WeixinClawAccount),
                typeof(MpAccount)
            };
            var dropTypes = dropTableKeys
                .Where(z => !types.Contains(z))
                .ToList();
            dropTypes.AddRange(types);
            types = dropTypes.ToArray();
            //specify the data entities to delete
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, types);

            #endregion

            await base.UninstallAsync(serviceProvider, unsinstallFunc).ConfigureAwait(false);
        }

        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            //Legacy comment translated to English.
            _ = Task.Factory.StartNew(async () =>
            {
                //Legacy comment translated to English.
                Senparc.Weixin.WeixinRegister.UseSenparcWeixin(null, null, senparcSetting: null);
                
                //Legacy comment translated to English.
                while (Ncf.Database.Register.UseNcfDatabaseSetted is false)
                {
                    await Task.Delay(1000);
                }
                
                //Legacy comment translated to English.
                try
                {
                    //Legacy comment translated to English.
                    using (var scope = app.ApplicationServices.CreateScope())
                    {
                        var mpAccountService = scope.ServiceProvider.GetRequiredService<MpAccountService>();
                        var allMpAccount = mpAccountService.GetAllMpAccounts();

                        //Legacy comment translated to English.
                        allMpAccount.AsParallel().ForAll(mpAccount =>
                        {
                            AccessTokenContainer.RegisterAsync(mpAccount.AppId, mpAccount.AppSecret, $"{mpAccount.Name}-{mpAccount.Id}");
                            //Legacy comment translated to English.
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

            //Legacy comment translated to English.
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
                //Legacy comment translated to English.

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
                    throw new NotImplementedException(WeixinManagerResource.Format("Error.UnsupportedPlatformType", "Unsupported PlatformType. Title: {0}", title));
                }

                var pathList = swaggerDoc.Paths.Keys.ToList();

                foreach (var path in pathList)
                {
                    if (!path.Contains(platformType))
                    {
                        //Legacy comment translated to English.
                        swaggerDoc.Paths.Remove(path);
                    }
                }

                //SwaggerOperationAttribute
                //Legacy comment translated to English.
                //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        //        var authAttributes = context.MethodInfo.DeclaringType.GetCustomAttributes(true)
        //         .Union(context.MethodInfo.GetCustomAttributes(true))
        //         .OfType<AuthorizeAttribute>().Any();

        //        if (authAttributes)
        //        {
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //            operation.Security = new List<OpenApiSecurityRequirement>
        //            {
        //                new OpenApiSecurityRequirement { { new OpenApiSecurityScheme() {  Name= "oauth2" }, new[] { "swagger_api" } }}
        //            };
        //        }
        //    }
        //}

    }
}

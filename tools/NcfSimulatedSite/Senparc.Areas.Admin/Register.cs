/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.cs
    File description: module registration and initialization logic

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260705
    Change description: v0.0.3 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.0.4 Updated module behavior and related support.

    Modified by: Senparc - 20260724
    Change description: v0.1.0 enhance batch module updates and improve the multilingual management interface

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20260804
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20260804
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20260808
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20260812
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.5.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.6.0 Updated module behavior and related support.

    Modified by: Senparc - 20260829
    Change description: v0.7.0 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20260916
    Change description: v0.9.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 implement AdminChat memory storage and management system

    Modified by: Senparc - 20261103
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: v0.12.28 Updated module behavior and related support.

----------------------------------------------------------------*/

/* 

 * Legacy comment translated to English.

 * Legacy comment translated to English.

 * Legacy comment translated to English.

 * Legacy comment translated to English.

 */

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.ACL.Repository;
using Senparc.Areas.Admin.Domain;
using Senparc.Areas.Admin.Domain.Cache;
using Senparc.Areas.Admin.Domain.Dto;
//using Senparc.Areas.Admin.Authorization;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Areas.Admin.WeixinClawIntegration;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Localization;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Shared.Abstractions.Security;
using Senparc.Ncf.Shared.Abstractions.ChatAgent;
using Senparc.Ncf.Service;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using Senparc.Ncf.XncfBase.Functions.Files;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin
{
    [XncfRegister]
    [XncfOrder(5996)]
    public class Register : XncfRegisterBase,
        IXncfRegister, //Legacy comment translated to English.
        IAreaRegister, //register the XNCF page interface (optional)
        IXncfDatabase  //register the XNCF module database (optional)
                       //Legacy comment translated to English.
    {
        private static readonly XncfRegisterText RegisterText =
            XncfRegisterText.Create<AdminResource>(
                "Admin.Register",
                "NCF 系统管理员后台",
                "这是管理员后台模块，用于 NCF 系统后台的自我管理，请勿删除此模块。如果你实在忍不住，请务必做好数据备份。");

        public const string ModuleUid = SiteConfig.SYSTEM_XNCF_MODULE_AREAS_ADMIN_UID;

        #region Legacy section

        public override string Name => "NeuCharFramework.Admin";

        public override string Uid => ModuleUid;// "00000000-0000-0001-0001-000000000001";

        public override string Version => "0.5.14";

        public override string MenuName => RegisterText.MenuName;

        public override string Icon => "fa fa-university";

        public override string Description => RegisterText.Description;


        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //updatedatabase
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //Legacy comment translated to English.
            var adminAuthConfigService = serviceProvider.GetService<AdminAuthConfigService>();
            if (adminAuthConfigService != null)
            {
                try
                {
                    var settings = adminAuthConfigService.GetEffectiveExpireSettings();
                    if (settings.UsingDefault && settings.Source == "missing-record")
                    {
                        await adminAuthConfigService.TrySaveExpireSettingsAsync(
                            AdminAuthConfig.DefaultAdminWebLoginExpireMinutes,
                            AdminAuthConfig.DefaultBackendJwtExpireMinutes);
                    }
                }
                catch (Exception ex)
                {
                    SenparcTrace.SendCustomLog("AdminAuthConfig 默认初始化失败", ex.ToString());
                }
            }

            //XncfModuleServiceExtension xncfModuleServiceExtension = serviceProvider.GetService<XncfModuleServiceExtension>();
            //var adminModule = xncfModuleServiceExtension.GetObject(z => z.Uid == this.Uid);
            //if (adminModule == null)
            //{
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //    await xncfModuleServiceExtension.InstallModuleAsync(this.Uid).ConfigureAwait(false);
            //}

            await base.InstallOrUpdateAsync(serviceProvider, installOrUpdate);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            //Legacy comment translated to English.

            #region Legacy section

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            AdminSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as AdminSenparcEntities;

            //specify the data entities to delete

            //Note: this is a demonstration that deletes all tables created by this module during uninstallation. In production, proceed carefully and order entities by their deletion dependencies.
            var dropTableKeys = EntitySetKeys.GetEntitySetInfo(this.TryGetXncfDatabaseDbContextType).Keys.ToArray();
            await base.DropTablesAsync(serviceProvider, mySenparcEntities, dropTableKeys);

            #endregion

            await base.UninstallAsync(serviceProvider, unsinstallFunc);
        }


        public override IServiceCollection AddXncfModule(IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
        {
            //Attributes
            services.AddScoped<AuthenticationResultFilterAttribute>();
            //services.AddScoped(typeof(AuthenticationAsyncPageFilterAttribute));

            services.Configure<JwtSettings>(JwtSettings.Position_Backend, configuration.GetSection(JwtSettings.Position_Backend));//Legacy comment translated to English.
            services.Configure<FunctionFileStoreOptions>(
                configuration.GetSection(FunctionFileStoreOptions.SectionName));
            services.Configure<WeixinClawAdminIntegrationOptions>(
                configuration.GetSection(WeixinClawAdminIntegrationOptions.SectionName));

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            //AutoMap mapping
            base.AddAutoMapMapping(profile =>
            {
                profile.CreateMap<AdminUserInfo, CreateOrUpdate_AdminUserInfoDto>();
                profile.CreateMap<SystemConfig, SystemConfigDto>().ReverseMap();
                profile.CreateMap<SystemConfig_CreateOrUpdateDto, SystemConfig>().ReverseMap();
                profile.CreateMap<XncfModule, XncfModuleDto>().ReverseMap();
                profile.CreateMap<XncfModuleDisplayDto, XncfModule>().ReverseMap();
            });

            AddJwtAuthentication(services, configuration);

            services.AddScoped<IAdminUserInfoRepository, AdminUserInfoRepository>();
            services.AddScoped<IAdminAuthConfigRepository, AdminAuthConfigRepository>();
            services.AddScoped<InstallerService>();
            services.AddScoped<AdminAuthConfigService>();
            services.AddScoped<AdminMenuLocalizationService>();
            //Legacy comment translated to English.
            services.AddScoped<LoginCaptchaService>();
            services.AddScoped<IDesktopAdminAuthTokenIssuer, DesktopAdminAuthTokenIssuer>();

            //Legacy comment translated to English.
            services.AddScoped<IAdminChatSessionRepository, AdminChatSessionRepository>();
            services.AddScoped<IAdminChatMessageRepository, AdminChatMessageRepository>();
            services.AddScoped<IAdminChatSessionModuleRepository, AdminChatSessionModuleRepository>();
            services.AddScoped<IAdminChatSessionWorkflowRepository, AdminChatSessionWorkflowRepository>();
            services.AddScoped<IAdminChatTrajectoryRepository, AdminChatTrajectoryRepository>();
            services.AddScoped<IAdminChatTrajectoryEventRepository, AdminChatTrajectoryEventRepository>();
            services.AddScoped<AdminChatSessionService>();
            services.AddScoped<AdminChatMessageService>();
            services.AddScoped<AdminChatSessionModuleService>();
            services.AddScoped<AdminChatSessionWorkflowService>();
            services.AddScoped<AdminChatTrajectoryService>();
            services.AddScoped<AdminChatTrajectoryEventService>();
            services.AddScoped<AdminChatAiService>();
            services.AddSingleton<AdminChatRunRegistry>();
            services.AddSingleton<AdminChatMemoryStore>();
            services.AddScoped<IAdminChatMemoryStore>(provider => new AdminChatMemoryDatabaseStore(
                provider.GetRequiredService<Senparc.Ncf.Core.Models.INcfDbData>().BaseDataContext,
                provider.GetRequiredService<AdminChatMemoryStore>(),
                provider.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>()));
            services.AddScoped<AdminChatMemoryService>();
            services.AddScoped<AdminChatWorkspaceAssetService>();
            services.AddSingleton<WorkspaceAssetPreviewBuilder>();
            services.AddSingleton<WorkspaceAssetShareStore>();
            services.AddSingleton<WorkspaceDocumentReader>();
            services.AddScoped<AdminChatAttachmentService>();
            services.AddScoped<IWeixinClawAdminBindingRepository, WeixinClawAdminBindingRepository>();
            services.AddScoped<WeixinClawAdminBindingService>();
            services.AddScoped<AdminWeixinClawMessageHandler>();
            services.AddScoped<Senparc.Xncf.WeixinManager.WeixinClaw.IWeixinClawMessageHandler>(
                serviceProvider => serviceProvider.GetRequiredService<AdminWeixinClawMessageHandler>());

            //Legacy comment translated to English.
            services.AddScoped<INeuCharPivotConfigurationRepository, NeuCharPivotConfigurationRepository>();
            services.AddScoped<INeuCharPivotFunctionRepository, NeuCharPivotFunctionRepository>();
            services.AddScoped<INeuCharPivotLoopTaskRepository, NeuCharPivotLoopTaskRepository>();
            services.AddScoped<INeuCharExecutionLogRepository, NeuCharExecutionLogRepository>();
            services.AddScoped<INeuCharPivotBoardRepository, NeuCharPivotBoardRepository>();
            services.AddScoped<NeuCharPivotConfigurationService>();
            services.AddScoped<NeuCharPivotFunctionService>();
            services.AddScoped<NeuCharPivotLoopTaskService>();
            services.AddScoped<NeuCharExecutionLogService>();
            services.AddScoped<NeuCharPivotBoardService>();
            services.AddScoped<NeuCharFunctionService>();
            services.AddScoped<NeuCharPivotService>();
            services.AddScoped<NeuCharPivotGlobalAccessService>();
            services.AddScoped<NeuCharPivotGlobalFunctionService>();
            //Legacy comment translated to English.
            services.AddScoped<INeuCharFunctionProvitAccessRepository, NeuCharFunctionProvitAccessRepository>();
            services.AddScoped<FullNeuCharFunctionProvitAccessCache>();
            services.AddScoped<NeuCharFunctionProvitAccessService>();
            services.AddDataProtection();
            services.AddScoped<NeuCharParameterProtector>();
            services.AddScoped<ChatAgentNeuCharPivotComposer>();
            services.AddHostedService<NeuCharPivotLoopTaskHostedService>();

            //Legacy comment translated to English.
            //Legacy comment translated to English.
            services.AddSingleton<HostMetricsCollector>();

            //Legacy comment translated to English.
            //Legacy comment translated to English.
            services.AddHostedService<ResourceMonitorRecorder>();

            //Legacy comment translated to English.
            //Legacy comment translated to English.
            services.AddSingleton<NeuBellChangeNotifier>();
            services.AddSingleton<Senparc.Ncf.Shared.Abstractions.NeuBell.INeuBellPublisher>(
                serviceProvider => serviceProvider.GetRequiredService<NeuBellChangeNotifier>());
            services.AddSingleton<NeuBellTestProvider>();
            services.AddSingleton<Senparc.Ncf.Shared.Abstractions.NeuBell.INeuBellProvider>(
                serviceProvider => serviceProvider.GetRequiredService<NeuBellTestProvider>());
            services.AddScoped<INeuBellModuleAvailabilityService, NeuBellModuleAvailabilityService>();
            services.AddScoped<NeuBellProviderCatalog>();
            services.AddScoped<NeuBellSnapshotService>();
            services.AddSingleton<NeuCharPivotNeuBellProvider>();
            services.AddSingleton<Senparc.Ncf.Shared.Abstractions.NeuBell.INeuBellProvider>(
                serviceProvider => serviceProvider.GetRequiredService<NeuCharPivotNeuBellProvider>());

            //Legacy comment translated to English.
            services.AddScoped<INeuBellWebHookRepository, NeuBellWebHookRepository>();
            services.AddScoped<INeuBellWebHookService, NeuBellWebHookService>();
            services.AddScoped<NeuBellWebHookService>();
            services.AddScoped<INeuBellWebHookLogRepository, NeuBellWebHookLogRepository>();
            services.AddScoped<INeuBellWebHookLogService, NeuBellWebHookLogService>();
            services.AddScoped<NeuBellWebHookLogService>();
            services.AddHttpClient(NeuBellWebHookDispatcher.HttpClientName, client =>
                client.Timeout = TimeSpan.FromSeconds(15));
            services.AddSingleton<NeuBellWebHookDispatcher>(serviceProvider =>
                new NeuBellWebHookDispatcher(
                    serviceProvider,
                    serviceProvider.GetRequiredService<IHttpClientFactory>(),
                    serviceProvider.GetRequiredService<ILogger<NeuBellWebHookDispatcher>>(),
                    configuration["NeuBellWebHook:BaseUrl"]));
            services.AddHostedService<NeuBellWebHookMonitorService>();
            services.AddHostedService<WeixinClawNeuBellPushHostedService>();

            return base.AddXncfModule(services, configuration, env);
        }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="services"></param>
        private void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            JwtSettings backend = new JwtSettings();
            configuration.Bind(JwtSettings.Position_Backend, backend);
            services.AddAuthentication()
                .AddJwtBearer(BackendJwtAuthorizeAttribute.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                    {
                        ValidIssuer = backend.Issuer,
                        ValidAudience = backend.Audience,
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(backend.SecretKey)),
                        ValidateIssuer = true, //whether or not valid Issuer
                        ValidateAudience = true, //whether or not valid Audience
                        ValidateLifetime = true, //whether or not valid out-of-service time
                        ValidateIssuerSigningKey = true, //whether or not valid SecurityKey　　　　　　　　　　　
                        ClockSkew = System.TimeSpan.Zero//Allowed server time offset
                    };
                }).AddJwtBearer("Bearer", options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                    {
                        ValidIssuer = backend.Issuer,
                        ValidAudience = backend.Audience,
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(backend.SecretKey)),
                        ValidateIssuer = true, //whether or not valid Issuer
                        ValidateAudience = true, //whether or not valid Audience
                        ValidateLifetime = true, //whether or not valid out-of-service time
                        ValidateIssuerSigningKey = true, //whether or not valid SecurityKey　　　　　　　　　　　
                        ClockSkew = System.TimeSpan.Zero//Allowed server time offset
                    };
                })
            //.AddJwtBearer(Core.ApiAttributes.JwtAuthorizeAttribute.AuthenticationScheme, options =>
            //{
            //    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
            //    {
            //        ValidIssuer = miniPro.Issuer,
            //        ValidAudience = miniPro.Audience,
            //        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(miniPro.SecretKey)),
            //        ValidateIssuer = true, //whether or not valid Issuer
            //        ValidateAudience = true, //whether or not valid Audience
            //        ValidateLifetime = true, //whether or not valid out-of-service time
            //        ValidateIssuerSigningKey = true, //whether or not valid SecurityKey　　　　　　　　　　　
            //        ClockSkew = System.TimeSpan.Zero//Allowed server time offset
            //    };
            //})
            ;

        }
        public override IApplicationBuilder UseXncfModule(IApplicationBuilder app, IRegisterService registerService)
        {
            var files = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot");
            var environment = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
            // Version tags must hash the same embedded assets that the middleware serves.
            environment.WebRootFileProvider = new CompositeFileProvider(environment.WebRootFileProvider, files);
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = files
            });

            return base.UseXncfModule(app, registerService);
        }

        #endregion

        #region Legacy section

        public string HomeUrl => "/Admin";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>()
        {
            new AreaPageMenuItem(GetAreaUrl("/Admin/Menu/Index"), ResourceStringLocalizer.Get(typeof(AdminResource), "Admin.Area.MenuManagement", "菜单管理"),"fa fa-bug"),
            new AreaPageMenuItem(GetAreaUrl("/Admin/SenparcTrace/Index"), ResourceStringLocalizer.Get(typeof(AdminResource), "Admin.Area.TraceLog", "SenparcTrace 日志"),"fa fa-calendar-o"),
            new AreaPageMenuItem(GetAreaUrl("/Admin/WeixinClaw/Index"), "个人微信集成", "fa fa-wechat"),
        };//Legacy comment translated to English.


        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            Console.WriteLine("[IAreaRegister - Admin] AuthorizeConfig - AdminArea");

            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = AdminAuthorizeAttribute.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = AdminAuthorizeAttribute.AuthenticationScheme;
                    options.DefaultChallengeScheme = AdminAuthorizeAttribute.AuthenticationScheme;
                    options.DefaultForbidScheme = AdminAuthorizeAttribute.AuthenticationScheme;
                })
                .AddCookie(AdminAuthorizeAttribute.AuthenticationScheme, options =>
                {
                    options.AccessDeniedPath = "/Admin/Forbidden/";
                    options.LoginPath = "/Admin/Login/";
                    // The authentication cookie is never a browser-readable API
                    // value. Keep it out of document.cookie to limit XSS impact.
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    // Keep Secure cookies on HTTPS while allowing local HTTP hosts
                    // (for example Safari/WebKit on http://localhost) to persist login.
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(AdminAuthConfig.DefaultAdminWebLoginExpireMinutes);
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnCheckSlidingExpiration = context =>
                        {
                            //Legacy comment translated to English.
                            if (context.Request.Path.StartsWithSegments("/Admin/Session"))
                            {
                                context.ShouldRenew = false;
                            }
                            return Task.CompletedTask;
                        },
                        OnValidatePrincipal = async context =>
                        {
                            //Legacy comment translated to English.
                            if (context.Principal?.Identity?.IsAuthenticated != true)
                            {
                                return;
                            }

                            if (!context.Principal.HasClaim(claim =>
                                    claim.Type == NcfAuthorizationPolicyNames.AdminMemberClaim))
                            {
                                context.RejectPrincipal();
                                await context.HttpContext.SignOutAsync(AdminAuthorizeAttribute.AuthenticationScheme);
                            }
                        },
                        OnRedirectToAccessDenied = context =>
                        {
                            //Legacy comment translated to English.
                            var principal = context.HttpContext.User;
                            var isAdmin = principal?.Identity?.IsAuthenticated == true
                                && principal.HasClaim(claim =>
                                    claim.Type == NcfAuthorizationPolicyNames.AdminMemberClaim);
                            if (!isAdmin)
                            {
                                var returnUrl = $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
                                var loginPath = options.LoginPath.HasValue ? options.LoginPath.Value : "/Admin/Login/";
                                context.Response.Redirect(
                                    $"{loginPath}?ReturnUrl={Uri.EscapeDataString(returnUrl)}");
                                return Task.CompletedTask;
                            }

                            //Legacy comment translated to English.
                            if (string.Equals(
                                    context.Request.Headers.XRequestedWith,
                                    "XMLHttpRequest",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                context.Response.Headers.Location = context.RedirectUri;
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            }
                            else
                            {
                                context.Response.Redirect(context.RedirectUri);
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services
                //.AddAuthorization(options =>
                .AddAuthorizationCore(options =>
                {
                    options.AddNcfAdminAuthorizationPolicies();
                    options.AddPolicy(BackendJwtAuthorizeAttribute.SuperAdminPolicyName, policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        policy.RequireClaim(NcfAuthorizationPolicyNames.AdminMemberClaim);
                        policy.RequireAssertion(context => context.User.Claims
                            .Where(claim => claim.Type == ClaimTypes.Role)
                            .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries))
                            .Any(role => string.Equals(role.Trim(), Config.SYSROLE_ADMINISTRATOR_ROLE_CODE, StringComparison.OrdinalIgnoreCase)));
                    });
                });

            builder.AddRazorPagesOptions(options =>
            {
                //options.Conventions.AddAreaFolderApplicationModelConvention("Admin", "/", model =>
                //{
                //    model.Filters.Add(new AdminAuthorizeAttribute());
                //});

                //Legacy comment translated to English.
                //Legacy comment translated to English.
                //Legacy comment translated to English.

                //options.Conventions.AddAreaFolderRouteModelConvention("Admin","/Admin/", model =>
                //{
                //    foreach (var selector in model.Selectors)
                //    {
                //        var template = selector.AttributeRouteModel.Template;
                //        if (template.StartsWith("/"))
                //        {
                //            selector.AttributeRouteModel.Template = AttributeRouteModel.CombineTemplates(
                //                "{area:exists}",
                //                template.TrimStart('/'));
                //        }
                //    }
                //});


                options.Conventions.AuthorizePage("/", NcfAuthorizationPolicyNames.AdminOnly);//Legacy comment translated to English.
                options.Conventions.AuthorizePage("/AdminChat/Chat", NcfAuthorizationPolicyNames.AdminOnly);//Legacy comment translated to English.
                options.Conventions.AuthorizePage("/NeuCharPivot/Aggregate", NcfAuthorizationPolicyNames.AdminOnly);
                options.Conventions.AuthorizePage("/NeuCharPivot/Access", NcfAuthorizationPolicyNames.AdminOnly);
                options.Conventions.AllowAnonymousToPage("/Login");//Legacy comment translated to English.

                //Legacy comment translated to English.
            });

            SenparcTrace.SendCustomLog("系统启动", "完成 Area:Admin 注册");

            builder.Services.AddScoped<ISysMenuRepository, SysMenuRepository>();
            builder.Services.AddScoped<ISysRolePermissionRepository, SysRolePermissionRepository>();
            builder.Services.AddScoped<IAuthorizationHandler, Ncf.Core.Authorization.PermissionHandler>();

            return builder;
        }

        #endregion

        #region Legacy section

        public const string DATABASE_PREFIX = "ADMIN_";
        //Legacy comment translated to English.
        public string DatabaseUniquePrefix => DATABASE_PREFIX;

        public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);


        public void OnModelCreating(ModelBuilder modelBuilder)
        {

        }

        public void AddXncfDatabaseModule(IServiceCollection services)
        {
            //services.AddScoped<AdminUserInfo>();
        }

        //public string DatabaseUniquePrefix => "NcfSystemAdmin_";
        //public Type XncfDatabaseDbContextType => typeof(SenparcEntities);


        //public void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //Legacy comment translated to English.

        //}

        //public void AddXncfDatabaseModule(IServiceCollection services)
        //{
        //    #region Legacy section
        //Legacy comment translated to English.
        //     *      errorinformation：
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //     *                InvalidOperationException: An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure()' to the 'UseSqlServer' call.
        //Legacy comment translated to English.
        //     */

        //Legacy comment translated to English.
        //     *      errorinformation：
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        //     *                InvalidOperationException: An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure()' to the 'UseSqlServer' call.
        //Legacy comment translated to English.
        //     */
        //    #endregion

        //    Func<IServiceProvider, SenparcEntities> implementationFactory = s =>
        //        new SenparcEntities(new DbContextOptionsBuilder<SenparcEntities>()
        //            .UseSqlServer(Ncf.Core.Config.SenparcDatabaseConfigs.ClientConnectionString,
        //                            b => base.DbContextOptionsAction(b, "Senparc.Web"))
        //            .Options);

        //    services.AddScoped(implementationFactory);
        //    services.AddScoped<ISenparcEntities>(implementationFactory);
        //    services.AddScoped<SenparcEntitiesBase>(implementationFactory);

        //    services.AddScoped(typeof(INcfClientDbData), typeof(NcfClientDbData));
        //    services.AddScoped(typeof(INcfDbData), typeof(NcfClientDbData));

        //    //Attributes
        //    services.AddScoped(typeof(AuthenticationResultFilterAttribute));
        //    services.AddScoped(typeof(AuthenticationAsyncPageFilterAttribute));

        //Legacy comment translated to English.
        //    EntitySetKeys.TryLoadSetInfo(typeof(SenparcEntities));

        //    //AutoMap mapping
        //    base.AddAutoMapMapping(profile =>
        //    {
        //        profile.CreateMap<AdminUserInfo, CreateOrUpdate_AdminUserInfoDto>();
        //    });
        //}


        #endregion

        //#region IXncfRazorRuntimeCompilation interface
        //public string LibraryPath => Path.GetFullPath(Path.Combine(SiteConfig.WebRootPath, "..", "..", "Senparc.Areas.Admin"));
        //#endregion
        public override void OnAutoMapMapping(IServiceCollection services, IConfiguration configuration)
        {
            base.OnAutoMapMapping(services, configuration);
            try
            {
                services.AddAutoMapper(z => z.AddProfile<AutoMpperProfiles.SenparcAreaAdminAutoMapperProfile>());
            }
            catch (Exception ex)
            {
                //Legacy comment translated to English.
                _ = new NcfExceptionBase(ex.Message, ex);
            }

        }
    }

}

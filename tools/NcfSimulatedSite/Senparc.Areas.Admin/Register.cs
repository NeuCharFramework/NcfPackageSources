/* 
 *Special note:
 * The current registration class is a special underlying system support module.
 * A series of special processing codes are added, which are not suitable for all modules.
 * If you need to learn extension modules, please refer to the Register.cs file of the [Senparc.ExtensionAreaTemplate] project!
 */

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Senparc.Areas.Admin.ACL;
using Senparc.Areas.Admin.ACL.Repository;
using Senparc.Areas.Admin.Domain;
using Senparc.Areas.Admin.Domain.Dto;
//using Senparc.Areas.Admin.Authorization;
using Senparc.Areas.Admin.Domain.Models;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.CO2NET.RegisterServices;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core.Areas;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin
{
    [XncfRegister]
    [XncfOrder(5996)]
    public class Register : XncfRegisterBase,
        IXncfRegister, //Register XNCF basic module interface (required)
        IAreaRegister, //Register XNCF page interface (optional on demand)
        IXncfDatabase  //Register the XNCF module database (optional)
                       //IXncfRazorRuntimeCompilation //Need to use RazorRuntimeCompilation to update the Razor Page in real time in the development environment
    {

        #region IXncfRegister 接口

        public override string Name => "NeuCharFramework.Admin";

        public override string Uid => SiteConfig.SYSTEM_XNCF_MODULE_AREAS_ADMIN_UID;// "00000000-0000-0001-0001-000000000001";

        public override string Version => "0.5.6-beta4";

        public override string MenuName => "NCF 系统管理员后台";

        public override string Icon => "fa fa-university";

        public override string Description => "这是管理员后台模块，用于 NCF 系统后台的自我管理，请勿删除此模块。如果你实在忍不住，请务必做好数据备份。";


        public override async Task InstallOrUpdateAsync(IServiceProvider serviceProvider, InstallOrUpdate installOrUpdate)
        {
            //Update database
            await XncfDatabaseDbContext.MigrateOnInstallAsync(serviceProvider, this);

            //XncfModuleServiceExtension xncfModuleServiceExtension = serviceProvider.GetService<XncfModuleServiceExtension>();
            //var adminModule = xncfModuleServiceExtension.GetObject(z => z.Uid == this.Uid);
            //if (adminModule == null)
            //{
            //    //Only install if it is not installed. InstallModuleAsync will access this method. Failure to make a judgment may cause an infinite loop.
            //    //Do not automatically install modules in this method in regular modules!
            //    await xncfModuleServiceExtension.InstallModuleAsync(this.Uid).ConfigureAwait(false);
            //}

            await base.InstallOrUpdateAsync(serviceProvider, installOrUpdate);
        }

        public override async Task UninstallAsync(IServiceProvider serviceProvider, Func<Task> unsinstallFunc)
        {
            //TODO: You can provide a BeforeUninstall method to prevent uninstallation.

            #region 删除数据库（演示）

            var mySenparcEntitiesType = this.TryGetXncfDatabaseDbContextType;
            AdminSenparcEntities mySenparcEntities = serviceProvider.GetService(mySenparcEntitiesType) as AdminSenparcEntities;

            //Specify the data entity to be deleted

            //Note: As a demonstration, all tables created by this module are deleted when uninstalling the module. During actual operation, please operate with caution and sort the entities in the order of deletion!
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

            services.Configure<JwtSettings>(JwtSettings.Position_Backend, configuration.GetSection(JwtSettings.Position_Backend));// Configure management background jwt

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
            services.AddScoped<InstallerService>();

            // Chat function related service registration
            services.AddScoped<IAdminChatSessionRepository, AdminChatSessionRepository>();
            services.AddScoped<IAdminChatMessageRepository, AdminChatMessageRepository>();
            services.AddScoped<IAdminChatSessionModuleRepository, AdminChatSessionModuleRepository>();
            services.AddScoped<AdminChatSessionService>();
            services.AddScoped<AdminChatMessageService>();
            services.AddScoped<AdminChatSessionModuleService>();
            services.AddScoped<AdminChatAiService>();

            return base.AddXncfModule(services, configuration, env);
        }


        /// <summary>
        /// Add front-end and back-end authentication
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
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "wwwroot")
            });

            return base.UseXncfModule(app, registerService);
        }

        #endregion

        #region IAreaRegister 接口

        public string HomeUrl => "/Admin";

        public List<AreaPageMenuItem> AreaPageMenuItems => new List<AreaPageMenuItem>()
        {
            new AreaPageMenuItem(GetAreaUrl("/Admin/Menu/Index"),"菜单管理","fa fa-bug"),
            new AreaPageMenuItem(GetAreaUrl("/Admin/SenparcTrace/Index"),"SenparcTrace 日志","fa fa-calendar-o"),
        };//Admin is special and does not need to output all


        public IMvcBuilder AuthorizeConfig(IMvcBuilder builder, IHostEnvironment env)
        {
            Console.WriteLine("[IAreaRegister - Admin] AuthorizeConfig - AdminArea");

            //Authentication configuration
            //Add cookie-based permission verification: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-2.1&tabs=aspnetcore2x
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(AdminAuthorizeAttribute.AuthenticationScheme, options =>
                {
                    options.AccessDeniedPath = "/Admin/Forbidden/";
                    options.LoginPath = "/Admin/Login/";
                    options.Cookie.HttpOnly = false;
                });

            builder.Services
                //.AddAuthorization(options =>
                .AddAuthorizationCore(options =>
                {
                    options.AddPolicy("AdminOnly", policy =>
                    {
                        policy.RequireClaim("AdminMember");
                    });
                });

            builder.AddRazorPagesOptions(options =>
            {
                //options.Conventions.AddAreaFolderApplicationModelConvention("Admin", "/", model =>
                //{
                //    model.Filters.Add(new AdminAuthorizeAttribute());
                //});

                //options.Conventions.AuthorizeAreaFolder("Admin", "/", "AdminOnly");//Must be logged in
                //options.Conventions.AddAreaPageRoute("Admin", "/Login", "/Admin/Login");//Allow anonymity
                //options.Conventions.AddAreaPageRoute("Admin", "/Index", "/Admin/Index");//Allow anonymity

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


                options.Conventions.AuthorizePage("/", "AdminOnly");//Must log in
                options.Conventions.AuthorizePage("/AdminChat/Chat", "AdminOnly");//You must log in to the chat page
                options.Conventions.AllowAnonymousToPage("/Login");//Allow anonymity

                //More: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/razor-pages-authorization?view=aspnetcore-8.0
            });

            SenparcTrace.SendCustomLog("系统启动", "完成 Area:Admin 注册");

            builder.Services.AddScoped<ISysMenuRepository, SysMenuRepository>();
            builder.Services.AddScoped<ISysRolePermissionRepository, SysRolePermissionRepository>();
            builder.Services.AddScoped<IAuthorizationHandler, Ncf.Core.Authorization.PermissionHandler>();

            return builder;
        }

        #endregion

        #region IXncfDatabase 接口

        public const string DATABASE_PREFIX = "ADMIN_";
        //NcfDatabaseMigrationHelper.SYSTEM_UNIQUE_PREFIX;//System table, will be left blank
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
        //    //Done in SenparcEntities

        //}

        //public void AddXncfDatabaseModule(IServiceCollection services)
        //{
        //    #region Historical Solution Reference Information
        //    /* Reference information
        //     *      error message:
        //     * Chinese: EnableRetryOnFailure solves temporary database connection failure
        //     * English: Win32Exception: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond
        //     *                InvalidOperationException: An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure()' to the 'UseSqlServer' call.
        //     * Problem solution description: https://www.colabug.com/2329124.html
        //     */

        //    /* Reference information
        //     *      error message:
        //     * Chinese: EnableRetryOnFailure solves temporary database connection failure
        //     * English: Win32Exception: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond
        //     *                InvalidOperationException: An exception has been raised that is likely due to a transient failure. Consider enabling transient error resiliency by adding 'EnableRetryOnFailure()' to the 'UseSqlServer' call.
        //     * Problem solution description: https://www.colabug.com/2329124.html
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

        //    //Preload EntitySetKey
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
                //TODO: Oracle has not been upgraded to .NET 8.0, an error will be thrown here
                _ = new NcfExceptionBase(ex.Message, ex);
            }

        }
    }

}








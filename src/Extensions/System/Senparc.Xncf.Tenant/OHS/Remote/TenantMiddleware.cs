using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.Config;
using Senparc.Xncf.Tenant.Domain.Services;
using System;
using System.Threading.Tasks;

namespace Senparc.Xncf.Tenant.OHS.Remote
{
    /// <summary>
    ///Multi-tenant middleware
    /// </summary>
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;
        private static bool AlertedTenantState = false;
        public static bool FirstRunAndInstalling = false;

        static TenantMiddleware()
        {
            FirstRunAndInstalling = !SiteConfig.CheckInstallFinishedFileExisted();
        }

        private string SetLog(string msg)
        {
            return $"[{SystemTime.Now}] {msg}";
        }

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                //Determine whether you need to use a database
                if (!SiteConfig.DatabaseXncfLoaded)
                {
                    //No database, skip
                    await _next(context);
                }

                var enableMultiTenant = SiteConfig.SenparcCoreSetting.EnableMultiTenant;

                if (!AlertedTenantState)
                {
                    await Console.Out.WriteLineAsync(SetLog($"自检完成，多租户引擎激活"));
                    await Console.Out.WriteLineAsync(SetLog($"当前多租户状态：{(enableMultiTenant ? "开启" : "关闭")}  /  租户识别规则：{SiteConfig.SenparcCoreSetting.TenantRule}"));
                    AlertedTenantState = true;
                }

                //Determine whether the tenant is enabled
                var urlPath = context.Request.Path.ToString();
                if (!FirstRunAndInstalling && enableMultiTenant)
                {
                    var serviceProvider = context.RequestServices;
                    var tenantInfoService = serviceProvider.GetRequiredService<TenantInfoService>();
                    var requestTenantInfo = await tenantInfoService.SetScopedRequestTenantInfoAsync(context);//Set the RequestTenantInfo parameter of the current Request
                }
            }
            catch (NcfUninstallException unInstallEx)
            {
                //await Console.Out.WriteLineAsync(unInstallEx.ToString());
                //await Console.Out.WriteLineAsync(unInstallEx.StackTrace?.ToString());
                Console.WriteLine($"\t NcfUninstallException from TenantMiddleware - {unInstallEx.Message}");
                context.Items["NcfUninstallException"] = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("\t Exception from TenantMiddleware");

                //If there is an error in the database
                SenparcTrace.BaseExceptionLog(ex);
                throw;
            }
            //Console.WriteLine($"\tTenantMiddleware requestTenantInfo({requestTenantInfo.GetHashCode()})：" + requestTenantInfo.ToJson());

            await _next(context);
            //Console.WriteLine("TenantMiddleware finished");
        }
    }
}

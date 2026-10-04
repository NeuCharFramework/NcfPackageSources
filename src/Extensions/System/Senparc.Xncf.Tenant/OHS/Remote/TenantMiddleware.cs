/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TenantMiddleware.cs
    文件功能描述：TenantMiddleware 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Xncf.Tenant.Domain.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Senparc.Xncf.Tenant.OHS.Remote
{
    /// <summary>
    /// 多租户中间件
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
                //判断是否需要使用数据库
                if (!SiteConfig.DatabaseXncfLoaded)
                {
                    //没有数据库，跳过
                    await _next(context);
                    return;
                }

                var enableMultiTenant = SiteConfig.SenparcCoreSetting.EnableMultiTenant;

                if (!AlertedTenantState)
                {
                    await Console.Out.WriteLineAsync(SetLog($"自检完成，多租户引擎激活"));
                    await Console.Out.WriteLineAsync(SetLog($"当前多租户状态：{(enableMultiTenant ? "开启" : "关闭")}  /  租户识别规则：{SiteConfig.SenparcCoreSetting.TenantRule}"));
                    AlertedTenantState = true;
                }

                //判断是否启用了租户
                var urlPath = context.Request.Path.ToString();
                if (!FirstRunAndInstalling && enableMultiTenant)
                {
                    if (SiteConfig.SenparcCoreSetting.TenantRule == TenantRule.RequestHeader)
                    {
                        var hasTenantHeader = context.Request.Headers.TryGetValue("TenantKey", out var tenantKeys);
                        // This middleware runs before UseAuthentication in the host.
                        var schemeProvider = context.RequestServices.GetService<IAuthenticationSchemeProvider>();
                        var adminScheme = schemeProvider == null
                            ? null
                            : await schemeProvider.GetSchemeAsync(SiteConfig.NcfAdminAuthorizeScheme);
                        var cookie = adminScheme == null
                            ? null
                            : await context.AuthenticateAsync(SiteConfig.NcfAdminAuthorizeScheme);
                        if (hasTenantHeader
                            && !TenantHeaderAccessPolicy.IsHeaderAllowed(
                                tenantKeys.ToArray(),
                                context.Request.Headers.ContainsKey("Authorization"),
                                cookie?.Succeeded == true ? cookie.Principal : null,
                                context.User))
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return;
                        }

                        if (cookie?.Succeeded == true)
                        {
                            if (cookie.Principal.FindAll("TenantKey").Skip(1).Any())
                            {
                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                return;
                            }

                            context.User = cookie.Principal;
                        }
                    }

                    var serviceProvider = context.RequestServices;
                    var tenantInfoService = serviceProvider.GetRequiredService<TenantInfoService>();
                    var requestTenantInfo = await tenantInfoService.SetScopedRequestTenantInfoAsync(context);//设置当前 Request 的 RequestTenantInfo 参数
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

                //如果数据库出错
                SenparcTrace.BaseExceptionLog(ex);
                throw;
            }
            //Console.WriteLine($"\tTenantMiddleware requestTenantInfo({requestTenantInfo.GetHashCode()})：" + requestTenantInfo.ToJson());

            await _next(context);
            //Console.WriteLine("TenantMiddleware finished");
        }
    }

    public static class TenantHeaderAccessPolicy
    {
        public static bool IsHeaderAllowed(
            string[] tenantKeys,
            bool hasAuthorizationHeader,
            ClaimsPrincipal cookiePrincipal,
            ClaimsPrincipal requestPrincipal)
        {
            return tenantKeys?.Length == 1
                && !string.IsNullOrWhiteSpace(tenantKeys[0])
                && !hasAuthorizationHeader
                && (cookiePrincipal == null || HasTenantAccess(cookiePrincipal, tenantKeys[0]))
                && (requestPrincipal?.Identity?.IsAuthenticated != true
                    || HasTenantAccess(requestPrincipal, tenantKeys[0]));
        }

        public static bool HasTenantAccess(ClaimsPrincipal principal, string tenantKey)
        {
            var tenantClaims = principal?.FindAll("TenantKey").ToArray();
            return principal?.Identity?.IsAuthenticated == true
                && tenantClaims?.Length == 1
                && !string.IsNullOrWhiteSpace(tenantKey)
                && string.Equals(tenantClaims[0].Value, tenantKey, StringComparison.OrdinalIgnoreCase);
        }
    }
}

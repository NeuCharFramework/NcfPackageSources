using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

using Senparc.CO2NET.WebApi;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Xncf.Swagger.Models;
using Senparc.Xncf.Swagger.Utils;

using Swashbuckle.AspNetCore.SwaggerUI;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;

namespace Senparc.Xncf.Swagger.Builder
{
    public static class SwaggerBuilderExtensions
    {
        public static IApplicationBuilder UseSwaggerCustom(this IApplicationBuilder app)
        {
            var options = ConfigurationHelper.CustsomSwaggerOptions;
            if (!options.Enabled)
            {
                return app;
            }
            app
            //.UseSwaggerCustomAuth(options)
            .UseSwagger(opt =>
            {
                opt.RouteTemplate = $"/{options.RoutePrefix}/{{documentName}}/swagger.json";
                if (options.UseSwaggerAction == null) return;
                options.UseSwaggerAction(opt);
            })
            .UseSwaggerUI(c =>
            {
                #region WebApiEngine

                //c.DocumentTitle = "Senparc Weixin SDK Demo API";
                c.InjectJavascript("/lib/jquery/dist/jquery.min.js");
                c.InjectJavascript("/js/swagger.js");
                //c.InjectJavascript("/js/tongji.js");
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);

                var sortedAssembly = WebApiEngine.ApiAssemblyCollection.OrderBy(z => z.Key);
                foreach (var co2netApiDocAssembly in sortedAssembly)
                {
                    //TODO: real dynamic version number
                    var verion = WebApiEngine.ApiAssemblyVersions[co2netApiDocAssembly.Key]; //neucharApiDocAssembly.Value.ImageRuntimeVersion;
                    var docName = WebApiEngine.GetDocName(co2netApiDocAssembly.Key);

                    //Console.WriteLine($"\tAdd {docName}");

                    c.SwaggerEndpoint($"/swagger/{docName}/swagger.json", $"{co2netApiDocAssembly.Key}");
                }

                #endregion

                c.RoutePrefix = options.RoutePrefix;
                c.DocumentTitle = options.ProjectName;
                if (options.UseCustomIndex)
                {
                    c.UseCustomSwaggerIndex();
                }
                if (options.CustomAuthList?.Count > 0)
                {
                    //c.ConfigObject["customAuth"] = true;
                    //c.ConfigObject["loginUrl"] = $"/{options.RoutePrefix}/login.html";
                    //c.ConfigObject["logoutUrl"] = $"/{options.RoutePrefix}/logout";
                }
                if (options.ApiVersions == null) options.ApiVersions = new List<string> { "v1" };
                foreach (var item in options.ApiVersions)
                {
                    var subPath = string.IsNullOrEmpty(options.AppPath) ? "" : $"/{options.AppPath}";//Used when publishing as a virtual site
                    c.SwaggerEndpoint($"{subPath}/{options.RoutePrefix}/{item}/swagger.json", $"{item}");
                }
                options.UseSwaggerUIAction?.Invoke(c);
            });
            return app;
        }
        /// <summary>
        /// Handle user authentication of interface documents
        /// </summary>
        /// <param name="app"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private static IApplicationBuilder UseSwaggerCustomAuth(this IApplicationBuilder app, CustsomSwaggerOptions options)
        {
            if (options.AllowAnonymous)
                return app;
            var currentAssembly = typeof(CustsomSwaggerOptions).GetTypeInfo().Assembly;
            app.Use(async (context, next) =>
            {
                var _method = context.Request.Method.ToLower();
                var _path = context.Request.Path.Value;
                var subPath = string.IsNullOrEmpty(options.AppPath) ? "" : $"/{options.AppPath}";//Used when publishing as a virtual site
                #region 自定义登录页
                if (_path.IndexOf($"/{options.RoutePrefix}") != 0)//Return directly when not accessing the interface
                {
                    await next();
                    return;
                }
                else if (_path == $"/{options.RoutePrefix}/login.html")
                {
                    //Log in
                    if (_method == "get")
                    {
                        var stream = currentAssembly.GetManifestResourceStream($"{currentAssembly.GetName().Name}.login.html");
                        byte[] buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);
                        context.Response.ContentType = "text/html;charset=utf-8";
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        await context.Response.Body.WriteAsync(buffer, 0, buffer.Length);
                        return;
                    }
                    else if (_method == "post")
                    {
                        var userModel = new CustomSwaggerAuth(context.Request.Form["userName"], context.Request.Form["userPwd"]);
                        if (!options.CustomAuthList.Any(e => e.UserName == userModel.UserName && e.UserPwd == userModel.UserPwd))
                        {
                            await context.Response.WriteAsync("login error!");
                            return;
                        }
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name,userModel.UserName)
                        };
                        var identity = new ClaimsIdentity(ConfigurationHelper.SWAGGER_ATUH_COOKIE);
                        identity.AddClaims(claims);
                        var authProperties = new AuthenticationProperties
                        {
                            AllowRefresh = false,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(120),
                            IsPersistent = false,
                        };
                        await context.SignOutAsync(ConfigurationHelper.SWAGGER_ATUH_COOKIE);//Sign out
                        await context.SignInAsync(ConfigurationHelper.SWAGGER_ATUH_COOKIE, new ClaimsPrincipal(identity), authProperties);

                        context.Response.Redirect($"{subPath}/{options.RoutePrefix}");
                        return;
                    }
                }
                else if (_path == $"/{options.RoutePrefix}/logout")
                {
                    //quit
                    context.Response.Cookies.Delete(ConfigurationHelper.SWAGGER_ATUH_COOKIE);
                    context.Response.Redirect($"{subPath}/{options.RoutePrefix}/login.html");
                    return;
                }
                #endregion
                else
                {
                    if (ConfigurationHelper.CustsomSwaggerOptions.UseAdminAuth) {
                        var authentcationResult = await context.AuthenticateAsync(AdminAuthorizeAttribute.AuthenticationScheme);
                        if (!authentcationResult.Succeeded)
                        {
                            context.Response.Redirect("/Admin/Login/");
                            return;
                        }
                    }
                    else
                    {
                        var authentcationResult = await context.AuthenticateAsync(ConfigurationHelper.SWAGGER_ATUH_COOKIE);
                        if (!authentcationResult.Succeeded)
                        {
                            context.Response.Redirect($"{subPath}/{options.RoutePrefix}/login.html");
                            return;
                        }
                    }
                }
                await next();
            });
            return app;
        }
        /// <summary>
        /// Use custom homepage
        /// </summary>
        /// <returns></returns>
        private static void UseCustomSwaggerIndex(this SwaggerUIOptions c)
        {
            var currentAssembly = typeof(CustsomSwaggerOptions).GetTypeInfo().Assembly;
            c.IndexStream = () => currentAssembly.GetManifestResourceStream($"{currentAssembly.GetName().Name}.index.html");
        }
    }
}

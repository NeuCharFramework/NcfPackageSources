using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Senparc.CO2NET.WebApi;
using Senparc.Xncf.Swagger.Filters;
using Senparc.Xncf.Swagger.Models;
using Senparc.Xncf.Swagger.Utils;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Senparc.Xncf.Swagger.Builder
{
    public static class CustomSwaggerServiceCollectionExtensions
    {
        public static IServiceCollection AddSwaggerCustom(this IServiceCollection services, string docXmlPath)
        {
            var options = ConfigurationHelper.CustsomSwaggerOptions;
            services.AddSwaggerGen(c =>
            {
                #region WebApiEngine

                //为每个程序集创建文档
                foreach (var apiAssembly in WebApiEngine.ApiAssemblyCollection)
                {
                    var version = WebApiEngine.ApiAssemblyVersions[apiAssembly.Key]; //neucharApiDocAssembly.Value.ImageRuntimeVersion;
                    var docName = WebApiEngine.GetDocName(apiAssembly.Key);
                    c.SwaggerDoc(docName, new OpenApiInfo
                    {
                        Title = $"CO2NET Dynamic WebApi Engine : {apiAssembly.Key}",
                        Version = $"v{version}",//"v16.5.4"
                        Description = $"Senparc CO2NET WebApi 动态引擎（{apiAssembly.Key} - v{version}）",
                        //License = new OpenApiLicense()
                        //{
                        //    Name = "Apache License Version 2.0",
                        //    Url = new Uri("https://github.com/NeuCharFramework")
                        //},
                        Contact = new OpenApiContact()
                        {
                            Email = "zsu@senparc.com",
                            Name = "NeuCharFramework Team",
                            Url = new Uri("https://github.com/NeuCharFramework")
                        },
                        //TermsOfService = new Uri("https://github.com/NeuCharFramework")
                    });

                    //c.DocumentFilter<TagDescriptionsDocumentFilter>();
                    var docXmlFile = Path.Combine(WebApiEngine.GetDynamicFilePath(docXmlPath), $"{WebApiEngine.ApiAssemblyNames[apiAssembly.Key]}.xml");
                    if (File.Exists(docXmlFile))
                    {
                        c.IncludeXmlComments(docXmlFile);
                    }
                }


                ////分组显示  https://www.cnblogs.com/toiv/archive/2018/07/28/9379249.html
                //c.DocInclusionPredicate((docName, apiDesc) =>
                //{
                //    if (!apiDesc.TryGetMethodInfo(out MethodInfo methodInfo))
                //    {
                //        return false;
                //    }

                //    var versions = methodInfo.DeclaringType
                //          .GetCustomAttributes(true)
                //          .OfType<SwaggerOperationAttribute>()
                //          .Select(z => z.Tags[0].Split(':')[0]);

                //    if (versions.FirstOrDefault() == null)
                //    {
                //        return false;//不符合要求的都不显示
                //    }

                //    //docName: $"{neucharApiDocAssembly.Key}-v1"
                //    return versions.Any(z => docName.StartsWith(z));
                //});

                c.OrderActionsBy(z => z.RelativePath);
                c.EnableAnnotations();
                //c.DocumentFilter<RemoveVerbsFilter>();
                c.CustomSchemaIds(x => x.FullName);//规避错误：InvalidOperationException: Can't use schemaId "$JsApiTicketResult" for type "$Senparc.Weixin.Open.Entities.JsApiTicketResult". The same schemaId was already used for type "$Senparc.Weixin.MP.Entities.JsApiTicketResult"

                /* 需要登陆，暂不考虑    —— Jeffrey Su 2021.06.17
                var oAuthDocName = "oauth2";// WeixinApiService.GetDocName(PlatformType.WeChat_OfficialAccount);

                //添加授权
                var authorizationUrl = NeuChar.App.AppStore.Config.IsDebug
                                               //以下是 appPurachase 的 Id，实际应该是 appId
                                               //? "http://localhost:12222/App/LoginOAuth/Authorize/1002/"
                                               //: "https://www.neuchar.com/App/LoginOAuth/Authorize/4664/";
                                               //以下是正确的 appId
                                               ? "http://localhost:12222/App/LoginOAuth/Authorize?appId=xxx"
                                               : "https://www.neuchar.com/App/LoginOAuth/Authorize?appId=3035";

                c.AddSecurityDefinition(oAuthDocName,//"Bearer" 
                    new OpenApiSecurityScheme
                    {
                        Description = "请输入带有Bearer开头的Token",
                        Name = oAuthDocName,// "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.OAuth2,
                        //OpenIdConnectUrl = new Uri("https://www.neuchar.com/"),
                        Flows = new OpenApiOAuthFlows()
                        {
                            Implicit = new OpenApiOAuthFlow()
                            {
                                AuthorizationUrl = new Uri(authorizationUrl),
                                Scopes = new Dictionary<string, string> { { "swagger_api", "Demo API - full access" } }
                            }
                        }
                    });

                //认证方式，此方式为全局添加
                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    { new OpenApiSecurityScheme(){ Name =oAuthDocName//"Bearer"
                    }, new List<string>() }
                    //{ "Bearer", Enumerable.Empty<string>() }
                });

                //c.OperationFilter<AuthResponsesOperationFilter>();//AuthorizeAttribute过滤

                */


                #endregion

                c.OperationFilter<SwaggerFileUploadFilter>();
                options.AddSwaggerGenAction?.Invoke(c);

                if (options.ApiVersions == null)
                {
                    return;
                }

                //foreach (var version in options.ApiVersions)
                //{
                //    c.SwaggerDoc(version, new OpenApiInfo { Title = options.ProjectName, Version = version });
                //}

                //分组显示  https://www.cnblogs.com/toiv/archive/2018/07/28/9379249.html
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    if (!apiDesc.TryGetMethodInfo(out MethodInfo methodInfo))
                    {
                        return false;
                    }

                    //获取方法上的特性
                    var versions = methodInfo.GetCustomAttributes(true)
                                              .OfType<SwaggerOperationAttribute>()
                                              .Select(z => z.Tags[0].Split(':')[0]);

                    //获取类上的特性
                    if (versions?.Count() == 0)
                    {
                        versions = methodInfo.DeclaringType.GetCustomAttributes(true)
                        .OfType<SwaggerOperationAttribute>()
                          .Select(z => z.Tags[0].Split(':')[0]);
                    }

                    if (versions?.Count() == 0)
                    {
                        return false;//不符合要求的都不显示
                    }


                    //docName: $"{neucharApiDocAssembly.Key}-v1"
                    return versions.Any(z => docName.StartsWith(z));
                });



                //分组  -- 当前分组策略无效，会导致所有 API 都不显示，暂时停用   —— Jeffrey Su 2021.7.22
                //c.TagActionsBy(s =>
                //{
                //    var controller = s.ActionDescriptor.RouteValues["controller"];
                //    if (s.ActionDescriptor.RouteValues.ContainsKey("area"))
                //    {
                //        var area = s.ActionDescriptor.RouteValues["area"];
                //        if (area != null)
                //            return new string[] { $"{area}_{controller}" };
                //    }
                //    return new string[] { controller };
                //});
                ////版本
                //c.DocInclusionPredicate((docName, apiDesc) =>
                //{
                //    if (!apiDesc.TryGetMethodInfo(out MethodInfo methodInfo)) return false;

                //    var methodVersions = methodInfo
                //        .GetCustomAttributes(true)
                //        .OfType<ApiVersionAttribute>()
                //        .SelectMany(attr => attr.Versions);
                //    var allow = methodVersions.Any(v => $"v{v.MajorVersion}.{v.MinorVersion}".Trim('.') == docName);
                //    if (allow) return true;

                //    var declaringTypeVersions = methodInfo.DeclaringType
                //        .GetCustomAttributes(true)
                //        .OfType<ApiVersionAttribute>()
                //        .SelectMany(attr => attr.Versions);

                //    return declaringTypeVersions.Any(v => $"v{v.MajorVersion}.{v.MinorVersion}".Trim('.') == docName);
                //});

                //c.OperationFilter<SwaggerDefaultValueFilter>();

            });
            return services;
        }
    }
}

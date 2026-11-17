/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: Register.Middleware.cs

    File description: Register.Middleware related implementation

    

    

    Created by: Senparc - 20250712

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



    Modified by: Senparc - 20260731

    Change description: v0.24.0-preview5 Updated module behavior and related support.



----------------------------------------------------------------*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.XncfBase;
using Senparc.NeuChar.Context;
using Senparc.NeuChar.Entities;
using Senparc.NeuChar.MessageHandlers;
using Senparc.NeuChar.Middlewares;
using Senparc.Weixin.Entities;
using Senparc.Weixin.Exceptions;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.WeixinManager.Domain.Services;

namespace Senparc.Xncf.WeixinManager
{
    public partial class Register : IXncfMiddleware  //Legacy comment translated to English.
    {
        public static ConcurrentDictionary<string, Type> MpMessageHandlerNames = new ConcurrentDictionary<string, Type>();

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="TMC"></typeparam>
        /// <param name="mpAccountDtoFunc"></param>
        /// <param name="mpMessageHandlerType"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static Func<Stream, PostModel, int, IServiceProvider, MessageHandler<TMC, IRequestMessageBase, IResponseMessageBase>>
            BuildMessageHandlerFunc<TMC>(Func<IServiceProvider, MpAccountDto> mpAccountDtoFunc, Type mpMessageHandlerType)
                where TMC : class, IMessageContext<IRequestMessageBase, IResponseMessageBase>, new()
        {
            Func<Stream, PostModel, int, IServiceProvider, MessageHandler<TMC, IRequestMessageBase, IResponseMessageBase>> 
            messageHandlerFunc = (stream, postModel, maxRecordCount, services) =>
            {
                try
                {
                    using (var middlewareScope = services.CreateScope())
                    {
                        var mpAccountDto = mpAccountDtoFunc(middlewareScope.ServiceProvider);

                        var senparcWeixinSetting = new SenparcWeixinSetting();
                        senparcWeixinSetting.WeixinAppId = mpAccountDto.AppId;
                        senparcWeixinSetting.WeixinAppSecret = mpAccountDto.AppSecret;
                        senparcWeixinSetting.Token = mpAccountDto.Token;
                        senparcWeixinSetting.EncodingAESKey = mpAccountDto.EncodingAESKey;

                        //Legacy comment translated to English.
                        Senparc.Weixin.Config.SenparcWeixinSetting[$"DynamicMP-{mpAccountDto.Id}"] = senparcWeixinSetting;

                        //Legacy comment translated to English.
                        var messageHandler = Activator.CreateInstance(mpMessageHandlerType, new object[] { mpAccountDto, stream, postModel, maxRecordCount, services });

                        return messageHandler as MessageHandler<TMC, IRequestMessageBase, IResponseMessageBase>;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(WeixinManagerResource.Format(
                        "Error.MessageHandlerConstructor",
                        "{0} must provide a constructor with the following parameter order: (MpAccountDto mpAccountDto, Stream inputStream, PostModel postModel, int maxRecordCount, IServiceProvider serviceProvider)",
                        mpMessageHandlerType.FullName), ex);
                }
            };
            return messageHandlerFunc;
        }

        #region Legacy section

        public IApplicationBuilder UseMiddleware(IApplicationBuilder app)
        {
            try
            {
                //using (var scope = app.ApplicationServices.CreateScope())

                var messageHandlerTypes = AppDomain.CurrentDomain.GetAssemblies()
                                    .SelectMany(a =>
                                    {
                                        try
                                        {
                                            var aTypes = a.GetTypes();
                                            return aTypes.Where(t =>
                                            {
                                                var attr = t.GetCustomAttributes(false).FirstOrDefault(z => z is MpMessageHandlerAttribute) as MpMessageHandlerAttribute;
                                                if (attr != null)
                                                {
                                                    MpMessageHandlerNames[attr.Name] = t;
                                                    return true;
                                                }
                                                return false;
                                            }
                                                 //!t.IsAbstract &&
                                                 //t is IMessageHandler &&
                                                 );
                                        }
                                        catch
                                        {
                                            return new List<Type>();
                                        }
                                    });

                SenparcTrace.SendCustomLog("Senparc.Xncf.WeixinManager 搜索 MessageHandler", $"搜索到 {messageHandlerTypes.Count()} 个");

                if (messageHandlerTypes == null || messageHandlerTypes.Count() == 0)
                {
                    return app;//Legacy comment translated to English.
                }

                Func<IServiceProvider, MpAccountDto> mpAccountDtoFunc = serviceProvider =>
                {
                    var httpCotextAssessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                    var httpContext = httpCotextAssessor.HttpContext;
                    var param = httpContext.Request.Query["parameter"].ToString();

                    if (!int.TryParse(param, out int mpAccountId))
                    {
                        throw new WeixinException(WeixinManagerResource.Get("Error.InvalidId", "Invalid ID."));
                    }

                    var mpAccountService = serviceProvider.GetRequiredService<MpAccountService>();
                    var mpAccountDto = mpAccountService.GetMpAccount(mpAccountId);

                    return mpAccountDto;
                };

                foreach (var mpMessageHandlerNamePair in MpMessageHandlerNames)
                {
                    try
                    {
                        var mpMessageHandlerName = mpMessageHandlerNamePair.Key;
                        var mpMessageHandlerType = mpMessageHandlerNamePair.Value;

                        //Legacy comment translated to English.
                        var messageContextGenericType = mpMessageHandlerType.BaseType.GetGenericArguments().First();
                        //Legacy comment translated to English.


                        //Legacy comment translated to English.

                        //Legacy comment translated to English.
                        var messageHandlerFuncMethodInfo = this.GetType().GetMethod(nameof(BuildMessageHandlerFunc), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        var messageHandlerFuncGenericMethodInfo = messageHandlerFuncMethodInfo.MakeGenericMethod(messageContextGenericType);
                        var messageHandlerFunc = messageHandlerFuncGenericMethodInfo.Invoke(null,
                                new object[] { mpAccountDtoFunc, mpMessageHandlerType });

                        //Legacy comment translated to English.
                        var mpUrl = new PathString($"/WeixinMp/{mpMessageHandlerName}");

                        //Legacy comment translated to English.
                        Action<MessageHandlerMiddlewareOptions<ISenparcWeixinSettingForMP>> optionsAction = options =>
                        {
                            //Legacy comment translated to English.

                            #region Legacy section

                            //Legacy comment translated to English.
                            options.AccountSettingFunc = context =>
                            {
                                var mpAccountDto = mpAccountDtoFunc(context.RequestServices);

                                var senparcWeixinSetting = new SenparcWeixinSetting();
                                senparcWeixinSetting.WeixinAppId = mpAccountDto.AppId;
                                senparcWeixinSetting.WeixinAppSecret = mpAccountDto.AppSecret;
                                senparcWeixinSetting.Token = mpAccountDto.Token;
                                senparcWeixinSetting.EncodingAESKey = mpAccountDto.EncodingAESKey;
                                return senparcWeixinSetting;

                            };

                            //Legacy comment translated to English.

                            //Legacy comment translated to English.
                            //Config.SenparcWeixinSetting["<Your SenparcWeixinSetting's name filled with Token, AppId and EncodingAESKey>"]; 

                            //Legacy comment translated to English.

                            #endregion

                            //Legacy comment translated to English.
                            options.DefaultMessageHandlerAsyncEvent = DefaultMessageHandlerAsyncEvent.SelfSynicMethod;

                            //Legacy comment translated to English.
                            options.AggregateExceptionCatch = ex =>
                            {
                                //Legacy comment translated to English.
                                return false;//Legacy comment translated to English.
                            };
                        };

                        //Legacy comment translated to English.
                        var messageHandlerMiddlewareExType = typeof(Senparc.Weixin.MP.MessageHandlers.Middleware.MessageHandlerMiddlewareExtension);
                        var useMessageHandlerForMpMethodInfo = messageHandlerMiddlewareExType.GetMethod("UseMessageHandlerForMp", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        var useMessageHandlerForMpGenericMethodInfo = useMessageHandlerForMpMethodInfo.MakeGenericMethod(messageContextGenericType);

                        //invoke app.UseMessageHandlerForMp() extensionmethod
                        useMessageHandlerForMpGenericMethodInfo.Invoke(null, new object[] { app, mpUrl, messageHandlerFunc, optionsAction });

                        #region Legacy section

                        //app.UseMessageHandlerForMp($"/WeixinMp/{mpMessageHandlerName}", messageHandlerFunc, options =>
                        //{
                        //Legacy comment translated to English.

                        //    #region configure SenparcWeixinSetting parameters to provide Token, EncodingAESKey, and related values automatically

                        //Legacy comment translated to English.
                        //    options.AccountSettingFunc = context =>
                        //    {
                        //        var senparcWeixinSetting = new SenparcWeixinSetting();

                        //        var mpAccountDto = mpAccountDtoFunc(context.RequestServices);

                        //        senparcWeixinSetting.WeixinAppId = mpAccountDto.AppId;
                        //        senparcWeixinSetting.WeixinAppSecret = mpAccountDto.AppSecret;
                        //        senparcWeixinSetting.Token = mpAccountDto.Token;
                        //        senparcWeixinSetting.EncodingAESKey = mpAccountDto.EncodingAESKey;
                        //        return senparcWeixinSetting;

                        //    };

                        //Legacy comment translated to English.

                        //Legacy comment translated to English.
                        //    //Config.SenparcWeixinSetting["<Your SenparcWeixinSetting's name filled with Token, AppId and EncodingAESKey>"]; 

                        //Legacy comment translated to English.

                        //    #endregion

                        //Legacy comment translated to English.
                        //    options.DefaultMessageHandlerAsyncEvent = DefaultMessageHandlerAsyncEvent.SelfSynicMethod;

                        //Legacy comment translated to English.
                        //    options.AggregateExceptionCatch = ex =>
                        //    {
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        //    };
                        //});

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.BaseExceptionLog(ex);
                        throw;
                    }
                }

                return app;

            }
            catch (Exception ex)
            {
                SenparcTrace.SendCustomLog("Senparc.Xncf.WeixinManager IXncfMiddleware 异常", ex.Message);
                SenparcTrace.BaseExceptionLog(ex);
                throw;
            }
        }

        #endregion


    }
}

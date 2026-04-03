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
    public partial class Register : IXncfMiddleware  //Modules that need to introduce middleware
    {
        public static ConcurrentDictionary<string, Type> MpMessageHandlerNames = new ConcurrentDictionary<string, Type>();

        /// <summary>
        /// Create messageHandlerFunc object
        /// <para>Since the generic TMC is unknown during programming, this method is created and provided for reflection to construct the final messageHandlerFunc object</para>
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

                        //Register global cache information
                        Senparc.Weixin.Config.SenparcWeixinSetting[$"DynamicMP-{mpAccountDto.Id}"] = senparcWeixinSetting;

                        //Constructing a MessageHandler object using reflection
                        var messageHandler = Activator.CreateInstance(mpMessageHandlerType, new object[] { mpAccountDto, stream, postModel, maxRecordCount, services });

                        return messageHandler as MessageHandler<TMC, IRequestMessageBase, IResponseMessageBase>;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{mpMessageHandlerType.FullName} 必须具有以下结构和参数顺序的构造函数：(MpAccountDto mpAccountDto, Stream inputStream, PostModel postModel, int maxRecordCount, IServiceProvider serviceProvider)", ex);
                }
            };
            return messageHandlerFunc;
        }

        #region IXncfMiddleware 接口

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
                    return app;//Not registered
                }

                Func<IServiceProvider, MpAccountDto> mpAccountDtoFunc = serviceProvider =>
                {
                    var httpCotextAssessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
                    var httpContext = httpCotextAssessor.HttpContext;
                    var param = httpContext.Request.Query["parameter"].ToString();

                    if (!int.TryParse(param, out int mpAccountId))
                    {
                        throw new WeixinException("ID 错误！");
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

                        // message context generic
                        var messageContextGenericType = mpMessageHandlerType.BaseType.GetGenericArguments().First();
                        //TODO: Verify context type correctness


                        //Register middleware

                        //Get the MessageHandlerFunc method (because the TMC type is unknown, use reflection to get the generic method and get the generic MessageHandler<TMC, IRequestMessageBase, IResponseMessageBase>>)
                        var messageHandlerFuncMethodInfo = this.GetType().GetMethod(nameof(BuildMessageHandlerFunc), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        var messageHandlerFuncGenericMethodInfo = messageHandlerFuncMethodInfo.MakeGenericMethod(messageContextGenericType);
                        var messageHandlerFunc = messageHandlerFuncGenericMethodInfo.Invoke(null,
                                new object[] { mpAccountDtoFunc, mpMessageHandlerType });

                        //WeChat URL interface address of the current site
                        var mpUrl = new PathString($"/WeixinMp/{mpMessageHandlerName}");

                        //WeChat configuration
                        Action<MessageHandlerMiddlewareOptions<ISenparcWeixinSettingForMP>> optionsAction = options =>
                        {
                            //Note: This code block demonstrates relatively comprehensive function points. For simplified use, please refer to the following mini programs and enterprise WeChat

                            #region 配置 SenparcWeixinSetting 参数，以自动提供 Token、EncodingAESKey 等参数

                            //Here is the delegation, which can dynamically determine the input conditions based on the conditions (required)
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

                            //TODO: Register Config.SenparcWeixinSetting

                            //Method 2: Use specified configuration:
                            //Config.SenparcWeixinSetting["<Your SenparcWeixinSetting's name filled with Token, AppId and EncodingAESKey>"]; 

                            //Method 3: Combined with the context parameter to dynamically determine and return the Setting value

                            #endregion

                            //When no overriding is provided for asynchronous methods in MessageHandler, call synchronous methods (on demand)
                            options.DefaultMessageHandlerAsyncEvent = DefaultMessageHandlerAsyncEvent.SelfSynicMethod;

                            //Handle exceptions (optional)
                            options.AggregateExceptionCatch = ex =>
                            {
                                //Logical processing...
                                return false;//Exception thrown at system level
                            };
                        };

                        //Get WeChat middleware method
                        var messageHandlerMiddlewareExType = typeof(Senparc.Weixin.MP.MessageHandlers.Middleware.MessageHandlerMiddlewareExtension);
                        var useMessageHandlerForMpMethodInfo = messageHandlerMiddlewareExType.GetMethod("UseMessageHandlerForMp", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        var useMessageHandlerForMpGenericMethodInfo = useMessageHandlerForMpMethodInfo.MakeGenericMethod(messageContextGenericType);

                        //Call the app.UseMessageHandlerForMp() extension method
                        useMessageHandlerForMpGenericMethodInfo.Invoke(null, new object[] { app, mpUrl, messageHandlerFunc, optionsAction });

                        #region 原始方法

                        //app.UseMessageHandlerForMp($"/WeixinMp/{mpMessageHandlerName}", messageHandlerFunc, options =>
                        //{
                        //    //Note: This code block demonstrates relatively comprehensive function points. For simplified use, please refer to the following mini program and enterprise WeChat.

                        //    #region Configure SenparcWeixinSetting parameters to automatically provide Token, EncodingAESKey and other parameters

                        //    //Here is the commission, which can dynamically determine the input conditions based on the conditions (required)
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

                        //    //TODO: Register Config.SenparcWeixinSetting

                        //    //Method 2: Use specified configuration:
                        //    //Config.SenparcWeixinSetting["<Your SenparcWeixinSetting's name filled with Token, AppId and EncodingAESKey>"]; 

                        //    //Method 3: Combined with context parameters to dynamically determine and return the Setting value

                        //    #endregion

                        //    //When no overriding is provided for the asynchronous method in MessageHandler, call the synchronous method (on demand)
                        //    options.DefaultMessageHandlerAsyncEvent = DefaultMessageHandlerAsyncEvent.SelfSynicMethod;

                        //    //Handle exceptions (optional)
                        //    options.AggregateExceptionCatch = ex =>
                        //    {
                        //        //Logical processing...
                        //        return false;//Exception thrown at system level
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

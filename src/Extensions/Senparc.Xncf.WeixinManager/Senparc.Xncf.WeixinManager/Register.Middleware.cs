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
    public partial class Register : IXncfMiddleware  //需要引入中间件的模块
    {
        public static ConcurrentDictionary<string, Type> MpMessageHandlerNames = new ConcurrentDictionary<string, Type>();

        /// <summary>
        /// 创建 messageHandlerFunc 对象
        /// <para>由于泛型 TMC 在编程时未知，因此创建此方法，提供给反射使用，用于构造最终的 messageHandlerFunc 对象</para>
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

                        //注册全局缓存信息
                        Senparc.Weixin.Config.SenparcWeixinSetting[$"DynamicMP-{mpAccountDto.Id}"] = senparcWeixinSetting;

                        //使用反射构造 MessageHandler 对象
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
                    return app;//未注册
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

                        // 消息上下文泛型
                        var messageContextGenericType = mpMessageHandlerType.BaseType.GetGenericArguments().First();
                        //TODO：校验上下文类型正确性


                        //注册中间件

                        //获取 MessageHandlerFunc 方法（因为 TMC 类型未知，因此使用反射获取泛型方法后得到带泛型的 MessageHandler<TMC, IRequestMessageBase, IResponseMessageBase>>）
                        var messageHandlerFuncMethodInfo = this.GetType().GetMethod(nameof(BuildMessageHandlerFunc), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        var messageHandlerFuncGenericMethodInfo = messageHandlerFuncMethodInfo.MakeGenericMethod(messageContextGenericType);
                        var messageHandlerFunc = messageHandlerFuncGenericMethodInfo.Invoke(null,
                                new object[] { mpAccountDtoFunc, mpMessageHandlerType });

                        //当前站点的微信 URL 接口地址
                        var mpUrl = new PathString($"/WeixinMp/{mpMessageHandlerName}");

                        //微信配置
                        Action<MessageHandlerMiddlewareOptions<ISenparcWeixinSettingForMP>> optionsAction = options =>
                        {
                            //说明：此代码块中演示了较为全面的功能点，简化的使用可以参考下面小程序和企业微信

                            #region 配置 SenparcWeixinSetting 参数，以自动提供 Token、EncodingAESKey 等参数

                            //此处为委托，可以根据条件动态判断输入条件（必须）
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

                            //TODO：注册 Config.SenparcWeixinSetting

                            //方法二：使用指定配置：
                            //Config.SenparcWeixinSetting["<Your SenparcWeixinSetting's name filled with Token, AppId and EncodingAESKey>"]; 

                            //方法三：结合 context 参数动态判断返回Setting值

                            #endregion

                            //对 MessageHandler 内异步方法未提供重写时，调用同步方法（按需）
                            options.DefaultMessageHandlerAsyncEvent = DefaultMessageHandlerAsyncEvent.SelfSynicMethod;

                            //对发生异常进行处理（可选）
                            options.AggregateExceptionCatch = ex =>
                            {
                                //逻辑处理...
                                return false;//系统层面抛出异常
                            };
                        };

                        //获取微信中间件方法
                        var messageHandlerMiddlewareExType = typeof(Senparc.Weixin.MP.MessageHandlers.Middleware.MessageHandlerMiddlewareExtension);
                        var useMessageHandlerForMpMethodInfo = messageHandlerMiddlewareExType.GetMethod("UseMessageHandlerForMp", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        var useMessageHandlerForMpGenericMethodInfo = useMessageHandlerForMpMethodInfo.MakeGenericMethod(messageContextGenericType);

                        //调用 app.UseMessageHandlerForMp() 扩展方法
                        useMessageHandlerForMpGenericMethodInfo.Invoke(null, new object[] { app, mpUrl, messageHandlerFunc, optionsAction });

                        #region 原始方法

                        //app.UseMessageHandlerForMp($"/WeixinMp/{mpMessageHandlerName}", messageHandlerFunc, options =>
                        //{
                        //    //说明：此代码块中演示了较为全面的功能点，简化的使用可以参考下面小程序和企业微信

                        //    #region 配置 SenparcWeixinSetting 参数，以自动提供 Token、EncodingAESKey 等参数

                        //    //此处为委托，可以根据条件动态判断输入条件（必须）
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

                        //    //TODO：注册 Config.SenparcWeixinSetting

                        //    //方法二：使用指定配置：
                        //    //Config.SenparcWeixinSetting["<Your SenparcWeixinSetting's name filled with Token, AppId and EncodingAESKey>"]; 

                        //    //方法三：结合 context 参数动态判断返回Setting值

                        //    #endregion

                        //    //对 MessageHandler 内异步方法未提供重写时，调用同步方法（按需）
                        //    options.DefaultMessageHandlerAsyncEvent = DefaultMessageHandlerAsyncEvent.SelfSynicMethod;

                        //    //对发生异常进行处理（可选）
                        //    options.AggregateExceptionCatch = ex =>
                        //    {
                        //        //逻辑处理...
                        //        return false;//系统层面抛出异常
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

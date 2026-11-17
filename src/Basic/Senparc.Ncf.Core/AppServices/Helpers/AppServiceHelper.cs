/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: AppServiceHelper.cs

    File description: AppServiceHelper related implementation

    

    

    Created by: Senparc - 20211012

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Senparc.CO2NET.Cache;
using Senparc.Ncf.Core.AppServices.Exceptions;
using Senparc.Ncf.Core.Config;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.AppServices
{
    /// <summary>
    /// AppService Helper Class
    /// </summary>
    public static class AppServiceHelper
    {

        /// <summary>
        /// Automatic handling process for AppService
        /// </summary>
        /// <typeparam name="TData">Main message type returned from Service (must be DTO or value type, cannot be Entity)</typeparam>
        /// <param name="appService">AppService</param>
        /// <param name="func"></param>
        /// <param name="exceptionHandler"></param>
        /// <param name="afterFunc"></param>
        /// <param name="saveLogAfterFinished">Whether to save log after executecomplete</param>
        /// <param name="saveLogName">Save the name of the log. Optional. If left empty, then return the name of the current AppService.</param>
        /// <returns></returns>
        public static Task<AppResponseBase<TData>> GetResponseAsync<TData>(this IAppService appService, Func<AppResponseBase<TData>, AppServiceLogger, Task<TData>> func, Action<Exception, AppResponseBase<TData>, AppServiceLogger> exceptionHandler = null, Action<AppResponseBase<TData>, AppServiceLogger> afterFunc = null, bool saveLogAfterFinished = false, string saveLogName = null)
            //where TData : class
        {
            return GetResponseAsync<AppResponseBase<TData>, TData>(appService, func, exceptionHandler, afterFunc, saveLogAfterFinished, saveLogName);
        }

        ///// <summary>
        ///// AppService's automatic handling process (return string data)
        ///// </summary>
        ///// <param name="appService">AppService</param>
        ///// <param name="func"></param>
        ///// <param name="exceptionHandler"></param>
        ///// <param name="afterFunc"></param>
        ///// Whether to <param name="saveLogAfterFinished">save the log after executecomplete</param>
        /////<param name="saveLogName">Save the name of the log. Optional. If left empty, then return the name of the current AppService.</param>
        ///// <returns></returns>
        //public static Task<AppResponseBase<string>> GetStringResponseAsync(this IAppService appService, Func<AppResponseBase<string>, AppServiceLogger, Task<string>> func, Action<Exception, AppResponseBase<string>, AppServiceLogger> exceptionHandler = null, Action<AppResponseBase<string>, AppServiceLogger> afterFunc = null, bool saveLogAfterFinished = false, string saveLogName = null)
        //{
        //    return GetResponseAsync<AppResponseBase<string>, string>(appService, func, exceptionHandler, afterFunc, saveLogAfterFinished, saveLogName);
        //}

        /// <summary>
        /// Automatic handling process of AppService (return string data)
        /// </summary>
        /// <param name="appService">AppService</param>
        /// <param name="func"></param>
        /// <param name="exceptionHandler"></param>
        /// <param name="afterFunc"></param>
        /// <param name="saveLogAfterFinished">Whether to save log after executecomplete</param>
        /// <param name="saveLogName">Save the name of the log. Optional. If left empty, then return the name of the current AppService.</param>
        /// <returns></returns>
        public static Task<StringAppResponse> GetStringResponseAsync(this IAppService appService, Func<StringAppResponse, AppServiceLogger, Task<string>> func, Action<Exception, StringAppResponse, AppServiceLogger> exceptionHandler = null, Action<StringAppResponse, AppServiceLogger> afterFunc = null, bool saveLogAfterFinished = false, string saveLogName = null)
           //where IStringAppResponse : StringAppResponse
        {
            //Func<AppResponseBase<string>, AppServiceLogger, Task<string>> adaptFunc = (response, logger) =>
            //{
            //    return func?.Invoke((StringAppResponse)response, logger);
            //};

            //Action<Exception, AppResponseBase<string>, AppServiceLogger> adaptExceptionHandler = (ex, response, logger) =>
            //{
            //    exceptionHandler?.Invoke(ex, (StringAppResponse)response, logger);
            //};

            //Action<AppResponseBase<string>, AppServiceLogger> adaptAfterFunc = (response, logger) =>
            //{
            //    afterFunc?.Invoke((StringAppResponse)response, logger);
            //};

            return GetResponseAsync<StringAppResponse, string>(appService, func, exceptionHandler, afterFunc, saveLogAfterFinished, saveLogName);
        }

        /// <summary>
        /// Automatic handling process for AppService
        /// </summary>
        /// <typeparam name="TData">Main message type returned from Service (must be DTO or value type, cannot be Entity)</typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="appService">AppService</param>
        /// <param name="func"></param>
        /// <param name="exceptionHandler"></param>
        /// <param name="afterFunc"></param>
        /// <param name="saveLogAfterFinished">Whether to save log after executecomplete</param>
        /// <param name="saveLogName">Save the name of the log. Optional. If left empty, then return the name of the current AppService.</param>
        /// <returns></returns>
        public static async Task<TResponse> GetResponseAsync<TResponse, TData>(this IAppService appService, Func<TResponse, AppServiceLogger, Task<TData>> func, Action<Exception, TResponse, AppServiceLogger> exceptionHandler = null, Action<TResponse, AppServiceLogger> afterFunc = null, bool saveLogAfterFinished = false, string saveLogName = null)
            where TResponse : AppResponseBase<TData>, new()
        {
            var response = new TResponse();
            var logger = new AppServiceLogger();
            if (func == null)
            {
                response.StateCode = 404;
                response.ErrorMessage = "未提供处理过程";
                logger.Append("未提供处理过程");
                return response;
            }

            try
            {
                var result = await func.Invoke(response, logger);
                if (result != null)
                {
                    response.Data = result;
                };

                if (response.Success == null)
                {
                    response.Success = true;
                }

                ////checkfiletype
                //if (result is INcfFile)
                //{

                //}

            }
            catch (Exception ex)
            {
                response.Success = false;
                logger.Append($"发生错误（{ex.GetType().FullName}）：");
                logger.Append(ex.Message);
                logger.Append(ex.StackTrace);

                if (ex is BaseAppServiceException appEx)
                {
                    response.StateCode = appEx.StateCode;
                    response.ErrorMessage = appEx.Message;
                    logger.Append(appEx.InnerException?.StackTrace);
                }
                else
                {
                    response.StateCode = 100;
                    response.ErrorMessage = ex.Message;
                }

                exceptionHandler?.Invoke(ex, response, logger);
            }
            finally
            {
                afterFunc?.Invoke(response, logger);

                if (saveLogAfterFinished)
                {
                    var name = saveLogName ?? $"完成执行：{appService.GetType().FullName}";
                    logger.SaveLogs(saveLogName);
                }

                try
                {
                    if (SiteConfig.SenparcCoreSetting.RequestTempLogCacheMinutes > 0)
                    {
                        var tempId = response.RequestTempId;
                        var cache = appService.ServiceProvider.GetObjectCacheStrategyInstance();
                        //To speed up the response, do not wait.
                        _ = cache.SetAsync(tempId, logger.GetLogs(), TimeSpan.FromMinutes(SiteConfig.SenparcCoreSetting.RequestTempLogCacheMinutes));
                    }
                }
                finally { }
            }



            return response;
        }
    }
}

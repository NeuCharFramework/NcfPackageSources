/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AppServiceHelper.cs
    File description: AppServiceHelper related implementation

    Created by: Senparc - 20211012

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

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
    /// Provides the standard response, logging, and temporary-log-cache lifecycle
    /// used by NCF application services.
    /// </summary>
    /// <remarks>
    /// Exceptions raised by the main asynchronous operation are converted into a
    /// failed response. Exceptions raised by an optional exception handler or
    /// completion callback occur outside that conversion boundary and propagate
    /// to the caller.
    /// </summary>
    public static class AppServiceHelper
    {

        /// <summary>
        /// Executes an application-service callback inside the standard response envelope,
        /// logging lifecycle messages and invoking optional exception/finalization callbacks.
        /// </summary>
        /// <typeparam name="TData">
        /// The data type placed in the response envelope. In normal application-service
        /// usage this is a DTO, projection, or value type rather than a persistence entity.
        /// </typeparam>
        /// <param name="appService">The application service providing dependency and logging context.</param>
        /// <param name="func">
        /// The asynchronous operation. It receives the response being populated and the
        /// operation logger, then returns the data value to assign to the response.
        /// </param>
        /// <param name="exceptionHandler">
        /// Optional callback invoked after an exception has been converted into a failed
        /// response.
        /// </param>
        /// <param name="afterFunc">
        /// Optional callback invoked during finalization after success or failure is determined.
        /// </param>
        /// <param name="saveLogAfterFinished">
        /// Whether to persist the accumulated operation log after finalization.
        /// </param>
        /// <param name="saveLogName">
        /// Optional persisted log name. When omitted, the helper derives one from the
        /// application-service type.
        /// </param>
        /// <returns>A task containing the populated response envelope.</returns>
        /// <exception cref="Exception">
        /// Propagated when an optional finalization callback or post-operation logging
        /// hook throws. Exceptions raised by <paramref name="func"/> are converted
        /// into the returned failed response.
        /// </exception>
        public static Task<AppResponseBase<TData>> GetResponseAsync<TData>(this IAppService appService, Func<AppResponseBase<TData>, AppServiceLogger, Task<TData>> func, Action<Exception, AppResponseBase<TData>, AppServiceLogger> exceptionHandler = null, Action<AppResponseBase<TData>, AppServiceLogger> afterFunc = null, bool saveLogAfterFinished = false, string saveLogName = null)
            //where TData : class
        {
            return GetResponseAsync<AppResponseBase<TData>, TData>(appService, func, exceptionHandler, afterFunc, saveLogAfterFinished, saveLogName);
        }

        /// <summary>
        /// Executes an application-service callback that returns string data using the standard
        /// response and logging lifecycle.
        /// </summary>
        /// <param name="appService">The application service providing dependency and logging context.</param>
        /// <param name="func">
        /// The asynchronous operation that receives the string response and logger,
        /// then returns the string payload.
        /// </param>
        /// <param name="exceptionHandler">Optional callback for converted operation exceptions.</param>
        /// <param name="afterFunc">Optional callback invoked during finalization.</param>
        /// <param name="saveLogAfterFinished">Whether to persist the operation log after finalization.</param>
        /// <param name="saveLogName">Optional persisted log name.</param>
        /// <returns>A task containing the string response.</returns>
        /// <exception cref="Exception">
        /// Propagated when an optional finalization callback or post-operation logging
        /// hook throws. Exceptions raised by <paramref name="func"/> are converted
        /// into the returned failed response.
        /// </exception>
        public static Task<StringAppResponse> GetStringResponseAsync(this IAppService appService, Func<StringAppResponse, AppServiceLogger, Task<string>> func, Action<Exception, StringAppResponse, AppServiceLogger> exceptionHandler = null, Action<StringAppResponse, AppServiceLogger> afterFunc = null, bool saveLogAfterFinished = false, string saveLogName = null)
           //where IStringAppResponse : StringAppResponse
        {
            return GetResponseAsync<StringAppResponse, string>(appService, func, exceptionHandler, afterFunc, saveLogAfterFinished, saveLogName);
        }

        /// <summary>
        /// Executes a callback using a caller-supplied response type and standard logging lifecycle.
        /// </summary>
        /// <typeparam name="TResponse">
        /// The concrete response envelope type. It must provide a public parameterless
        /// constructor and derive from <see cref="AppResponseBase{TData}"/>.
        /// </typeparam>
        /// <typeparam name="TData">The data type returned by the callback.</typeparam>
        /// <param name="appService">The application service providing dependency and logging context.</param>
        /// <param name="func">
        /// The asynchronous operation. A <see langword="null"/> callback produces a
        /// 404-style response without invoking an operation.
        /// </param>
        /// <param name="exceptionHandler">Optional callback invoked after a callback exception is recorded.</param>
        /// <param name="afterFunc">Optional finalization callback invoked for success and failure.</param>
        /// <param name="saveLogAfterFinished">Whether to persist the accumulated operation log.</param>
        /// <param name="saveLogName">Optional persisted log name.</param>
        /// <returns>
        /// A task containing a response whose data and success/error fields reflect the
        /// callback outcome.
        /// </returns>
        /// <exception cref="Exception">
        /// Propagated when <paramref name="exceptionHandler"/>,
        /// <paramref name="afterFunc"/>, or a post-operation logging hook throws.
        /// Exceptions raised by <paramref name="func"/> are converted into the
        /// returned response.
        /// </exception>
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

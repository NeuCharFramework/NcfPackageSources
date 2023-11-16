using Senparc.CO2NET.Cache;
using Senparc.Ncf.Core.AppServices.Exceptions;
using Senparc.Ncf.Core.Config;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.AppServices
{
    /// <summary>
    /// AppService 帮助类
    /// </summary>
    public static class AppServiceHelper
    {
        /// <summary>
        /// AppService 的自动处理过程
        /// </summary>
        /// <typeparam name="TData">从 Service 返回的主体消息类型（必须为 DTO 或 值类型，不能是 Entity）</typeparam>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="appService">AppService</param>
        /// <param name="func"></param>
        /// <param name="exceptionHandler"></param>
        /// <param name="afterFunc"></param>
        /// <param name="saveLogAfterFinished">执行完成后是否保存日志</param>
        /// <param name="saveLogName">保存日志的名称，可选，如果留空，则返回当前 AppService 的名称</param>
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

                ////判断文件类型
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
                        //为了加快响应速度，不等待
                        _ = cache.SetAsync(tempId, logger.GetLogs(), TimeSpan.FromMinutes(SiteConfig.SenparcCoreSetting.RequestTempLogCacheMinutes));
                    }
                }
                finally { }
            }



            return response;
        }
    }
}

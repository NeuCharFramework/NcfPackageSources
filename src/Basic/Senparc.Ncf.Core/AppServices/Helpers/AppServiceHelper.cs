using Senparc.Ncf.Core.AppServices.Exceptions;
using System;
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
        public static TResponse GetResponse<TResponse, TData>(this IAppService appService, Func<TResponse, AppServiceLogger, TData> func, Action<Exception, TResponse, AppServiceLogger> exceptionHandler = null, Action<TResponse, AppServiceLogger> afterFunc = null, bool saveLogAfterFinished = false, string saveLogName = null)
            where TResponse : AppResponseBase<TData>, new()
        {
            var response = new TResponse();
            var logger = new AppServiceLogger();
            if (func == null)
            {
                response.StateCode = 404;
                response.ErrorMessage = "未提供处理过程";
                logger.RecordLog("未提供处理过程");
                return response;
            }

            try
            {
                response.Data = func.Invoke(response, logger);
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                logger.RecordLog($"发生错误（{ex.GetType().FullName}）：");
                logger.RecordLog(ex.Message);
                logger.RecordLog(ex.StackTrace);


                if (ex is BaseAppServiceException appEx)
                {
                    response.StateCode = appEx.StateCode;
                    response.ErrorMessage = appEx.Message;
                    logger.RecordLog(appEx.InnerException?.StackTrace);
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
            }

            return response;
        }
    }
}

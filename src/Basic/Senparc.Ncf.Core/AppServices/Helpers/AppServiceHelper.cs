using Senparc.Ncf.Core.AppServices.Exceptions;
using System;

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
        /// <param name="func"></param>
        /// <param name="exceptionHandler"></param>
        /// <returns></returns>
        public static TResponse GetResponse<TResponse,TData>(Func<TResponse,TData> func, Action<Exception, TResponse> exceptionHandler = null)
            where TResponse : AppResponseBase<TData>, new()
        {
            var response = new TResponse();
            if (func == null)
            {
                response.StateCode = 404;
                response.ErrorMessage = "未提供处理过程";
                return response;
            }

            try
            {
                response.Data = func.Invoke(response);
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                if (ex is BaseAppServiceException appEx)
                {
                    response.StateCode = appEx.StateCode;
                    response.ErrorMessage = appEx.Message;
                }
                else
                {
                    response.StateCode = 100;
                    response.ErrorMessage = ex.Message;
                }

                exceptionHandler?.Invoke(ex, response);
            }

            return response;
        }
    }
}

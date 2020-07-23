using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Functions
{
    /// <summary>
    /// Function 帮助类
    /// </summary>
    public class FunctionHelper
    {
        #region 同步方法

        /// <summary>
        /// 执行 Run 方法的公共方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="param"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static FunctionResult RunFunction<T>(IFunctionParameter param, Func<T, StringBuilder, FunctionResult, FunctionResult> func)
            where T : IFunctionParameter
        {
            var typeParam = (T)param;
            StringBuilder sb = new StringBuilder();
            FunctionResult result = new FunctionResult()
            {
                Success = true
            };

            try
            {
                var newResult = func.Invoke(typeParam, sb, result);
                if (newResult != null)
                {
                    result = newResult;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Exception = new XscfFunctionException(ex.Message, ex);
                result.Message = "发生错误！" + result.Message;

                RecordLog(sb, "发生错误：" + ex.Message);
                RecordLog(sb, ex.StackTrace.ToString());
            }
            result.Log = sb.ToString();
            return result;
        }


        /// <summary>
        /// 执行 Run 方法的公共方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="param"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static FunctionResult RunFunction<T>(IFunctionParameter param, Action<T, StringBuilder, FunctionResult> action)
            where T : IFunctionParameter
        {
            return RunFunction<T>(param, (typeParam, sb, result) =>
            {
                action(typeParam, sb, result);
                return null;
            });
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="msg"></param>
        public static void RecordLog(StringBuilder sb, string msg)
        {
            sb.AppendLine($"[{SystemTime.Now.ToString()}]\t{msg}");
        }

        #endregion

        #region 异步方法


        /// <summary>
        /// 【异步方法】执行 Run 方法的公共方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="param"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static async Task<FunctionResult> RunFunctionAsync<T>(IFunctionParameter param, Func<T, StringBuilder, FunctionResult, Task<FunctionResult>> func)
            where T : IFunctionParameter
        {
            var typeParam = (T)param;
            StringBuilder sb = new StringBuilder();
            FunctionResult result = new FunctionResult()
            {
                Success = true
            };

            try
            {
                var newResult = await func.Invoke(typeParam, sb, result).ConfigureAwait(false);
                if (newResult != null)
                {
                    result = newResult;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Exception = new XscfFunctionException(ex.Message, ex);
                result.Message = "发生错误！" + result.Message;

                RecordLog(sb, "发生错误：" + ex.Message);
                RecordLog(sb, ex.StackTrace.ToString());
            }
            result.Log = sb.ToString();
            return result;
        }

        /// <summary>
        /// 【异步方法】执行 Run 方法的公共方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="param"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static async Task<FunctionResult> RunFunctionAsync<T>(IFunctionParameter param, Func<T, StringBuilder, FunctionResult, Task> action)
            where T : IFunctionParameter
        {
            return await RunFunctionAsync<T>(param, async (typeParam, sb, result) =>
             {
                 await action(typeParam, sb, result).ConfigureAwait(false);
                 return null;
             });
        }


        #endregion
    }
}

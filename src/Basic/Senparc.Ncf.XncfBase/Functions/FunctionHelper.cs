using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
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
                result.Exception = new XncfFunctionException(ex.Message, ex);
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
                result.Exception = new XncfFunctionException(ex.Message, ex);
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


        /// <summary>
        /// 获取所有参数的信息列表
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="functionRenderBag">FunctionRenderBag</param>
        /// <param name="tryLoadData">是否尝试载入数据（参数必须实现 IFunctionParameterLoadDataBase 接口）</param>
        /// <returns></returns>
        public static async Task<List<FunctionParameterInfo>> GetFunctionParameterInfoAsync(IServiceProvider serviceProvider, FunctionRenderBag? functionRenderBag = null, bool tryLoadData = true)
        {
            var functionParameterType = functionRenderBag?.GetType();
            if (functionParameterType == null)
            {
                functionParameterType = typeof(FunctionAppRequestBase);
            }

            var paraObj = functionParameterType.Assembly.CreateInstance(functionParameterType.FullName) as IAppRequest;

            if (tryLoadData && paraObj is FunctionAppRequestBase functionRequestPara)
            {
                await functionRequestPara.LoadData(serviceProvider);//载入原有信息
            }

            var props = functionParameterType.GetProperties();
            ParameterType parameterType = ParameterType.Text;
            List<FunctionParameterInfo> result = new List<FunctionParameterInfo>();
            foreach (var prop in props)
            {
                SelectionList selectionList = null;
                parameterType = ParameterType.Text;//默认为文本内容
                                                   //判断是否存在选项
                if (prop.PropertyType == typeof(SelectionList))
                {
                    var selections = prop.GetValue(paraObj, null) as SelectionList;
                    switch (selections.SelectionType)
                    {
                        case SelectionType.DropDownList:
                            parameterType = ParameterType.DropDownList;
                            break;
                        case SelectionType.CheckBoxList:
                            parameterType = ParameterType.CheckBoxList;
                            break;
                        default:
                            //TODO: throw
                            break;
                    }
                    selectionList = selections;
                }

                var name = prop.Name;
                string title = null;
                string description = null;
                var isRequired = prop.GetCustomAttribute<RequiredAttribute>() != null;
                var descriptionAttr = prop.GetCustomAttribute<DescriptionAttribute>();
                if (descriptionAttr != null && descriptionAttr.Description != null)
                {
                    //分割：名称||说明
                    var descriptionAttrArr = descriptionAttr.Description.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                    title = descriptionAttrArr[0];
                    if (descriptionAttrArr.Length > 1)
                    {
                        description = descriptionAttrArr[1];
                    }
                }
                var systemType = prop.PropertyType.Name;

                object value = null;
                try
                {
                    value = prop.GetValue(paraObj);
                }
                catch (Exception ex)
                {
                    SenparcTrace.BaseExceptionLog(ex);
                }

                var functionParamInfo = new FunctionParameterInfo(name, title, description, isRequired, systemType, parameterType,
                                            selectionList ?? new SelectionList(SelectionType.Unknown), value);
                result.Add(functionParamInfo);
            }
            return result;
        }

        #endregion
    }
}

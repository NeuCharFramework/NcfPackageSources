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
        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="msg"></param>
        public static void RecordLog(StringBuilder sb, string msg)
        {
            sb.AppendLine($"[{SystemTime.Now.ToString()}]\t{msg}");
        }

        /// <summary>
        /// 获取所有参数的信息列表
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="functionRenderBag">FunctionRenderBag</param>
        /// <param name="tryLoadData">是否尝试载入数据（参数必须实现 IFunctionParameterLoadDataBase 接口）</param>
        /// <returns></returns>
        public static async Task<List<FunctionParameterInfo>> GetFunctionParameterInfoAsync(IServiceProvider serviceProvider, FunctionRenderBag functionRenderBag, bool tryLoadData = true)
        {
            var functionParameterType = functionRenderBag.FunctionParameterType;

            var paraObj = functionParameterType.Assembly.CreateInstance(functionParameterType.FullName) as IAppRequest;//TODO:通过 DI 生成

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
    }
}

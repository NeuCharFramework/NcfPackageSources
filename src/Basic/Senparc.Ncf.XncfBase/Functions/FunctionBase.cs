using Senparc.CO2NET.Trace;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase
{
    public abstract class FunctionBase : IXncfFunction
    {
        /// <summary>
        /// 方法名称
        /// <para>注意：Name 必须在单个 Xncf 模块中唯一！</para>
        /// </summary>
        public abstract string Name { get; }

        //TODO:检查 name 冲突的情况

        /// <summary>
        /// 说明
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// FunctionParameter 类型
        /// </summary>
        public abstract Type FunctionParameterType { get; }

        public virtual IFunctionParameter GenerateParameterInstance()
        {
            var obj = FunctionParameterType.Assembly.CreateInstance(FunctionParameterType.FullName) as IFunctionParameter;
            return obj;
        }

        /// <summary>
        /// ServiceProvider 实例
        /// </summary>
        public virtual IServiceProvider ServiceProvider { get; set; }


        public FunctionBase(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// 执行程序
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public abstract FunctionResult Run(IFunctionParameter param);

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="msg"></param>
        protected void RecordLog(StringBuilder sb, string msg)
        {
            FunctionHelper.RecordLog(sb, msg);
        }

        /// <summary>
        /// 获取所有参数的信息列表
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="tryLoadData">是否尝试载入数据（参数必须实现 IFunctionParameterLoadDataBase 接口）</param>
        /// <returns></returns>
        public async Task<List<FunctionParameterInfo>> GetFunctionParameterInfoAsync(IServiceProvider serviceProvider, bool tryLoadData)
        {
            var obj = GenerateParameterInstance();

            //预载入参数
            if (tryLoadData && obj is IFunctionParameterLoadDataBase loadDataParam)
            {
                await loadDataParam.LoadData(serviceProvider);//载入参数
            }

            var props = FunctionParameterType.GetProperties();
            ParameterType parameterType = ParameterType.Text;
            List<FunctionParameterInfo> result = new List<FunctionParameterInfo>();
            foreach (var prop in props)
            {
                SelectionList selectionList = null;
                parameterType = ParameterType.Text;//默认为文本内容
                //判断是否存在选项
                if (prop.PropertyType == typeof(SelectionList))
                {
                    var selections = prop.GetValue(obj, null) as SelectionList;
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
                    value = prop.GetValue(obj);
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

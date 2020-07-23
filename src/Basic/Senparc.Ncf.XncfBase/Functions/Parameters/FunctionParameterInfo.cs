using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    public enum ParameterType
    {
        Text,
        DropDownList,
        CheckBoxList,
    }

    /// <summary>
    /// FunctionParameter 信息（供输出用）
    /// </summary>
    public class FunctionParameterInfo
    {
        /// <summary>
        /// 参数名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 标题（标签内容）
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 是否必须
        /// </summary>
        public bool IsRequired { get; set; }
        /// <summary>
        /// 系统类型
        /// </summary>
        public string SystemType { get; set; }

        /// <summary>
        /// 参数类型
        /// </summary>
        public ParameterType ParameterType { get; set; } = ParameterType.Text;

        /// <summary>
        /// 文本值（当文本类型时使用）
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// 选项（当出现非文本内容时使用）
        /// </summary>
        public SelectionList SelectionList { get; set; }

        public FunctionParameterInfo()
        {
        }

        public FunctionParameterInfo(string name, string title, string description,
            bool isRequired, string systemType, ParameterType parameterType, SelectionList selectionList, object value)
        {
            Name = name;
            Title = title;
            Description = description;
            IsRequired = isRequired;
            SystemType = systemType;
            SelectionList = selectionList;
            ParameterType = parameterType;
            Value = value;
        }
    }
}

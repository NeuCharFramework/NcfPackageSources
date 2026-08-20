/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FunctionParameterInfo.cs
    文件功能描述：FunctionParameterInfo 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// 参数类型
    /// <para>注意：请勿更改已经定义的顺序和值！</para>
    /// </summary>
    public enum ParameterType
    {
        Text = 0,
        DropDownList = 1,
        CheckBoxList = 2,
        Password = 3,
        /// <summary>
        /// 单个布尔，对应 <c>bool</c> / <c>bool?</c>，前端渲染为单个复选框。
        /// </summary>
        CheckBox = 4,
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
        /// 最大长度（一般应用于字符串）
        public int MaxLength { get; set; }

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

        /// <summary>
        /// 下拉框是否允许搜索
        /// </summary>
        public bool Filterable { get; set; }

        /// <summary>
        /// 下拉框是否允许创建自定义值
        /// </summary>
        public bool AllowCreate { get; set; }

        public FunctionParameterInfo()
        {
        }

        public FunctionParameterInfo(string name, string title, string description,
            bool isRequired, string systemType, ParameterType parameterType, SelectionList selectionList, object value, int maxLength,
            bool filterable = false, bool allowCreate = false)
        {
            Name = name;
            Title = title;
            Description = description;
            IsRequired = isRequired;
            SystemType = systemType;
            SelectionList = selectionList;
            ParameterType = parameterType;
            Value = value;
            MaxLength = maxLength;
            Filterable = filterable;
            AllowCreate = allowCreate;
        }
    }
}

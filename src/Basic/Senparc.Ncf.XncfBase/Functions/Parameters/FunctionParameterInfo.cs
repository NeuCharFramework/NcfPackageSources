using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// parameter type
    /// <para>Note: Do not change the defined order and values! </para>
    /// </summary>
    public enum ParameterType
    {
        Text = 0,
        DropDownList = 1,
        CheckBoxList = 2,
        Password = 3,
    }

    /// <summary>
    ///FunctionParameter information (for output)
    /// </summary>
    public class FunctionParameterInfo
    {
        /// <summary>
        /// parameter name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// title (tag content)
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Remark
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// is it necessary
        /// </summary>
        public bool IsRequired { get; set; }
        /// <summary>
        /// system type
        /// </summary>
        public string SystemType { get; set; }
        /// <summary>
        /// Maximum length (generally applied to strings)
        public int MaxLength { get; set; }

        /// <summary>
        /// parameter type
        /// </summary>
        public ParameterType ParameterType { get; set; } = ParameterType.Text;

        /// <summary>
        /// Text value (used when text type)
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// option (used when non-text content is present)
        /// </summary>
        public SelectionList SelectionList { get; set; }

        public FunctionParameterInfo()
        {
        }

        public FunctionParameterInfo(string name, string title, string description,
            bool isRequired, string systemType, ParameterType parameterType, SelectionList selectionList, object value, int maxLength)
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
        }
    }
}

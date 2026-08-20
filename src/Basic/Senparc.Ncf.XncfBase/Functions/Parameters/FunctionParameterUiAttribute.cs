/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FunctionParameterUiAttribute.cs
    文件功能描述：FunctionParameterUiAttribute 相关实现
    
    
    创建标识：Senparc - 20260424
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Declares how a simple request property should be rendered in the Function UI.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FunctionParameterUiAttribute : Attribute
    {
        public ParameterType ParameterType { get; }

        public string SelectionListPropertyName { get; }

        public bool Filterable { get; set; }

        public bool AllowCreate { get; set; }

        public FunctionParameterUiAttribute(ParameterType parameterType, string selectionListPropertyName = null)
        {
            ParameterType = parameterType;
            SelectionListPropertyName = selectionListPropertyName;
        }
    }
}
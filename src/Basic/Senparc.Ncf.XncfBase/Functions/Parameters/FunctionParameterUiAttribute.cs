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
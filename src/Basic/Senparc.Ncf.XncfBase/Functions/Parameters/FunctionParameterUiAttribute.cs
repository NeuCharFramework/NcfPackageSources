/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FunctionParameterUiAttribute.cs
    File description: FunctionParameterUiAttribute related implementation

    Created by: Senparc - 20260424

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261103
    Change description: v0.28.1 add bulk file upload functionality to Sandbox Lab

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.29.1.2 Extend shared chat-tool contracts for admin task initiation.

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

        public bool AllowMultiple { get; set; }

        public int MaxFiles { get; set; }

        public string Accept { get; set; }

        /// <summary>
        /// Initializes UI metadata for a Function request property.
        /// </summary>
        /// <param name="parameterType">The control/parameter type to render.</param>
        /// <param name="selectionListPropertyName">The optional property supplying selection items.</param>
        public FunctionParameterUiAttribute(ParameterType parameterType, string selectionListPropertyName = null)
        {
            ParameterType = parameterType;
            SelectionListPropertyName = selectionListPropertyName;
        }
    }
}

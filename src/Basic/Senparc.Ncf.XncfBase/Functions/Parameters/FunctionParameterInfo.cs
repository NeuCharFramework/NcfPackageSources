/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FunctionParameterInfo.cs
    File description: FunctionParameterInfo related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260916
    Change description: v0.27.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.28.1 0.28.1 Merge remote-tracking branch 'origin/Developer-MAF-V3' into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.28.1 add bulk file upload functionality to Sandbox Lab

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Identifies the UI control used to collect a Function parameter value.
    /// </summary>
    public enum ParameterType
    {
        Text = 0,
        DropDownList = 1,
        CheckBoxList = 2,
        Password = 3,
        /// <summary>Renders a boolean checkbox.</summary>
        CheckBox = 4,
        /// <summary>Renders a multi-line text editor.</summary>
        TextArea = 5,
        /// <summary>Renders a file-upload control.</summary>
        File = 6,
    }

    /// <summary>
    /// Describes one reflected function parameter for UI rendering and request binding.
    /// </summary>
    public class FunctionParameterInfo
    {
        /// <summary>
        /// parametersname
        /// </summary>
        public string Name { get; set; }
        /// <summary>Gets or sets the localized display title.</summary>
        public string Title { get; set; }
        /// <summary>Gets or sets the localized helper description.</summary>
        public string Description { get; set; }
        /// <summary>
        /// whethermust
        /// </summary>
        public bool IsRequired { get; set; }
        /// <summary>Gets or sets the CLR type name used by binding.</summary>
        public string SystemType { get; set; }
        /// <summary>Gets or sets the maximum accepted text length.
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// parameterstype
        /// </summary>
        public ParameterType ParameterType { get; set; } = ParameterType.Text;

        /// <summary>Gets or sets the default or current value.</summary>
        public object Value { get; set; }

        /// <summary>Gets or sets the available values for selection controls.</summary>
        public SelectionList SelectionList { get; set; }

        /// <summary>Gets or sets whether selection values may be filtered.</summary>
        public bool Filterable { get; set; }

        /// <summary>Gets or sets whether a new selection value may be created.</summary>
        public bool AllowCreate { get; set; }

        /// <summary>Gets or sets whether multiple values may be selected.</summary>
        public bool AllowMultiple { get; set; }

        /// <summary>Gets or sets the maximum number of uploaded files.</summary>
        public int MaxFiles { get; set; }

        /// <summary>Gets or sets the accepted file extension or MIME filter.</summary>
        public string Accept { get; set; }

        /// <summary>Initializes an empty parameter metadata object.</summary>
        public FunctionParameterInfo()
        {
        }

        /// <summary>
        /// Initializes parameter metadata used by function renderers and callers.
        /// </summary>
        /// <param name="name">The property/parameter name.</param>
        /// <param name="title">The localized display title.</param>
        /// <param name="description">The localized helper description.</param>
        /// <param name="isRequired">Whether a value is required.</param>
        /// <param name="systemType">The CLR type name.</param>
        /// <param name="parameterType">The rendered control type.</param>
        /// <param name="selectionList">Optional selection values.</param>
        /// <param name="value">The default/current value.</param>
        /// <param name="maxLength">The maximum text length.</param>
        /// <param name="filterable">Whether selection filtering is allowed.</param>
        /// <param name="allowCreate">Whether a new selection value may be created.</param>
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

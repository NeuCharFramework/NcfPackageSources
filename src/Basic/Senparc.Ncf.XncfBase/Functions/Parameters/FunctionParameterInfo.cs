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

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// parameterstype
    ///Legacy comment translated to English.
    /// </summary>
    public enum ParameterType
    {
        Text = 0,
        DropDownList = 1,
        CheckBoxList = 2,
        Password = 3,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        CheckBox = 4,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        TextArea = 5,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        File = 6,
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class FunctionParameterInfo
    {
        /// <summary>
        /// parametersname
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// whethermust
        /// </summary>
        public bool IsRequired { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string SystemType { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        public int MaxLength { get; set; }

        /// <summary>
        /// parameterstype
        /// </summary>
        public ParameterType ParameterType { get; set; } = ParameterType.Text;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public SelectionList SelectionList { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool Filterable { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool AllowCreate { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool AllowMultiple { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int MaxFiles { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Accept { get; set; }

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

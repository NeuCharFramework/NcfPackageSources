/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfMethodAttribute.cs
    File description: XncfMethodAttribute related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class XncfMethodAttribute : Attribute
    {
        public string Name { get; set; }

        public XncfMethodAttribute(string name)
        {
            Name = name;
        }
    }
}

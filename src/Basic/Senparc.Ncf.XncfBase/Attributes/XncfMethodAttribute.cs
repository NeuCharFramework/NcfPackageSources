/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfMethodAttribute.cs
    File description: XncfMethodAttribute related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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

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
    /// Marks a method with the display name used by XNCF discovery and metadata rendering.
    /// </summary>
    public class XncfMethodAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the method display name or localization key.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Initializes an XNCF method metadata attribute.
        /// </summary>
        /// <param name="name">The method display name or localization key.</param>
        public XncfMethodAttribute(string name)
        {
            Name = name;
        }
    }
}

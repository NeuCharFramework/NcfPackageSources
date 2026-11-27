/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfOrderAttribute.cs
    File description: XncfOrderAttribute related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.29.1.2 Extend shared chat-tool contracts for admin task initiation.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Assigns an explicit ordering value to an XNCF-discovered type or member.
    /// </summary>
    public class XncfOrderAttribute : Attribute
    {
        /// <summary>
        /// Initializes an ordering attribute.
        /// </summary>
        /// <param name="order">The numeric order used by the discovery consumer.</param>
        public XncfOrderAttribute(int order)
        {
            Order = order;
        }

        /// <summary>
        /// Gets or sets the numeric discovery order.
        /// </summary>
        public int Order { get; set; }
    }
}

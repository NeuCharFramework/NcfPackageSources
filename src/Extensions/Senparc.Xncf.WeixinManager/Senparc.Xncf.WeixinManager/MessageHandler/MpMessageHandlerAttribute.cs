/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MpMessageHandlerAttribute.cs
    File description: MpMessageHandlerAttribute related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

    Modified by: Senparc - 20261127
    Change description: v0.25.1.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.WeixinManager
{
    /// <summary>
    /// Marks a message-handler type with the logical handler name used by discovery.
    /// </summary>
    public class MpMessageHandlerAttribute : Attribute
    {
        /// <summary>
        /// Initializes the attribute with a handler name.
        /// </summary>
        /// <param name="name">The logical handler name.</param>
        public MpMessageHandlerAttribute(string name)
        {
            Name = name;
        }


        /// <summary>
        /// Gets or sets the logical handler name.
        /// </summary>
        public string Name { get; set; }
    }
}

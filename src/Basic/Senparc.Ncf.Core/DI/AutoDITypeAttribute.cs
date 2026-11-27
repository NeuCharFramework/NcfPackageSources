/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AutoDITypeAttribute.cs
    File description: AutoDITypeAttribute related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.DI
{
    /// <summary>
    /// Specifies the dependency-injection lifetime used when registering a type automatically.
    /// </summary>
    public class AutoDITypeAttribute : Attribute
    {
        /// <summary>Gets or sets the dependency-injection lifetime.</summary>
        public DILifecycleType DILifecycleType { get; set; } = DILifecycleType.Scoped;

        /// <summary>Initializes an attribute using the scoped lifetime.</summary>
        public AutoDITypeAttribute() { }

        /// <summary>
        /// Initializes an attribute with an explicit dependency-injection lifetime.
        /// </summary>
        /// <param name="diLifecycleType">The lifetime to use during registration.</param>
        public AutoDITypeAttribute(DILifecycleType diLifecycleType)
        {
            DILifecycleType = diLifecycleType;
        }
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AutoDITypeAttribute.cs
    File description: AutoDITypeAttribute related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.DI
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class AutoDITypeAttribute : Attribute
    {
        public DILifecycleType DILifecycleType { get; set; } = DILifecycleType.Scoped;

        public AutoDITypeAttribute() { }

        public AutoDITypeAttribute(DILifecycleType diLifecycleType)
        {
            DILifecycleType = diLifecycleType;
        }
    }
}

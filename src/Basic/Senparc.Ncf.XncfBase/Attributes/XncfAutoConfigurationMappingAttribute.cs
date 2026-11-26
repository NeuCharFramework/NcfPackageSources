/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfAutoConfigurationMappingAttribute.cs
    File description: XncfAutoConfigurationMappingAttribute related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.Attributes
{
    /// <summary>
    /// Marks an Entity Framework Core configuration class for automatic discovery
    /// by the XNCF database registration pipeline.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class XncfAutoConfigurationMappingAttribute : Attribute
    {
    }
}

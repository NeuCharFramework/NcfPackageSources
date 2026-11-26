/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfModuleProfile.cs
    File description: XncfModuleProfile related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using AutoMapper;
using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.AutoMapper
{
    /// <summary>
    /// Builds an AutoMapper profile from mapping actions contributed by all
    /// currently registered XNCF modules.
    /// </summary>
    public class XncfModuleProfile : Profile
    {

        /// <summary>
        /// Initializes the profile and invokes each registered module's mapping
        /// configuration action against this profile.
        /// </summary>
        /// <exception cref="Exception">
        /// Propagated when a module mapping action fails while the profile is built.
        /// </exception>
        public XncfModuleProfile()
        {
            foreach (var register in XncfRegisterManager.RegisterList)
            {
                if (register.AutoMapMappingConfigs != null)
                {
                    foreach (var config in register.AutoMapMappingConfigs)
                    {
                        config(this);
                    }
                }
            }
        }
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfModuleProfile.cs
    File description: XncfModuleProfile related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using AutoMapper;
using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.AutoMapper
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class XncfModuleProfile : Profile
    {

        public XncfModuleProfile()
        {
            //Console.WriteLine("XncfModuleProfile, XncfRegisterManager.RegisterList Count:" + XncfRegisterManager.RegisterList.Count);
            foreach (var register in XncfRegisterManager.RegisterList)
            {
                //Console.WriteLine("register:" + register.Name);
                if (register.AutoMapMappingConfigs != null)
                {
                    //Console.WriteLine("register.AutoMapMappingConfigs:" + register.Name + "/" + register.AutoMapMappingConfigs.Count);
                    foreach (var config in register.AutoMapMappingConfigs)
                    {
                        config(this);
                    }
                }
            }
        }
    }
}

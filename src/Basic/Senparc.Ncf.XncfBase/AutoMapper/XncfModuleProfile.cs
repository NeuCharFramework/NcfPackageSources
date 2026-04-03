using AutoMapper;
using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.AutoMapper
{
    /// <summary>
    ///AutoMap configuration used by the Xncf module
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

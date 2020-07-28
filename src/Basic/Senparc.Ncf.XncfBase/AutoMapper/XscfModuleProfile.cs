using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.AutoMapper
{
    /// <summary>
    /// Xncf 模块使用的 AutoMap 配置
    /// </summary>
    public class XncfModuleProfile : Profile
    {
        public XncfModuleProfile()
        {
            foreach (var register in Register.RegisterList)
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

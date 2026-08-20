/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfModuleProfile.cs
    文件功能描述：XncfModuleProfile 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using AutoMapper;
using Senparc.CO2NET.Trace;
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

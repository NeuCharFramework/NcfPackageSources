/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：TenantInfoProfile.cs
    文件功能描述：TenantInfoProfile 相关实现
    
    
    创建标识：Senparc - 20211222
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using AutoMapper;
using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.Tenant.Domain.DatabaseModel.AutoMapper
{
    public class TenantInfoProfile : Profile
    {
        public TenantInfoProfile()
        {
            CreateMap<TenantInfo, TenantInfoDto>();
        }
    }
}

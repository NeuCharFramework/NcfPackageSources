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

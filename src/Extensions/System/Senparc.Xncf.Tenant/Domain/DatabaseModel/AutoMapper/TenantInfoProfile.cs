/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TenantInfoProfile.cs
    File description: TenantInfoProfile related implementation

    Created by: Senparc - 20211222

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.15.17 Updated module behavior, localization metadata, and English source comments

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

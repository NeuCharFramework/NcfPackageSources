/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: WeixinClawAdminBindingRepository.cs
    File description: WeixinClawAdminBindingRepository.cs implementation and project behavior.


    Created by: Senparc - 20260926

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 enhance WeixinClaw integration with new message handling and UI improvements

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL.Repository;

public interface IWeixinClawAdminBindingRepository : IClientRepositoryBase<WeixinClawAdminBinding>
{
}

public sealed class WeixinClawAdminBindingRepository
    : ClientRepositoryBase<WeixinClawAdminBinding>, IWeixinClawAdminBindingRepository
{
    private WeixinClawAdminBindingRepository() : base(null)
    {
    }

    public WeixinClawAdminBindingRepository(INcfDbData ncfDbData) : base(ncfDbData)
    {
    }
}

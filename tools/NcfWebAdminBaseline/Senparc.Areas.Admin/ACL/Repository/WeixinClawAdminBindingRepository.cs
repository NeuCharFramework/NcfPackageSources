/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawAdminBindingRepository.cs
    File description: WeixinClawAdminBindingRepository.cs implementation and project behavior.

    Created by: Senparc - 20260926

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 enhance WeixinClaw integration with new message handling and UI improvements

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

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
    /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
    private WeixinClawAdminBindingRepository() : base(null)
    {
    }

    /// <summary>Initializes the binding repository with the NCF database provider.</summary>
    /// <param name="ncfDbData">The database data provider used by the base repository.</param>
    public WeixinClawAdminBindingRepository(INcfDbData ncfDbData) : base(ncfDbData)
    {
    }
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: AdminChatSessionWorkflowRepository.cs
    File description: AdminChatSessionWorkflowRepository.cs related implementation


    Created by: Senparc - 20260821

    Modified by: Senparc - 20260822
    Change description: v0.6.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL;

public interface IAdminChatSessionWorkflowRepository : IClientRepositoryBase<AdminChatSessionWorkflow>
{
}

public sealed class AdminChatSessionWorkflowRepository
    : ClientRepositoryBase<AdminChatSessionWorkflow>, IAdminChatSessionWorkflowRepository
{
    private AdminChatSessionWorkflowRepository() : base(null!) { }

    public AdminChatSessionWorkflowRepository(INcfDbData ncfDbData) : base(ncfDbData)
    {
    }
}

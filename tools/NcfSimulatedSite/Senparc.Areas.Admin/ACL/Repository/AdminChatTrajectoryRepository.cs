/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatTrajectoryRepository.cs
    File description: Related implementation.

    Created by: Senparc - 20260915

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

----------------------------------------------------------------*/
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL;

public interface IAdminChatTrajectoryRepository : IClientRepositoryBase<AdminChatTrajectory>
{
}

public class AdminChatTrajectoryRepository : ClientRepositoryBase<AdminChatTrajectory>, IAdminChatTrajectoryRepository
{
    private AdminChatTrajectoryRepository() : base(null) { }

    public AdminChatTrajectoryRepository(INcfDbData ncfDbData) : base(ncfDbData)
    {
    }
}

public interface IAdminChatTrajectoryEventRepository : IClientRepositoryBase<AdminChatTrajectoryEvent>
{
}

public class AdminChatTrajectoryEventRepository : ClientRepositoryBase<AdminChatTrajectoryEvent>, IAdminChatTrajectoryEventRepository
{
    private AdminChatTrajectoryEventRepository() : base(null) { }

    public AdminChatTrajectoryEventRepository(INcfDbData ncfDbData) : base(ncfDbData)
    {
    }
}

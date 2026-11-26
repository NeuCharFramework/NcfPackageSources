/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatTrajectoryRepository.cs
    File description: Related implementation.

    Created by: Senparc - 20260915

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

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
    /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
    private AdminChatTrajectoryRepository() : base(null) { }

    /// <summary>Initializes the trajectory repository with the NCF database provider.</summary>
    /// <param name="ncfDbData">The database data provider used by the base repository.</param>
    public AdminChatTrajectoryRepository(INcfDbData ncfDbData) : base(ncfDbData)
    {
    }
}

public interface IAdminChatTrajectoryEventRepository : IClientRepositoryBase<AdminChatTrajectoryEvent>
{
}

public class AdminChatTrajectoryEventRepository : ClientRepositoryBase<AdminChatTrajectoryEvent>, IAdminChatTrajectoryEventRepository
{
    /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
    private AdminChatTrajectoryEventRepository() : base(null) { }

    /// <summary>Initializes the trajectory-event repository with the NCF database provider.</summary>
    /// <param name="ncfDbData">The database data provider used by the base repository.</param>
    public AdminChatTrajectoryEventRepository(INcfDbData ncfDbData) : base(ncfDbData)
    {
    }
}

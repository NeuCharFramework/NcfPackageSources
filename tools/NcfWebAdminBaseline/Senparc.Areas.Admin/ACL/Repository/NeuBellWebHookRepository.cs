/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuBellWebHookRepository.cs
    File description: Related implementation.

    Created by: Senparc - 20260906

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL.Repository;

public interface INeuBellWebHookRepository : IClientRepositoryBase<NeuBellWebHook> { }

public sealed class NeuBellWebHookRepository : ClientRepositoryBase<NeuBellWebHook>, INeuBellWebHookRepository
{
    /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
    private NeuBellWebHookRepository() : base(null) { }
    /// <summary>Initializes the webhook repository with the NCF database provider.</summary>
    /// <param name="ncfDbData">The database data provider used by the base repository.</param>
    public NeuBellWebHookRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public interface INeuBellWebHookLogRepository : IClientRepositoryBase<NeuBellWebHookLog> { }

public sealed class NeuBellWebHookLogRepository : ClientRepositoryBase<NeuBellWebHookLog>, INeuBellWebHookLogRepository
{
    /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
    private NeuBellWebHookLogRepository() : base(null) { }
    /// <summary>Initializes the webhook-log repository with the NCF database provider.</summary>
    /// <param name="ncfDbData">The database data provider used by the base repository.</param>
    public NeuBellWebHookLogRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuBellWebHookRepository.cs
    File description: Related implementation.

    Created by: Senparc - 20260906

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL.Repository;

public interface INeuBellWebHookRepository : IClientRepositoryBase<NeuBellWebHook> { }

public sealed class NeuBellWebHookRepository : ClientRepositoryBase<NeuBellWebHook>, INeuBellWebHookRepository
{
    private NeuBellWebHookRepository() : base(null) { }
    public NeuBellWebHookRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public interface INeuBellWebHookLogRepository : IClientRepositoryBase<NeuBellWebHookLog> { }

public sealed class NeuBellWebHookLogRepository : ClientRepositoryBase<NeuBellWebHookLog>, INeuBellWebHookLogRepository
{
    private NeuBellWebHookLogRepository() : base(null) { }
    public NeuBellWebHookLogRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

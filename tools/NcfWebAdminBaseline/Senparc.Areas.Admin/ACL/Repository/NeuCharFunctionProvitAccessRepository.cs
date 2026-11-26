/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharFunctionProvitAccessRepository.cs
    File description: Related implementation.
    Change description: v0.9.1 Updated module behavior and related support.

    Created by: Senparc - 20260917

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL;

public interface INeuCharFunctionProvitAccessRepository : IClientRepositoryBase<NeuCharFunctionProvitAccess> { }

public sealed class NeuCharFunctionProvitAccessRepository : ClientRepositoryBase<NeuCharFunctionProvitAccess>, INeuCharFunctionProvitAccessRepository
{
    /// <summary>
    /// Reserved parameterless constructor for repository infrastructure.
    /// </summary>
    private NeuCharFunctionProvitAccessRepository() : base(null) { }
    /// <summary>
    /// Initializes the repository with the current NCF database data provider.
    /// </summary>
    /// <param name="ncfDbData">The database data provider used by the base repository.</param>
    public NeuCharFunctionProvitAccessRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

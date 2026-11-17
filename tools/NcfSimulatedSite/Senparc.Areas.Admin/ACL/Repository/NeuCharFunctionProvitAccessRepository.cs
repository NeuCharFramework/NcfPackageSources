/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: NeuCharFunctionProvitAccessRepository.cs
    File description: Related implementation.

    Created by: Senparc - 20260917
    Change description: v0.9.1 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL;

public interface INeuCharFunctionProvitAccessRepository : IClientRepositoryBase<NeuCharFunctionProvitAccess> { }

public sealed class NeuCharFunctionProvitAccessRepository : ClientRepositoryBase<NeuCharFunctionProvitAccess>, INeuCharFunctionProvitAccessRepository
{
    private NeuCharFunctionProvitAccessRepository() : base(null) { }
    public NeuCharFunctionProvitAccessRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

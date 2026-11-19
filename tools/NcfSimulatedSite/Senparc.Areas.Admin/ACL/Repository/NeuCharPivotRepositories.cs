/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NeuCharPivotRepositories.cs
    File description: Related implementation.

    Created by: Senparc - 20260809

    Modified by: Senparc - 20260813
    Change description: v0.5.0 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL;

public interface INeuCharPivotConfigurationRepository : IClientRepositoryBase<NeuCharPivotConfiguration> { }
public interface INeuCharPivotFunctionRepository : IClientRepositoryBase<NeuCharPivotFunction> { }
public interface INeuCharPivotLoopTaskRepository : IClientRepositoryBase<NeuCharPivotLoopTask> { }
public interface INeuCharExecutionLogRepository : IClientRepositoryBase<NeuCharExecutionLog> { }

public sealed class NeuCharPivotConfigurationRepository : ClientRepositoryBase<NeuCharPivotConfiguration>, INeuCharPivotConfigurationRepository
{
    private NeuCharPivotConfigurationRepository() : base(null) { }
    public NeuCharPivotConfigurationRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharPivotFunctionRepository : ClientRepositoryBase<NeuCharPivotFunction>, INeuCharPivotFunctionRepository
{
    private NeuCharPivotFunctionRepository() : base(null) { }
    public NeuCharPivotFunctionRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharPivotLoopTaskRepository : ClientRepositoryBase<NeuCharPivotLoopTask>, INeuCharPivotLoopTaskRepository
{
    private NeuCharPivotLoopTaskRepository() : base(null) { }
    public NeuCharPivotLoopTaskRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharExecutionLogRepository : ClientRepositoryBase<NeuCharExecutionLog>, INeuCharExecutionLogRepository
{
    private NeuCharExecutionLogRepository() : base(null) { }
    public NeuCharExecutionLogRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public interface INeuCharPivotBoardRepository : IClientRepositoryBase<NeuCharPivotBoard> { }

public sealed class NeuCharPivotBoardRepository : ClientRepositoryBase<NeuCharPivotBoard>, INeuCharPivotBoardRepository
{
    private NeuCharPivotBoardRepository() : base(null) { }
    public NeuCharPivotBoardRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

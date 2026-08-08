/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharPivotRepositories.cs
    文件功能描述：NeuCharPivot 系统实体仓储
----------------------------------------------------------------*/

using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL;

public interface INeuCharPivotConfigurationRepository : IClientRepositoryBase<NeuCharPivotConfiguration> { }
public interface INeuCharPivotFunctionRepository : IClientRepositoryBase<NeuCharPivotFunction> { }
public interface INeuCharPivotLoopTaskRepository : IClientRepositoryBase<NeuCharPivotLoopTask> { }
public interface INeuCharWorkflowRepository : IClientRepositoryBase<NeuCharWorkflow> { }
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

public sealed class NeuCharWorkflowRepository : ClientRepositoryBase<NeuCharWorkflow>, INeuCharWorkflowRepository
{
    private NeuCharWorkflowRepository() : base(null) { }
    public NeuCharWorkflowRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharExecutionLogRepository : ClientRepositoryBase<NeuCharExecutionLog>, INeuCharExecutionLogRepository
{
    private NeuCharExecutionLogRepository() : base(null) { }
    public NeuCharExecutionLogRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

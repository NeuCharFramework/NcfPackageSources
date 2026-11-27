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

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

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
    /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
    private NeuCharPivotConfigurationRepository() : base(null) { }
    /// <summary>Initializes the repository with the NCF database provider.</summary>
    /// <param name="ncfDbData">The database data provider used by the base repository.</param>
    public NeuCharPivotConfigurationRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharPivotFunctionRepository : ClientRepositoryBase<NeuCharPivotFunction>, INeuCharPivotFunctionRepository
{
    /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
    private NeuCharPivotFunctionRepository() : base(null) { }
    /// <summary>Initializes the repository with the NCF database provider.</summary>
    /// <param name="ncfDbData">The database data provider used by the base repository.</param>
    public NeuCharPivotFunctionRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharPivotLoopTaskRepository : ClientRepositoryBase<NeuCharPivotLoopTask>, INeuCharPivotLoopTaskRepository
{
    /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
    private NeuCharPivotLoopTaskRepository() : base(null) { }
    /// <summary>Initializes the repository with the NCF database provider.</summary>
    /// <param name="ncfDbData">The database data provider used by the base repository.</param>
    public NeuCharPivotLoopTaskRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public sealed class NeuCharExecutionLogRepository : ClientRepositoryBase<NeuCharExecutionLog>, INeuCharExecutionLogRepository
{
    /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
    private NeuCharExecutionLogRepository() : base(null) { }
    /// <summary>Initializes the repository with the NCF database provider.</summary>
    /// <param name="ncfDbData">The database data provider used by the base repository.</param>
    public NeuCharExecutionLogRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

public interface INeuCharPivotBoardRepository : IClientRepositoryBase<NeuCharPivotBoard> { }

public sealed class NeuCharPivotBoardRepository : ClientRepositoryBase<NeuCharPivotBoard>, INeuCharPivotBoardRepository
{
    /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
    private NeuCharPivotBoardRepository() : base(null) { }
    /// <summary>Initializes the repository with the NCF database provider.</summary>
    /// <param name="ncfDbData">The database data provider used by the base repository.</param>
    public NeuCharPivotBoardRepository(INcfDbData ncfDbData) : base(ncfDbData) { }
}

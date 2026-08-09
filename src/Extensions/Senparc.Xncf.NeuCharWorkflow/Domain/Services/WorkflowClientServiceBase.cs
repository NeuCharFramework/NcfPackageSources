using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>模块内持久化服务的最小基类，避免依赖 Admin 的服务基类。</summary>
public abstract class WorkflowClientServiceBase<TEntity> : ClientServiceBase<TEntity>
    where TEntity : EntityBase
{
    protected WorkflowClientServiceBase(IClientRepositoryBase<TEntity> repository, IServiceProvider serviceProvider)
        : base(repository, serviceProvider) { }
}

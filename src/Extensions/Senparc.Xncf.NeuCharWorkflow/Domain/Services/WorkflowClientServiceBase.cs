/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WorkflowClientServiceBase.cs
    文件功能描述：领域服务与业务流程实现


    创建标识：Senparc - 20260810

    修改标识：Senparc - 20260813
    修改描述：v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

----------------------------------------------------------------*/

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

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: WorkflowClientServiceBase.cs
    File description: 领域服务与业务流程实现


    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 增强工作流编排、回放、Webhook 与并行执行能力

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

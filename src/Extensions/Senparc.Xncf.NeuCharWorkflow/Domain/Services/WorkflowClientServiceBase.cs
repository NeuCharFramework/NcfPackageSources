/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WorkflowClientServiceBase.cs
    File description: Domain services and business workflow implementation

    Created by: Senparc - 20260810

    Modified by: Senparc - 20260813
    Change description: v0.1.0-preview1 improve workflow orchestration, replay, Webhook, and parallel execution

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using System;

namespace Senparc.Xncf.NeuCharWorkflow.Domain.Services;

/// <summary>Minimal base class for module persistence services without depending on the Admin service base.</summary>
public abstract class WorkflowClientServiceBase<TEntity> : ClientServiceBase<TEntity>
    where TEntity : EntityBase
{
    protected WorkflowClientServiceBase(IClientRepositoryBase<TEntity> repository, IServiceProvider serviceProvider)
        : base(repository, serviceProvider) { }
}

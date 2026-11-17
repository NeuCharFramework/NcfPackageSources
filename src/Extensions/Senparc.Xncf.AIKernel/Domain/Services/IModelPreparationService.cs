/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IModelPreparationService.cs
    File description: Related implementation.

    Created by: Senparc - 20261014

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add bilingual contextual documentation help with iframe dialog and new-window access

----------------------------------------------------------------*/

#nullable enable
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Senparc.Xncf.AIKernel.Domain.Models.ModelPreparation;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public interface IModelPreparationService
{
    Task<IReadOnlyList<ModelSearchItem>> SearchAsync(ModelSearchRequest request,
        CancellationToken cancellationToken = default);
    Task<ModelPreparationStatus> GetStatusAsync(ModelPreparationRequest request,
        CancellationToken cancellationToken = default);
    Task<ModelPreparationResult> DownloadAsync(ModelPreparationRequest request,
        CancellationToken cancellationToken = default);
}

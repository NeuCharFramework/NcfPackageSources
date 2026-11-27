/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIFineTuningWorkerService.cs
    File description: Related implementation.

    Created by: Senparc - 20261009

    Modified by: Senparc - 20261009
    Change description: v0.16.4 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 enhance Agent Studio, group configuration, and task management interactions

    Modified by: Senparc - 20261106
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.AIKernel.OHS.Local.PL;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public sealed class AIFineTuningWorkerService : ServiceBase<AIFineTuningWorker>
{
    private readonly IHostEnvironment _environment;

    /// <summary>Initializes fine-tuning Worker persistence and launch validation.</summary>
    /// <param name="repo">The Worker repository.</param>
    /// <param name="serviceProvider">The dependency provider.</param>
    /// <param name="environment">The host environment used to resolve launch paths.</param>
    public AIFineTuningWorkerService(IRepositoryBase<AIFineTuningWorker> repo, IServiceProvider serviceProvider,
        IHostEnvironment environment)
        : base(repo, serviceProvider)
    {
        _environment = environment;
    }

    /// <summary>Lists Workers ordered by name and marks aliases with configured secrets.</summary>
    /// <param name="configuredAliases">Aliases known to have configured secrets.</param>
    /// <param name="cancellationToken">Token used to cancel lookup.</param>
    /// <returns>Worker DTOs ordered for display.</returns>
    public async Task<List<AIFineTuningWorkerDto>> GetListAsync(IEnumerable<string> configuredAliases,
        CancellationToken cancellationToken = default)
    {
        var configured = new HashSet<string>(configuredAliases, StringComparer.OrdinalIgnoreCase);
        var workers = await GetFullListAsync(z => true).ConfigureAwait(false);
        return workers.OrderBy(z => z.Name, StringComparer.OrdinalIgnoreCase)
            .Select(z => new AIFineTuningWorkerDto(z, configured.Contains(z.Alias))).ToList();
    }

    /// <summary>Loads all persisted fine-tuning Workers.</summary>
    /// <param name="cancellationToken">Token used to cancel lookup.</param>
    /// <returns>All Worker entities.</returns>
    public async Task<List<AIFineTuningWorker>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var workers = await GetFullListAsync(z => true).ConfigureAwait(false);
        return workers.ToList();
    }

    /// <summary>Creates or updates a Worker after validating launch profile, endpoint, and alias uniqueness.</summary>
    /// <param name="request">The Worker create/edit request.</param>
    /// <param name="configuredAliases">Aliases with configured secrets.</param>
    /// <param name="cancellationToken">Token used to cancel persistence.</param>
    /// <returns>The saved Worker DTO.</returns>
    public async Task<AIFineTuningWorkerDto> SaveAsync(AIFineTuningWorker_CreateOrEditRequest request,
        IEnumerable<string> configuredAliases, CancellationToken cancellationToken = default)
    {
        var peers = await GetFullListAsync(z => z.Id != request.Id).ConfigureAwait(false);
        var profile = FineTuningWorkerLaunch.Prepare(request, _environment.ContentRootPath,
            peers.Select(peer => new FineTuningWorkerPeer(peer.Alias, peer.LaunchProfileJson)));
        var launchJson = profile.ToJson();
        ValidateEndpoint(request.Endpoint);
        var duplicate = await GetObjectAsync(z => z.Alias == request.Alias && z.Id != request.Id).ConfigureAwait(false);
        if (duplicate != null)
            throw new NcfExceptionBase("A fine-tuning worker with this alias already exists.");

        AIFineTuningWorker worker;
        if (request.Id == 0)
        {
            worker = new AIFineTuningWorker(request.Alias, request.Name, request.Endpoint,
                request.RequestTimeoutSeconds, request.Enabled, request.Note, launchJson);
        }
        else
        {
            worker = await GetObjectAsync(z => z.Id == request.Id).ConfigureAwait(false)
                ?? throw new NcfExceptionBase("The fine-tuning worker does not exist.");
            worker.Update(request.Alias, request.Name, request.Endpoint, request.RequestTimeoutSeconds,
                request.Enabled, request.Note, launchJson);
        }

        await SaveObjectAsync(worker).ConfigureAwait(false);
        return new AIFineTuningWorkerDto(worker,
            configuredAliases.Contains(worker.Alias, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>Loads a Worker by alias and rejects missing aliases/entities.</summary>
    /// <param name="workerAlias">The Worker alias.</param>
    /// <param name="cancellationToken">Token used to cancel lookup.</param>
    /// <returns>The matching Worker entity.</returns>
    public async Task<AIFineTuningWorker> GetRequiredAsync(string workerAlias, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workerAlias))
            throw new NcfExceptionBase("A fine-tuning worker must be selected.");
        return await GetObjectAsync(z => z.Alias == workerAlias).ConfigureAwait(false)
            ?? throw new NcfExceptionBase("The selected fine-tuning worker does not exist.");
    }

    /// <summary>Loads an enabled Worker by alias.</summary>
    /// <param name="workerAlias">The Worker alias.</param>
    /// <param name="cancellationToken">Token used to cancel lookup.</param>
    /// <returns>The enabled Worker entity.</returns>
    public async Task<AIFineTuningWorker> GetEnabledAsync(string workerAlias, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workerAlias))
            throw new NcfExceptionBase("A fine-tuning worker must be selected.");
        var worker = await GetObjectAsync(z => z.Alias == workerAlias).ConfigureAwait(false)
            ?? throw new NcfExceptionBase("The selected fine-tuning worker does not exist.");
        if (!worker.Enabled)
            throw new NcfExceptionBase("The selected fine-tuning worker is disabled.");
        return worker;
    }

    /// <summary>Validates an absolute HTTP(S) Worker endpoint without credentials/query/fragment.</summary>
    /// <param name="value">The endpoint value.</param>
    /// <exception cref="NcfExceptionBase">Thrown when endpoint validation fails.</exception>
    private static void ValidateEndpoint(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new NcfExceptionBase("Fine-tuning WorkerEndpoint must be an absolute HTTP(S) URL without credentials, query or fragment.");
    }
}

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Models;
using Senparc.Xncf.AIKernel.OHS.Local.PL;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public sealed class AIFineTuningWorkerService : ServiceBase<AIFineTuningWorker>
{
    public AIFineTuningWorkerService(IRepositoryBase<AIFineTuningWorker> repo, IServiceProvider serviceProvider)
        : base(repo, serviceProvider)
    {
    }

    public async Task<List<AIFineTuningWorkerDto>> GetListAsync(IEnumerable<string> configuredAliases,
        CancellationToken cancellationToken = default)
    {
        var configured = new HashSet<string>(configuredAliases, StringComparer.OrdinalIgnoreCase);
        var workers = await GetFullListAsync(z => true).ConfigureAwait(false);
        return workers.OrderBy(z => z.Name, StringComparer.OrdinalIgnoreCase)
            .Select(z => new AIFineTuningWorkerDto(z, configured.Contains(z.Alias))).ToList();
    }

    public async Task<AIFineTuningWorkerDto> SaveAsync(AIFineTuningWorker_CreateOrEditRequest request,
        IEnumerable<string> configuredAliases, CancellationToken cancellationToken = default)
    {
        ValidateEndpoint(request.Endpoint);
        var duplicate = await GetObjectAsync(z => z.Alias == request.Alias && z.Id != request.Id).ConfigureAwait(false);
        if (duplicate != null)
            throw new NcfExceptionBase("A fine-tuning worker with this alias already exists.");

        AIFineTuningWorker worker;
        if (request.Id == 0)
        {
            worker = new AIFineTuningWorker(request.Alias, request.Name, request.Endpoint,
                request.RequestTimeoutSeconds, request.Enabled, request.Note);
        }
        else
        {
            worker = await GetObjectAsync(z => z.Id == request.Id).ConfigureAwait(false)
                ?? throw new NcfExceptionBase("The fine-tuning worker does not exist.");
            worker.Update(request.Alias, request.Name, request.Endpoint, request.RequestTimeoutSeconds,
                request.Enabled, request.Note);
        }

        await SaveObjectAsync(worker).ConfigureAwait(false);
        return new AIFineTuningWorkerDto(worker,
            configuredAliases.Contains(worker.Alias, StringComparer.OrdinalIgnoreCase));
    }

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

    private static void ValidateEndpoint(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new NcfExceptionBase("Fine-tuning WorkerEndpoint must be an absolute HTTP(S) URL without credentials, query or fragment.");
    }
}

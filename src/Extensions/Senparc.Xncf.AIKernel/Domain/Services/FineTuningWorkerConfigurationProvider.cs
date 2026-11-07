/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FineTuningWorkerConfigurationProvider.cs
    文件功能描述：微调 Worker 配置提供程序


    创建标识：Senparc - 20261009

    修改标识：Senparc - 20261009
    修改描述：v0.16.4 完善 AIKernel 本地微调 Worker、数据库配置与本地化管理能力

    修改标识：Senparc - 20261103
    修改描述：v0.17.0 enhance Agent Studio, group configuration, and task management interactions

    修改标识：Senparc - 20261106
    修改描述：环境变量中的密钥优先，缺失时读取站点密钥文件

-----------------------------------------------------------

    修改标识：Senparc - 20261108
    修改描述：v0.18.0 新增微调 Worker 配置提供器
----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Models;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public sealed record FineTuningWorkerConfiguration(string Alias, Uri Endpoint, string ApiKey, int RequestTimeoutSeconds);

public interface IFineTuningWorkerConfigurationProvider
{
    Task<FineTuningWorkerConfiguration> GetAsync(string workerAlias, CancellationToken cancellationToken = default);
    Task<List<AIFineTuningWorkerDto>> GetWorkersAsync(CancellationToken cancellationToken = default);
}

public sealed class FineTuningWorkerConfigurationProvider : IFineTuningWorkerConfigurationProvider
{
    private readonly AIFineTuningWorkerService _workerService;
    private readonly FineTuningSecretStore _secrets;
    private readonly FineTuningWorkerOptions _options;

    public FineTuningWorkerConfigurationProvider(AIFineTuningWorkerService workerService,
        FineTuningSecretStore secrets, IOptions<FineTuningWorkerOptions> options)
    {
        _workerService = workerService;
        _secrets = secrets;
        _options = options.Value;
    }

    public async Task<FineTuningWorkerConfiguration> GetAsync(string workerAlias, CancellationToken cancellationToken = default)
    {
        var worker = await _workerService.GetEnabledAsync(workerAlias, cancellationToken).ConfigureAwait(false);
        var endpoint = _options.GetValidatedEndpoint(worker.Alias, worker.Endpoint, worker.RequestTimeoutSeconds);
        return new FineTuningWorkerConfiguration(worker.Alias, endpoint, _secrets.Resolve(_options, worker.Alias),
            worker.RequestTimeoutSeconds);
    }

    public async Task<List<AIFineTuningWorkerDto>> GetWorkersAsync(CancellationToken cancellationToken = default)
    {
        var aliases = _options.ConfiguredWorkerAliases.Concat(_secrets.ListConfiguredAliases());
        var workers = await _workerService.GetListAsync(aliases, cancellationToken).ConfigureAwait(false);
        foreach (var worker in workers)
        {
            try
            {
                worker.SecretPath = _secrets.PathFor(worker.Alias);
            }
            catch (NcfExceptionBase)
            {
                worker.SecretPath = "";
            }
        }

        return workers;
    }
}

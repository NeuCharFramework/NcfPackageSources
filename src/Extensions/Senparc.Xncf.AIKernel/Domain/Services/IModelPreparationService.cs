#nullable enable
using System.Threading;
using System.Threading.Tasks;
using Senparc.Xncf.AIKernel.Domain.Models.ModelPreparation;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public interface IModelPreparationService
{
    Task<ModelPreparationStatus> GetStatusAsync(ModelPreparationRequest request,
        CancellationToken cancellationToken = default);
    Task<ModelPreparationResult> DownloadAsync(ModelPreparationRequest request,
        CancellationToken cancellationToken = default);
}

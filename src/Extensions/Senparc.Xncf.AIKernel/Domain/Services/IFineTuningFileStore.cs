using Senparc.Xncf.FileManager.Abstractions;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    public interface IFineTuningFileStore
    {
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

        Task<FileManagerImportResult> ImportAsync(
            Stream content,
            string fileName,
            string contentType,
            long length,
            string description,
            CancellationToken cancellationToken = default);
    }
}

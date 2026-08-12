using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using System.Threading.Tasks;

namespace Senparc.Xncf.FileManager.Domain.Services;

/// <summary>
/// Optional cross-module guard for a file deletion. FileManager owns the file
/// lifecycle, while consumers such as KnowledgeBase can register a guard to
/// prevent removal of a source that is still referenced.
/// </summary>
public interface INcfFileDeletionGuard
{
    Task EnsureCanDeleteAsync(NcfFile file);
}

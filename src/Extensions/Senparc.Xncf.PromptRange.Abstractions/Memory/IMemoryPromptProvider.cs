using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Abstractions.Memory;

public sealed record MemoryUpdatePrompt(string Content, string PromptCode);

public interface IMemoryPromptProvider
{
    Task<MemoryUpdatePrompt> GetAsync(int modelId, CancellationToken cancellationToken = default);
}

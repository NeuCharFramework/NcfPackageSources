/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IMemoryPromptProvider.cs
    File description: IMemoryPromptProvider related definitions and implementation

    Created by: Senparc - 20261103

    Modified by: Senparc - 20261103
    Change description: v0.3.1-preview6 implement AdminChat memory storage and management system

    Modified by: Senparc - 20261119
    Change description: v0.3.3-preview8 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Abstractions.Memory;

public sealed record MemoryUpdatePrompt(string Content, string PromptCode);

public interface IMemoryPromptProvider
{
    Task<MemoryUpdatePrompt> GetAsync(int modelId, CancellationToken cancellationToken = default);
}

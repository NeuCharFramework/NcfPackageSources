/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IMemoryPromptProvider.cs
    文件功能描述：IMemoryPromptProvider 相关定义与实现


    创建标识：Senparc - 20261103

    修改标识：Senparc - 20261103
    修改描述：v0.3.1-preview6 implement AdminChat memory storage and management system

----------------------------------------------------------------*/

using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Abstractions.Memory;

public sealed record MemoryUpdatePrompt(string Content, string PromptCode);

public interface IMemoryPromptProvider
{
    Task<MemoryUpdatePrompt> GetAsync(int modelId, CancellationToken cancellationToken = default);
}

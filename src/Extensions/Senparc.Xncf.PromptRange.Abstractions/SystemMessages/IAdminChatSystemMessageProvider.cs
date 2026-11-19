/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IAdminChatSystemMessageProvider.cs
    File description: Contract for the default AdminChat system prompt provider

    Created by: Senparc - 20261115

    Modified by: Senparc - 20261119
    Change description: v0.3.3-preview8 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Abstractions.SystemMessages;

public sealed record AdminChatSystemMessagePrompt(string Content, string PromptCode);

public interface IAdminChatSystemMessageProvider
{
    Task<AdminChatSystemMessagePrompt> GetAsync(CancellationToken cancellationToken = default);
}

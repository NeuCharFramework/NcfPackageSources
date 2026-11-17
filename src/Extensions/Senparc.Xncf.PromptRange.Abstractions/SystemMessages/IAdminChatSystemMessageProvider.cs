/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IAdminChatSystemMessageProvider.cs
    File description: Contract for the default AdminChat system prompt provider

    Created by: Senparc - 20261115

----------------------------------------------------------------*/

using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Abstractions.SystemMessages;

public sealed record AdminChatSystemMessagePrompt(string Content, string PromptCode);

public interface IAdminChatSystemMessageProvider
{
    Task<AdminChatSystemMessagePrompt> GetAsync(CancellationToken cancellationToken = default);
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ChatGroupHistoryService.cs
    File description: Related implementation.

    Created by: Senparc - 20241017

    Modified by: Senparc - 20260701
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261119
    Change description: v0.19.4 Localized agent management and task surfaces

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.Domain.Exceptions;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class ChatGroupHistoryService : ServiceBase<ChatGroupHistory>
    {
        /// <summary>
        /// Initializes the ChatGroup history persistence service.
        /// </summary>
        /// <param name="repo">The repository for history entities.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public ChatGroupHistoryService(IRepositoryBase<ChatGroupHistory> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        //[ApiBind(ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        /// <summary>
        /// Creates and persists one chat-history record from a DTO.
        /// </summary>
        /// <param name="chatGroupHistoryDto">The history values to persist.</param>
        /// <returns>The persisted history entity.</returns>
        /// <exception cref="AgentsManagerException">Thrown when persistence fails after diagnostics are recorded.</exception>
        public async Task<ChatGroupHistory> CreateHistory(ChatGroupHistoryDto chatGroupHistoryDto)
        {
            try
            {
                ChatGroupHistory chatGroupHistory = new ChatGroupHistory(chatGroupHistoryDto);
                await base.SaveObjectAsync(chatGroupHistory);
                return chatGroupHistory;
            }
            catch (Exception ex)
            {
                new AgentsManagerException(ex.Message, ex, false);
                throw;
            }
        }

        /// <summary>
        /// Extracts the raw message from a stored history payload by removing the
        /// diagnostic prefix used by the chat pipeline.
        /// </summary>
        /// <param name="message">The stored message text.</param>
        /// <returns>The extracted message, or the original text when no separator exists.</returns>
        public string GetRawMessage(string message)
        {
            var arr = message.Split(new[] { $"{Environment.NewLine}--------------------{Environment.NewLine}" }, StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length >= 2)
            {
                return arr[1].Trim();
            }

            return message;
        }

    }
}

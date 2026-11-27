/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptResultChatService.cs
    File description: Related implementation.

    Created by: Senparc - 20251213

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.20.17.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    /// <summary>
    /// PromptResultChat Service
    /// </summary>
    public class PromptResultChatService : ServiceBase<PromptResultChat>
    {
        public PromptResultChatService(
            IRepositoryBase<PromptResultChat> repo,
            IServiceProvider serviceProvider
            ) : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Loads chat messages for one prompt result in sequence order.
        /// </summary>
        /// <param name="promptResultId">The prompt-result identifier.</param>
        /// <returns>The ordered chat message DTOs.</returns>
        public async Task<List<PromptResultChatDto>> GetByPromptResultIdAsync(int promptResultId)
        {
            var chatList = await this.GetFullListAsync(
                c => c.PromptResultId == promptResultId,
                c => c.Sequence,
                OrderingType.Ascending);

            return chatList.Select(c => new PromptResultChatDto(c)).ToList();
        }

        /// <summary>
        /// Persists chat messages and assigns sequence values when the caller does not provide a start sequence.
        /// </summary>
        /// <param name="promptResultId">The owning prompt-result identifier.</param>
        /// <param name="chatMessages">The user/assistant messages to persist.</param>
        /// <param name="startSequence">Optional first sequence value.</param>
        /// <returns>The persisted chat message DTOs.</returns>
        /// <exception cref="Exception">Thrown when a saved message cannot be reloaded with a generated ID.</exception>
        public async Task<List<PromptResultChatDto>> AddChatMessagesAsync(int promptResultId, List<ChatMessageDto> chatMessages, int? startSequence = null)
        {
            if (chatMessages == null || chatMessages.Count == 0)
            {
                return new List<PromptResultChatDto>();
            }

            var chatEntities = new List<PromptResultChat>();
            int sequence;

            if (startSequence.HasValue)
            {
                sequence = startSequence.Value;
            }
            else
            {
                var existingChats = await this.GetFullListAsync(c => c.PromptResultId == promptResultId);
                sequence = existingChats.Count > 0 ? existingChats.Max(c => c.Sequence) + 1 : 1;
            }

            foreach (var msg in chatMessages)
            {
                var roleType = msg.Role.ToLower() == "user" 
                    ? ChatRoleType.User 
                    : ChatRoleType.Assistant;

                var chatEntity = new PromptResultChat(
                    promptResultId,
                    roleType,
                    msg.Content ?? string.Empty,
                    sequence++);

                chatEntities.Add(chatEntity);
            }

            await this.SaveObjectListAsync(chatEntities);
            
            await this.SaveChangesAsync();

            var savedDtos = new List<PromptResultChatDto>();
            
            var entityKeys = chatEntities.Select(e => new { e.Sequence, e.RoleType }).ToList();
            
            var allSavedEntities = await this.GetFullListAsync(c => 
                c.PromptResultId == promptResultId);
            
            foreach (var originalEntity in chatEntities)
            {
                var savedEntity = allSavedEntities.FirstOrDefault(e => 
                    e.Sequence == originalEntity.Sequence && 
                    e.RoleType == originalEntity.RoleType);
                
                if (savedEntity != null && savedEntity.Id > 0)
                {
                    savedDtos.Add(new PromptResultChatDto(savedEntity));
                }
                else
                {
                    throw new Exception($"保存对话记录失败：未找到 Sequence={originalEntity.Sequence}, RoleType={originalEntity.RoleType} 的已保存实体，或 ID 为 0。PromptResultId={promptResultId}");
                }
            }

            return savedDtos;
        }

        /// <summary>
        /// Updates the optional user score for one stored prompt-result chat record.
        /// </summary>
        /// <param name="chatId">The chat record identifier.</param>
        /// <param name="score">The score to store, or <see langword="null"/> to clear it.</param>
        /// <returns>The updated chat DTO.</returns>
        /// <exception cref="Exception">Thrown when the chat record does not exist.</exception>
        public async Task<PromptResultChatDto> UpdateUserFeedbackAsync(int chatId, bool? feedback)
        {
            var chat = await this.GetObjectAsync(c => c.Id == chatId)
                ?? throw new Exception($"未找到 ID 为 {chatId} 的对话记录");

            chat.UpdateUserFeedback(feedback);
            await this.SaveObjectAsync(chat);

            return new PromptResultChatDto(chat);
        }

        /// <summary>
        /// Updates the optional user score for one stored prompt-result chat record.
        /// </summary>
        /// <param name="chatId">The chat record identifier.</param>
        /// <param name="score">The score from zero through ten, or <see langword="null"/> to clear it.</param>
        /// <returns>The updated chat DTO.</returns>
        /// <exception cref="Exception">Thrown when the chat record does not exist.</exception>
        public async Task<PromptResultChatDto> UpdateUserScoreAsync(int chatId, decimal? score)
        {
            var chat = await this.GetObjectAsync(c => c.Id == chatId)
                ?? throw new Exception($"未找到 ID 为 {chatId} 的对话记录");

            chat.UpdateUserScore(score);
            await this.SaveObjectAsync(chat);

            return new PromptResultChatDto(chat);
        }

        /// <summary>
        /// Deletes all chat records associated with a prompt result.
        /// </summary>
        /// <param name="promptResultId">The prompt-result identifier whose chat records should be deleted.</param>
        /// <returns>A task that completes after all matching records have been deleted.</returns>
        public async Task DeleteByPromptResultIdAsync(int promptResultId)
        {
            var chatList = await this.GetFullListAsync(c => c.PromptResultId == promptResultId);
            await this.DeleteAllAsync(chatList);
        }


        /// <summary>
        /// Converts stored prompt-result chat DTOs into AI chat messages while preserving
        /// user/assistant role mapping and empty-content behavior.
        /// </summary>
        /// <param name="promptResultDtos">The chat DTOs to convert.</param>
        /// <returns>The converted chat messages, or an empty list when no DTOs are supplied.</returns>
        public List<ChatMessage> GetChatMessageList(List<PromptResultChatDto> promptResultDtos)
        {
            if (promptResultDtos == null || promptResultDtos.Count == 0)
            {
                return new List<ChatMessage>();
            }

            var list = promptResultDtos.Select(z =>
                new ChatMessage(
                    z.RoleType == ChatRoleType.User ? ChatRole.User : ChatRole.Assistant,
                    z.Content ?? string.Empty)
            ).ToList();
            return list;
        }
    }

    /// <summary>Represents one user or assistant message supplied to chat persistence.</summary>
    public class ChatMessageDto
    {
        /// <summary>
        /// role：'user' or 'assistant'
        /// </summary>
        public string Role { get; set; }

        /// <summary>Gets or sets the message content.</summary>
        public string Content { get; set; }
    }
}

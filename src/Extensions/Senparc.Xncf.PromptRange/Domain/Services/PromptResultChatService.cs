using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        /// Get all conversation records based on PromptResultId
        /// </summary>
        /// <param name="promptResultId">ID of PromptResult</param>
        /// <returns></returns>
        public async Task<List<PromptResultChatDto>> GetByPromptResultIdAsync(int promptResultId)
        {
            var chatList = await this.GetFullListAsync(
                c => c.PromptResultId == promptResultId,
                c => c.Sequence,
                OrderingType.Ascending);

            return chatList.Select(c => new PromptResultChatDto(c)).ToList();
        }

        /// <summary>
        /// Add conversation records in batches
        /// </summary>
        /// <param name="promptResultId">ID of PromptResult</param>
        /// <param name="chatMessages">Conversation message list, format: [{role: 'user'|'assistant', content: string}]</param>
        /// <param name="startSequence">Start sequence number, if it is null, start from the existing maximum sequence number + 1, if it is 1, start from the beginning</param>
        /// <returns></returns>
        public async Task<List<PromptResultChatDto>> AddChatMessagesAsync(int promptResultId, List<ChatMessageDto> chatMessages, int? startSequence = null)
        {
            if (chatMessages == null || chatMessages.Count == 0)
            {
                return new List<PromptResultChatDto>();
            }

            var chatEntities = new List<PromptResultChat>();
            int sequence;

            // If a starting sequence number is specified, use it; otherwise start from the highest existing sequence number + 1
            if (startSequence.HasValue)
            {
                sequence = startSequence.Value;
            }
            else
            {
                // Get the largest existing serial number
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
            
            // Force changes to be saved to ensure IDs are updated correctly
            await this.SaveChangesAsync();

            // After saving, Entity Framework will automatically update the entity's ID
            // But to make sure the IDs are correct, we force all newly saved entities to be re-read from the database
            // This avoids the problem of ID 0
            var savedDtos = new List<PromptResultChatDto>();
            
            // Get the Sequence and RoleType combination of all the entities just saved (for exact matching)
            var entityKeys = chatEntities.Select(e => new { e.Sequence, e.RoleType }).ToList();
            
            // Re-read these entities from the database (via PromptResultId, Sequence and RoleType)
            // Note: The same Sequence may have two records, User and Assistant, so we need exact matching
            var allSavedEntities = await this.GetFullListAsync(c => 
                c.PromptResultId == promptResultId);
            
            // Sort and create DTO in original order
            // Need to match the original order of chatEntities to ensure the correct return order
            foreach (var originalEntity in chatEntities)
            {
                // Find the corresponding saved entity through exact matching of Sequence and RoleType
                var savedEntity = allSavedEntities.FirstOrDefault(e => 
                    e.Sequence == originalEntity.Sequence && 
                    e.RoleType == originalEntity.RoleType);
                
                if (savedEntity != null && savedEntity.Id > 0)
                {
                    savedDtos.Add(new PromptResultChatDto(savedEntity));
                }
                else
                {
                    // If not found or ID is still 0, throw an exception instead of returning an invalid DTO
                    // This ensures that problems are discovered and fixed promptly
                    throw new Exception($"保存对话记录失败：未找到 Sequence={originalEntity.Sequence}, RoleType={originalEntity.RoleType} 的已保存实体，或 ID 为 0。PromptResultId={promptResultId}");
                }
            }

            return savedDtos;
        }

        /// <summary>
        ///Update user feedback
        /// </summary>
        /// <param name="chatId">Conversation record ID</param>
        /// <param name="feedback">Like (true), Unlike (false), Cancel feedback (null)</param>
        /// <returns></returns>
        public async Task<PromptResultChatDto> UpdateUserFeedbackAsync(int chatId, bool? feedback)
        {
            var chat = await this.GetObjectAsync(c => c.Id == chatId)
                ?? throw new Exception($"未找到 ID 为 {chatId} 的对话记录");

            chat.UpdateUserFeedback(feedback);
            await this.SaveObjectAsync(chat);

            return new PromptResultChatDto(chat);
        }

        /// <summary>
        ///update user ratings
        /// </summary>
        /// <param name="chatId">Conversation record ID</param>
        /// <param name="score">Score (0-10 points), null means cancel the score</param>
        /// <returns></returns>
        public async Task<PromptResultChatDto> UpdateUserScoreAsync(int chatId, decimal? score)
        {
            var chat = await this.GetObjectAsync(c => c.Id == chatId)
                ?? throw new Exception($"未找到 ID 为 {chatId} 的对话记录");

            chat.UpdateUserScore(score);
            await this.SaveObjectAsync(chat);

            return new PromptResultChatDto(chat);
        }

        /// <summary>
        /// Delete all conversation records of the specified PromptResult
        /// </summary>
        /// <param name="promptResultId">ID of PromptResult</param>
        /// <returns></returns>
        public async Task DeleteByPromptResultIdAsync(int promptResultId)
        {
            var chatList = await this.GetFullListAsync(c => c.PromptResultId == promptResultId);
            await this.DeleteAllAsync(chatList);
        }
    }

    /// <summary>
    /// Conversation message DTO (for batch addition)
    /// </summary>
    public class ChatMessageDto
    {
        /// <summary>
        /// Role: 'user' or 'assistant'
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// message content
        /// </summary>
        public string Content { get; set; }
    }
}

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
        /// 根据 PromptResultId 获取所有对话记录
        /// </summary>
        /// <param name="promptResultId">PromptResult 的 ID</param>
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
        /// 批量添加对话记录
        /// </summary>
        /// <param name="promptResultId">PromptResult 的 ID</param>
        /// <param name="chatMessages">对话消息列表，格式：[{role: 'user'|'assistant', content: string}]</param>
        /// <returns></returns>
        public async Task<List<PromptResultChatDto>> AddChatMessagesAsync(int promptResultId, List<ChatMessageDto> chatMessages)
        {
            if (chatMessages == null || chatMessages.Count == 0)
            {
                return new List<PromptResultChatDto>();
            }

            var chatEntities = new List<PromptResultChat>();
            int sequence = 1;

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

            return chatEntities.Select(c => new PromptResultChatDto(c)).ToList();
        }

        /// <summary>
        /// 更新用户反馈
        /// </summary>
        /// <param name="chatId">对话记录 ID</param>
        /// <param name="feedback">Like（true）、Unlike（false）、取消反馈（null）</param>
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
        /// 更新用户评分
        /// </summary>
        /// <param name="chatId">对话记录 ID</param>
        /// <param name="score">评分（0-10分），null 表示取消评分</param>
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
        /// 删除指定 PromptResult 的所有对话记录
        /// </summary>
        /// <param name="promptResultId">PromptResult 的 ID</param>
        /// <returns></returns>
        public async Task DeleteByPromptResultIdAsync(int promptResultId)
        {
            var chatList = await this.GetFullListAsync(c => c.PromptResultId == promptResultId);
            await this.DeleteAllAsync(chatList);
        }
    }

    /// <summary>
    /// 对话消息 DTO（用于批量添加）
    /// </summary>
    public class ChatMessageDto
    {
        /// <summary>
        /// 角色：'user' 或 'assistant'
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }
    }
}

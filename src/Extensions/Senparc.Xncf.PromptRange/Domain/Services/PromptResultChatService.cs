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
        /// <param name="startSequence">起始序号，如果为 null 则从现有最大序号+1开始，如果为 1 则从头开始</param>
        /// <returns></returns>
        public async Task<List<PromptResultChatDto>> AddChatMessagesAsync(int promptResultId, List<ChatMessageDto> chatMessages, int? startSequence = null)
        {
            if (chatMessages == null || chatMessages.Count == 0)
            {
                return new List<PromptResultChatDto>();
            }

            var chatEntities = new List<PromptResultChat>();
            int sequence;

            // 如果指定了起始序号，使用它；否则从现有最大序号+1开始
            if (startSequence.HasValue)
            {
                sequence = startSequence.Value;
            }
            else
            {
                // 获取现有的最大序号
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
            
            // 强制保存更改，确保 ID 被正确更新
            await this.SaveChangesAsync();

            // 保存后，Entity Framework 会自动更新实体的 ID
            // 但为了确保 ID 正确，我们强制重新从数据库读取所有刚保存的实体
            // 这样可以避免 ID 为 0 的问题
            var savedDtos = new List<PromptResultChatDto>();
            
            // 获取所有刚保存的实体的 Sequence 和 RoleType 组合（用于精确匹配）
            var entityKeys = chatEntities.Select(e => new { e.Sequence, e.RoleType }).ToList();
            
            // 重新从数据库读取这些实体（通过 PromptResultId、Sequence 和 RoleType）
            // 注意：同一个 Sequence 可能有 User 和 Assistant 两条记录，所以我们需要精确匹配
            var allSavedEntities = await this.GetFullListAsync(c => 
                c.PromptResultId == promptResultId);
            
            // 按照原始顺序排序并创建 DTO
            // 需要按照 chatEntities 的原始顺序来匹配，确保返回顺序正确
            foreach (var originalEntity in chatEntities)
            {
                // 通过 Sequence 和 RoleType 精确匹配找到对应的已保存实体
                var savedEntity = allSavedEntities.FirstOrDefault(e => 
                    e.Sequence == originalEntity.Sequence && 
                    e.RoleType == originalEntity.RoleType);
                
                if (savedEntity != null && savedEntity.Id > 0)
                {
                    savedDtos.Add(new PromptResultChatDto(savedEntity));
                }
                else
                {
                    // 如果找不到或 ID 仍然为 0，抛出异常而不是返回无效的 DTO
                    // 这样可以确保问题被及时发现和修复
                    throw new Exception($"保存对话记录失败：未找到 Sequence={originalEntity.Sequence}, RoleType={originalEntity.RoleType} 的已保存实体，或 ID 为 0。PromptResultId={promptResultId}");
                }
            }

            return savedDtos;
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






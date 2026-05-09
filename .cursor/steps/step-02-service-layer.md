# Step 02: 服务层实现

## 📋 任务概述
创建 AdminChat 功能的服务层，包括 Service 层（数据访问）和 AppService 层（API 接口），实现聊天会话管理、消息管理和模块关联管理的业务逻辑。

## 🎯 目标
- ✅ 创建 AdminChatSessionService（会话服务）
- ✅ 创建 AdminChatMessageService（消息服务）
- ✅ 创建 AdminChatSessionModuleService（模块关联服务）
- ✅ 创建 AdminChatAppService（API 接口服务）
- ✅ 实现 AI 对话调用逻辑

## 📂 涉及文件

### 新建文件
1. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Services/AdminChatSessionService.cs`
2. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Services/AdminChatMessageService.cs`
3. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Services/AdminChatSessionModuleService.cs`
4. `tools/NcfSimulatedSite/Senparc.Areas.Admin/OHS/Local/AppService/AdminChatAppService.cs`

## 🔧 实现步骤

### 1. 创建 AdminChatSessionService

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Services/AdminChatSessionService.cs`

**完整代码示例**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Microsoft.EntityFrameworkCore;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// AdminChatSession Service - 管理后台聊天会话服务
    /// </summary>
    public class AdminChatSessionService : ServiceBase<AdminChatSession>
    {
        private readonly AdminChatMessageService _messageService;
        private readonly AdminChatSessionModuleService _sessionModuleService;

        public AdminChatSessionService(
            IRepositoryBase<AdminChatSession> repo,
            IServiceProvider serviceProvider,
            AdminChatMessageService messageService,
            AdminChatSessionModuleService sessionModuleService
            ) : base(repo, serviceProvider)
        {
            _messageService = messageService;
            _sessionModuleService = sessionModuleService;
        }

        /// <summary>
        /// 创建新的聊天会话
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="initialMessage">初始消息（用于生成标题）</param>
        /// <returns></returns>
        public async Task<AdminChatSessionDto> CreateSessionAsync(int userId, string initialMessage = null)
        {
            // 从初始消息提取标题（取前50个字符）
            var title = string.IsNullOrEmpty(initialMessage) 
                ? "新对话" 
                : (initialMessage.Length > 50 ? initialMessage.Substring(0, 50) + "..." : initialMessage);

            var session = new AdminChatSession(title, userId);
            await base.SaveObjectAsync(session);

            return new AdminChatSessionDto(session);
        }

        /// <summary>
        /// 获取用户的所有会话列表（分页）
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pageIndex">页码（从1开始）</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="includeArchived">是否包含已归档的会话</param>
        /// <returns></returns>
        public async Task<(List<AdminChatSessionDto> Sessions, int TotalCount)> GetUserSessionsAsync(
            int userId, 
            int pageIndex = 1, 
            int pageSize = 20, 
            bool includeArchived = false)
        {
            var query = base.GetFullList(z => z.UserId == userId && z.Status != ChatSessionStatus.Deleted, null);

            if (!includeArchived)
            {
                query = query.Where(z => z.Status == ChatSessionStatus.Active);
            }

            // 按最后消息时间倒序
            var orderedQuery = query.OrderByDescending(z => z.LastMessageTime);

            var totalCount = await orderedQuery.CountAsync();
            var sessions = await orderedQuery
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var sessionDtos = new List<AdminChatSessionDto>();
            
            foreach (var session in sessions)
            {
                var dto = new AdminChatSessionDto(session);
                
                // 获取最后一条消息作为预览
                var lastMessage = await _messageService.GetLastMessageAsync(session.Id);
                if (lastMessage != null)
                {
                    dto.LastMessagePreview = lastMessage.Content?.Length > 100 
                        ? lastMessage.Content.Substring(0, 100) + "..." 
                        : lastMessage.Content;
                }
                
                // 获取关联的模块
                dto.Modules = await _sessionModuleService.GetSessionModulesAsync(session.Id);
                
                sessionDtos.Add(dto);
            }

            return (sessionDtos, totalCount);
        }

        /// <summary>
        /// 根据ID获取会话详情
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <returns></returns>
        public async Task<AdminChatSessionDto> GetSessionByIdAsync(int sessionId)
        {
            var session = await base.GetObjectAsync(z => z.Id == sessionId);
            if (session == null)
            {
                return null;
            }

            var dto = new AdminChatSessionDto(session);
            dto.Modules = await _sessionModuleService.GetSessionModulesAsync(sessionId);

            return dto;
        }

        /// <summary>
        /// 更新会话标题
        /// </summary>
        public async Task<bool> UpdateSessionTitleAsync(int sessionId, string title)
        {
            var session = await base.GetObjectAsync(z => z.Id == sessionId);
            if (session == null)
            {
                return false;
            }

            session.UpdateTitle(title);
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        /// 归档会话
        /// </summary>
        public async Task<bool> ArchiveSessionAsync(int sessionId)
        {
            var session = await base.GetObjectAsync(z => z.Id == sessionId);
            if (session == null)
            {
                return false;
            }

            session.Archive();
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        /// 删除会话（软删除）
        /// </summary>
        public async Task<bool> DeleteSessionAsync(int sessionId)
        {
            var session = await base.GetObjectAsync(z => z.Id == sessionId);
            if (session == null)
            {
                return false;
            }

            session.Delete();
            await base.SaveObjectAsync(session);
            return true;
        }

        /// <summary>
        /// 更新会话的最后消息时间
        /// </summary>
        public async Task UpdateLastMessageTimeAsync(int sessionId)
        {
            var session = await base.GetObjectAsync(z => z.Id == sessionId);
            if (session != null)
            {
                session.UpdateLastMessageTime();
                await base.SaveObjectAsync(session);
            }
        }
    }
}
```

**关键技术点**：
- 继承自 `ServiceBase<AdminChatSession>`，自动获得基础 CRUD 方法
- 注入其他服务以实现关联查询
- 使用异步方法提升性能
- 实现分页查询，避免一次性加载大量数据

---

### 2. 创建 AdminChatMessageService

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Services/AdminChatMessageService.cs`

**完整代码示例**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Microsoft.EntityFrameworkCore;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// AdminChatMessage Service - 管理后台聊天消息服务
    /// </summary>
    public class AdminChatMessageService : ServiceBase<AdminChatMessage>
    {
        public AdminChatMessageService(
            IRepositoryBase<AdminChatMessage> repo,
            IServiceProvider serviceProvider
            ) : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// 根据会话ID获取所有消息（按序号排序）
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <returns></returns>
        public async Task<List<AdminChatMessageDto>> GetSessionMessagesAsync(int sessionId)
        {
            var messages = await this.GetFullListAsync(
                z => z.SessionId == sessionId,
                z => z.Sequence,
                OrderingType.Ascending);

            return messages.Select(m => new AdminChatMessageDto(m)).ToList();
        }

        /// <summary>
        /// 获取会话的最后一条消息
        /// </summary>
        public async Task<AdminChatMessageDto> GetLastMessageAsync(int sessionId)
        {
            var message = await base.GetFullList(z => z.SessionId == sessionId, null)
                .OrderByDescending(z => z.Sequence)
                .FirstOrDefaultAsync();

            return message != null ? new AdminChatMessageDto(message) : null;
        }

        /// <summary>
        /// 添加用户消息
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="content">消息内容</param>
        /// <returns></returns>
        public async Task<AdminChatMessageDto> AddUserMessageAsync(int sessionId, string content)
        {
            var nextSequence = await GetNextSequenceAsync(sessionId);
            var message = new AdminChatMessage(sessionId, ChatRoleType.User, content, nextSequence);
            
            await base.SaveObjectAsync(message);
            return new AdminChatMessageDto(message);
        }

        /// <summary>
        /// 添加 AI 助手消息
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="content">消息内容</param>
        /// <param name="modelIdentifier">AI模型标识</param>
        /// <returns></returns>
        public async Task<AdminChatMessageDto> AddAssistantMessageAsync(int sessionId, string content, string modelIdentifier = null)
        {
            var nextSequence = await GetNextSequenceAsync(sessionId);
            var message = new AdminChatMessage(sessionId, ChatRoleType.Assistant, content, nextSequence, modelIdentifier);
            
            await base.SaveObjectAsync(message);
            return new AdminChatMessageDto(message);
        }

        /// <summary>
        /// 批量添加消息（用于对话）
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="userMessage">用户消息</param>
        /// <param name="assistantMessage">AI回复</param>
        /// <param name="modelIdentifier">AI模型标识</param>
        /// <returns></returns>
        public async Task<(AdminChatMessageDto UserMsg, AdminChatMessageDto AssistantMsg)> AddConversationPairAsync(
            int sessionId, 
            string userMessage, 
            string assistantMessage,
            string modelIdentifier = null)
        {
            var startSequence = await GetNextSequenceAsync(sessionId);

            var userMsg = new AdminChatMessage(sessionId, ChatRoleType.User, userMessage, startSequence);
            var assistantMsg = new AdminChatMessage(sessionId, ChatRoleType.Assistant, assistantMessage, startSequence + 1, modelIdentifier);

            await base.SaveObjectAsync(userMsg);
            await base.SaveObjectAsync(assistantMsg);

            return (new AdminChatMessageDto(userMsg), new AdminChatMessageDto(assistantMsg));
        }

        /// <summary>
        /// 设置消息的用户反馈
        /// </summary>
        public async Task<bool> SetMessageFeedbackAsync(int messageId, bool isLike)
        {
            var message = await base.GetObjectAsync(z => z.Id == messageId);
            if (message == null || message.RoleType != ChatRoleType.Assistant)
            {
                return false; // 只能对 AI 回复进行反馈
            }

            message.SetUserFeedback(isLike);
            await base.SaveObjectAsync(message);
            return true;
        }

        /// <summary>
        /// 获取会话中的下一个序号
        /// </summary>
        private async Task<int> GetNextSequenceAsync(int sessionId)
        {
            var maxSequence = await base.GetFullList(z => z.SessionId == sessionId, null)
                .MaxAsync(z => (int?)z.Sequence);

            return (maxSequence ?? 0) + 1;
        }

        /// <summary>
        /// 获取会话的消息数量
        /// </summary>
        public async Task<int> GetMessageCountAsync(int sessionId)
        {
            return await base.GetFullList(z => z.SessionId == sessionId, null).CountAsync();
        }

        /// <summary>
        /// 删除会话的所有消息（物理删除，谨慎使用）
        /// </summary>
        public async Task<int> DeleteSessionMessagesAsync(int sessionId)
        {
            var messages = await base.GetFullListAsync(z => z.SessionId == sessionId);
            var count = messages.Count;
            
            foreach (var message in messages)
            {
                await base.DeleteObjectAsync(message);
            }
            
            return count;
        }
    }
}
```

**关键技术点**：
- 继承自 `ServiceBase<AdminChatMessage>`
- 实现消息序号自动递增
- 支持批量添加对话对（用户+AI回复）
- 提供消息反馈功能

---

### 3. 创建 AdminChatSessionModuleService

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Services/AdminChatSessionModuleService.cs`

**完整代码示例**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Microsoft.EntityFrameworkCore;

namespace Senparc.Areas.Admin.Domain.Services
{
    /// <summary>
    /// AdminChatSessionModule Service - 会话-模块关联服务
    /// </summary>
    public class AdminChatSessionModuleService : ServiceBase<AdminChatSessionModule>
    {
        public AdminChatSessionModuleService(
            IRepositoryBase<AdminChatSessionModule> repo,
            IServiceProvider serviceProvider
            ) : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// 为会话添加模块
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="moduleUid">模块UID</param>
        /// <param name="moduleName">模块名称</param>
        /// <param name="moduleVersion">模块版本</param>
        /// <returns></returns>
        public async Task<AdminChatSessionModuleDto> AddModuleToSessionAsync(
            int sessionId, 
            string moduleUid, 
            string moduleName, 
            string moduleVersion = null)
        {
            // 检查是否已存在
            var existing = await base.GetObjectAsync(z => z.SessionId == sessionId && z.XncfModuleUid == moduleUid);
            if (existing != null)
            {
                return new AdminChatSessionModuleDto(existing); // 已存在，返回现有记录
            }

            var sessionModule = new AdminChatSessionModule(sessionId, moduleUid, moduleName, moduleVersion);
            await base.SaveObjectAsync(sessionModule);

            return new AdminChatSessionModuleDto(sessionModule);
        }

        /// <summary>
        /// 批量添加模块到会话
        /// </summary>
        public async Task<List<AdminChatSessionModuleDto>> AddModulesToSessionAsync(
            int sessionId, 
            List<(string Uid, string Name, string Version)> modules)
        {
            var results = new List<AdminChatSessionModuleDto>();

            foreach (var (uid, name, version) in modules)
            {
                var result = await AddModuleToSessionAsync(sessionId, uid, name, version);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// 获取会话关联的所有模块
        /// </summary>
        public async Task<List<AdminChatSessionModuleDto>> GetSessionModulesAsync(int sessionId)
        {
            var modules = await base.GetFullListAsync(
                z => z.SessionId == sessionId,
                z => z.AddedTime,
                Ncf.Core.Enums.OrderingType.Ascending);

            return modules.Select(m => new AdminChatSessionModuleDto(m)).ToList();
        }

        /// <summary>
        /// 从会话中移除模块
        /// </summary>
        public async Task<bool> RemoveModuleFromSessionAsync(int sessionId, string moduleUid)
        {
            var sessionModule = await base.GetObjectAsync(z => z.SessionId == sessionId && z.XncfModuleUid == moduleUid);
            if (sessionModule == null)
            {
                return false;
            }

            await base.DeleteObjectAsync(sessionModule);
            return true;
        }

        /// <summary>
        /// 清除会话的所有模块
        /// </summary>
        public async Task<int> ClearSessionModulesAsync(int sessionId)
        {
            var modules = await base.GetFullListAsync(z => z.SessionId == sessionId);
            var count = modules.Count;

            foreach (var module in modules)
            {
                await base.DeleteObjectAsync(module);
            }

            return count;
        }
    }
}
```

**关键技术点**：
- 实现模块与会话的关联管理
- 支持批量操作
- 自动去重（检查是否已存在）

---

### 4. 创建 AdminChatAppService（API 接口层）

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/OHS/Local/AppService/AdminChatAppService.cs`

**完整代码示例**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Ncf.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Senparc.Xncf.AIKernel.Domain.Services;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    /// <summary>
    /// 管理后台聊天 AppService
    /// </summary>
    public class AdminChatAppService : AppServiceBase
    {
        private readonly AdminChatSessionService _sessionService;
        private readonly AdminChatMessageService _messageService;
        private readonly AdminChatSessionModuleService _sessionModuleService;
        private readonly AiModelService _aiModelService;
        private readonly ILogger<AdminChatAppService> _logger;

        public AdminChatAppService(
            IServiceProvider serviceProvider,
            AdminChatSessionService sessionService,
            AdminChatMessageService messageService,
            AdminChatSessionModuleService sessionModuleService,
            AiModelService aiModelService,
            ILogger<AdminChatAppService> logger) : base(serviceProvider)
        {
            _sessionService = sessionService;
            _messageService = messageService;
            _sessionModuleService = sessionModuleService;
            _aiModelService = aiModelService;
            _logger = logger;
        }

        /// <summary>
        /// 创建新的聊天会话
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AdminChatSessionDto>> CreateSession([FromBody] CreateSessionRequest request)
        {
            return await this.GetResponseAsync<AdminChatSessionDto>(
                async (response, logger) =>
                {
                    var session = await _sessionService.CreateSessionAsync(request.UserId, request.InitialMessage);
                    return session;
                });
        }

        /// <summary>
        /// 获取用户的会话列表
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<SessionListResponse>> GetUserSessions(
            [FromQuery] int userId, 
            [FromQuery] int pageIndex = 1, 
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeArchived = false)
        {
            return await this.GetResponseAsync<SessionListResponse>(
                async (response, logger) =>
                {
                    var (sessions, totalCount) = await _sessionService.GetUserSessionsAsync(userId, pageIndex, pageSize, includeArchived);
                    
                    return new SessionListResponse
                    {
                        Sessions = sessions,
                        TotalCount = totalCount,
                        PageIndex = pageIndex,
                        PageSize = pageSize
                    };
                });
        }

        /// <summary>
        /// 获取会话的消息历史
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<List<AdminChatMessageDto>>> GetSessionMessages([FromQuery] int sessionId)
        {
            return await this.GetResponseAsync<List<AdminChatMessageDto>>(
                async (response, logger) =>
                {
                    return await _messageService.GetSessionMessagesAsync(sessionId);
                });
        }

        /// <summary>
        /// 发送消息并获取 AI 回复
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<ChatResponse>> SendMessage([FromBody] ChatMessageInputDto request)
        {
            return await this.GetResponseAsync<ChatResponse>(
                async (response, logger) =>
                {
                    // 1. 保存用户消息
                    var userMessage = await _messageService.AddUserMessageAsync(request.SessionId, request.Content);

                    // 2. 获取历史消息上下文
                    var historyMessages = await _messageService.GetSessionMessagesAsync(request.SessionId);

                    // 3. 获取关联的模块信息（用于上下文）
                    var sessionModules = await _sessionModuleService.GetSessionModulesAsync(request.SessionId);
                    
                    // 4. 构建 AI 对话上下文
                    string systemPrompt = BuildSystemPrompt(sessionModules);
                    
                    // 5. 调用 AI 接口（这里需要集成 AIKernel 模块）
                    // TODO: 集成 AIKernel 的 AI 对话接口
                    var aiResponse = await CallAiServiceAsync(historyMessages, request.Content, systemPrompt);

                    // 6. 保存 AI 回复
                    var assistantMessage = await _messageService.AddAssistantMessageAsync(
                        request.SessionId, 
                        aiResponse.Content, 
                        aiResponse.ModelIdentifier);

                    // 7. 更新会话的最后消息时间
                    await _sessionService.UpdateLastMessageTimeAsync(request.SessionId);

                    return new ChatResponse
                    {
                        UserMessage = userMessage,
                        AssistantMessage = assistantMessage,
                        SessionId = request.SessionId
                    };
                });
        }

        /// <summary>
        /// 添加模块到会话
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AdminChatSessionModuleDto>> AddModuleToSession([FromBody] AddModuleRequest request)
        {
            return await this.GetResponseAsync<AdminChatSessionModuleDto>(
                async (response, logger) =>
                {
                    return await _sessionModuleService.AddModuleToSessionAsync(
                        request.SessionId, 
                        request.ModuleUid, 
                        request.ModuleName, 
                        request.ModuleVersion);
                });
        }

        /// <summary>
        /// 删除会话
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Delete)]
        public async Task<AppResponseBase<bool>> DeleteSession([FromQuery] int sessionId)
        {
            return await this.GetResponseAsync<bool>(
                async (response, logger) =>
                {
                    return await _sessionService.DeleteSessionAsync(sessionId);
                });
        }

        /// <summary>
        /// 构建系统提示词（根据关联的模块）
        /// </summary>
        private string BuildSystemPrompt(List<AdminChatSessionModuleDto> modules)
        {
            if (modules == null || modules.Count == 0)
            {
                return "你是 NeuCharFramework 管理后台的 AI 助手，帮助用户管理和使用系统功能。";
            }

            var moduleNames = string.Join("、", modules.Select(m => m.ModuleName));
            return $"你是 NeuCharFramework 管理后台的 AI 助手。当前对话上下文包含以下模块：{moduleNames}。请根据这些模块的功能帮助用户。";
        }

        /// <summary>
        /// 调用 AI 服务（简化版本，实际需要集成 AIKernel）
        /// </summary>
        private async Task<(string Content, string ModelIdentifier)> CallAiServiceAsync(
            List<AdminChatMessageDto> historyMessages, 
            string userMessage, 
            string systemPrompt)
        {
            // TODO: 实际集成 AIKernel 模块的 AI 接口
            // 这里提供一个简化的实现框架

            try
            {
                // 获取默认 AI 模型
                var aiModel = await _aiModelService.GetObjectAsync(z => z.IsDefault == true);
                if (aiModel == null)
                {
                    throw new NcfExceptionBase("未配置默认 AI 模型，请先在 AI 模型管理中配置。");
                }

                // 构建对话历史
                var chatHistory = new List<object>();
                foreach (var msg in historyMessages.TakeLast(10)) // 只取最近10条
                {
                    chatHistory.Add(new 
                    { 
                        role = msg.RoleType == ChatRoleType.User ? "user" : "assistant",
                        content = msg.Content 
                    });
                }

                // TODO: 调用 AIKernel 的接口
                // 示例代码（需要根据实际 AIKernel 接口调整）：
                /*
                var handler = serviceProvider.GetService<IWantToChat>();
                var iWantToRun = handler.ChatConfig(
                    new Dictionary<string, object>(),
                    userId: $"AdminChat_{sessionId}",
                    chatSystemMessage: systemPrompt,
                    senparcAiSetting: aiModel.ToSenparcAiSetting()
                );
                var aiResult = await handler.ChatAsync(iWantToRun, userMessage);
                return (aiResult.OutputString, aiModel.Id.ToString());
                */

                // 临时返回（实际开发时替换为真实 AI 调用）
                return ($"AI 回复：{userMessage}（这是临时回复，需要集成 AIKernel）", aiModel.Id.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调用 AI 服务失败");
                return ("抱歉，AI 服务暂时不可用，请稍后再试。", "error");
            }
        }
    }

    #region Request/Response DTOs

    /// <summary>
    /// 创建会话请求
    /// </summary>
    public class CreateSessionRequest
    {
        public int UserId { get; set; }
        public string InitialMessage { get; set; }
    }

    /// <summary>
    /// 添加模块请求
    /// </summary>
    public class AddModuleRequest
    {
        public int SessionId { get; set; }
        public string ModuleUid { get; set; }
        public string ModuleName { get; set; }
        public string ModuleVersion { get; set; }
    }

    /// <summary>
    /// 会话列表响应
    /// </summary>
    public class SessionListResponse
    {
        public List<AdminChatSessionDto> Sessions { get; set; }
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    /// <summary>
    /// 对话响应
    /// </summary>
    public class ChatResponse
    {
        public AdminChatMessageDto UserMessage { get; set; }
        public AdminChatMessageDto AssistantMessage { get; set; }
        public int SessionId { get; set; }
    }

    #endregion
}
```

**关键技术点**：
- AppService 是 API 接口层，使用 `[ApiBind]` 特性标记
- 继承自 `AppServiceBase`
- 使用 `GetResponseAsync` 统一处理异常和响应格式
- AI 调用逻辑在 AppService 层实现
- 提供 Request/Response DTO 用于 API 交互

---

## ✅ 验收标准

### 功能验收
- [ ] AdminChatSessionService 可以创建、查询、更新、删除会话
- [ ] AdminChatMessageService 可以添加消息、查询历史、设置反馈
- [ ] AdminChatSessionModuleService 可以管理会话-模块关联
- [ ] AdminChatAppService 提供完整的 API 接口
- [ ] 服务之间的依赖注入正确

### 技术验收
- [ ] 所有 Service 继承自 `ServiceBase<T>`
- [ ] AppService 继承自 `AppServiceBase`
- [ ] 使用 `[ApiBind]` 特性标记 API 方法
- [ ] 异步方法使用 `async/await`
- [ ] 事务处理正确（SaveObjectAsync）
- [ ] 错误处理完善（try-catch）

### 质量验收
- [ ] 代码注释清晰完整
- [ ] 符合 NCF 框架规范
- [ ] 无编译错误
- [ ] 无 linting 警告
- [ ] 日志记录关键操作

---

## 📝 注意事项

⚠️ **重要**：
- Service 层只处理数据访问和业务逻辑，不涉及 HTTP 请求
- AppService 层是 API 接口，使用 `[ApiBind]` 和 `GetResponseAsync`
- AI 调用逻辑需要实际集成 AIKernel 模块，当前提供的是框架代码
- 所有异步方法要使用 `Task` 返回类型
- 依赖注入要在构造函数中声明

⚠️ **AI 集成说明**：
- `CallAiServiceAsync` 方法需要根据实际的 AIKernel 接口调整
- 可以参考 `Senparc.Xncf.PromptRange` 中的 AI 调用示例
- 需要处理 AI 服务不可用的异常情况

---

## 🔗 相关任务
- 上一步：[Step 01: 数据模型层设计](./step-01-data-models.md)
- 下一步：[Step 03: 首页UI改版](./step-03-homepage-ui.md)
- 关联文档：[scratchpad.md](../scratchpad.md)

---

## 📊 进度追踪

**任务拆解**：
- [ ] **[TASK-05]** 创建 AdminChatSessionService (0.8h)
- [ ] **[TASK-06]** 创建 AdminChatMessageService (0.7h)
- [ ] **[TASK-07]** 创建 AdminChatSessionModuleService (0.5h)
- [ ] **[TASK-08]** 创建 AdminChatAppService API 接口 (0.5h)

**预计总耗时**: 2.5 小时

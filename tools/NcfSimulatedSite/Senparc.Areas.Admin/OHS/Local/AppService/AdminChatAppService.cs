using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    /// <summary>
    /// AdminChatAppService：管理后台聊天功能 API 服务
    /// 支持 Cookie 和 JWT 两种认证方式
    /// </summary>
    [AdminOrJwtAuthorize("AdminOnly")]
    public class AdminChatAppService : LocalAppServiceBase
    {
        private readonly AdminChatSessionService _sessionService;
        private readonly AdminChatMessageService _messageService;
        private readonly AdminChatSessionModuleService _sessionModuleService;

        public AdminChatAppService(
            IServiceProvider serviceProvider,
            AdminChatSessionService sessionService,
            AdminChatMessageService messageService,
            AdminChatSessionModuleService sessionModuleService) : base(serviceProvider)
        {
            _sessionService = sessionService;
            _messageService = messageService;
            _sessionModuleService = sessionModuleService;
        }

        #region 会话管理

        /// <summary>
        /// 创建新的聊天会话
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<CreateSessionResponse>> CreateSessionAsync([FromBody] CreateChatSessionInputDto request)
        {
            return await this.GetResponseAsync<AppResponseBase<CreateSessionResponse>, CreateSessionResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase("用户未登录");
                }

                var title = string.IsNullOrEmpty(request.InitialMessage) 
                    ? "新对话" 
                    : (request.InitialMessage.Length > 50 ? request.InitialMessage.Substring(0, 50) + "..." : request.InitialMessage);

                var session = await _sessionService.CreateSessionAsync(title, userId);

                if (request.ModuleUids != null && request.ModuleUids.Any())
                {
                    var modules = new List<(string uid, string name, string version)>();
                    foreach (var uid in request.ModuleUids)
                    {
                        modules.Add((uid, uid, ""));
                    }
                    await _sessionModuleService.AddModulesToSessionAsync(session.Id, modules);
                }

                if (!string.IsNullOrEmpty(request.InitialMessage))
                {
                    await _messageService.AddMessageAsync(
                        session.Id,
                        ChatMessageRoleType.User,
                        request.InitialMessage);

                    var aiResponse = await GenerateAIResponseAsync(session.Id, request.InitialMessage);
                    await _messageService.AddMessageAsync(
                        session.Id,
                        ChatMessageRoleType.Assistant,
                        aiResponse,
                        "placeholder-model");
                }

                logger.Append($"创建会话: SessionId={session.Id}, UserId={userId}");

                return new CreateSessionResponse
                {
                    SessionId = session.Id,
                    Title = session.Title
                };
            });
        }

        /// <summary>
        /// 获取用户的会话列表
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetSessionListResponse>> GetSessionListAsync(int pageIndex = 1, int pageSize = 20)
        {
            return await this.GetResponseAsync<AppResponseBase<GetSessionListResponse>, GetSessionListResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase("用户未登录");
                }

                var (sessions, totalCount) = await _sessionService.GetUserActiveSessionsAsync(userId, pageIndex, pageSize);

                return new GetSessionListResponse
                {
                    Sessions = sessions.Select(AdminChatSessionDto.CreateFromEntity).ToList(),
                    TotalCount = totalCount
                };
            });
        }

        /// <summary>
        /// 获取会话详情（包含消息和模块）
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetSessionDetailResponse>> GetSessionDetailAsync(int sessionId)
        {
            return await this.GetResponseAsync<AppResponseBase<GetSessionDetailResponse>, GetSessionDetailResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase("用户未登录");
                }

                var session = await _sessionService.GetSessionByIdAsync(sessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase("会话不存在或无权访问");
                }

                var (messages, _) = await _messageService.GetSessionMessagesAsync(sessionId);
                var modules = await _sessionModuleService.GetSessionModulesAsync(sessionId);

                var sessionDto = AdminChatSessionDto.CreateFromEntity(session);
                sessionDto.Messages = messages.Select(AdminChatMessageDto.CreateFromEntity).ToList();
                sessionDto.Modules = modules.Select(AdminChatSessionModuleDto.CreateFromEntity).ToList();

                return new GetSessionDetailResponse
                {
                    Session = sessionDto
                };
            });
        }

        /// <summary>
        /// 删除会话
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Delete)]
        public async Task<StringAppResponse> DeleteSessionAsync(int sessionId)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase("用户未登录");
                }

                var success = await _sessionService.DeleteSessionAsync(sessionId, userId);
                if (!success)
                {
                    throw new NcfExceptionBase("会话不存在或无权删除");
                }

                logger.Append($"删除会话: SessionId={sessionId}, UserId={userId}");
                return "删除成功";
            });
        }

        #endregion

        #region 消息管理

        /// <summary>
        /// 发送消息
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<SendMessageResponse>> SendMessageAsync([FromBody] ChatMessageInputDto request)
        {
            return await this.GetResponseAsync<AppResponseBase<SendMessageResponse>, SendMessageResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase("用户未登录");
                }

                var session = await _sessionService.GetSessionByIdAsync(request.SessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase("会话不存在或无权访问");
                }

                var userMessage = await _messageService.AddMessageAsync(
                    request.SessionId,
                    ChatMessageRoleType.User,
                    request.Content);

                await _sessionService.UpdateLastMessageTimeAsync(request.SessionId);

                var aiResponse = await GenerateAIResponseAsync(request.SessionId, request.Content);

                var assistantMessage = await _messageService.AddMessageAsync(
                    request.SessionId,
                    ChatMessageRoleType.Assistant,
                    aiResponse,
                    "placeholder-model");

                logger.Append($"发送消息: SessionId={request.SessionId}, MessageId={userMessage.Id}");

                return new SendMessageResponse
                {
                    UserMessage = AdminChatMessageDto.CreateFromEntity(userMessage),
                    AssistantMessage = AdminChatMessageDto.CreateFromEntity(assistantMessage)
                };
            });
        }

        /// <summary>
        /// 获取会话的消息列表
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetMessagesResponse>> GetMessagesAsync(int sessionId, int pageIndex = 0, int pageSize = 50)
        {
            return await this.GetResponseAsync<AppResponseBase<GetMessagesResponse>, GetMessagesResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase("用户未登录");
                }

                var session = await _sessionService.GetSessionByIdAsync(sessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase("会话不存在或无权访问");
                }

                var (messages, totalCount) = await _messageService.GetSessionMessagesAsync(sessionId, pageIndex, pageSize);

                return new GetMessagesResponse
                {
                    Messages = messages.Select(AdminChatMessageDto.CreateFromEntity).ToList(),
                    TotalCount = totalCount
                };
            });
        }

        /// <summary>
        /// 设置消息反馈
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Put)]
        public async Task<StringAppResponse> SetMessageFeedbackAsync(int messageId, MessageFeedbackType feedback)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var success = await _messageService.SetMessageFeedbackAsync(messageId, feedback);
                if (!success)
                {
                    throw new NcfExceptionBase("消息不存在");
                }

                logger.Append($"设置反馈: MessageId={messageId}, Feedback={feedback}");
                return "反馈成功";
            });
        }

        #endregion

        #region 模块管理

        /// <summary>
        /// 添加模块到会话
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> AddModulesToSessionAsync([FromBody] AddModulesRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase("用户未登录");
                }

                var session = await _sessionService.GetSessionByIdAsync(request.SessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase("会话不存在或无权访问");
                }

                var modules = request.Modules.Select(m => (m.Uid, m.Name, m.Version ?? "")).ToList();
                await _sessionModuleService.AddModulesToSessionAsync(request.SessionId, modules);

                logger.Append($"添加模块: SessionId={request.SessionId}, Count={modules.Count}");
                return "添加成功";
            });
        }

        /// <summary>
        /// 获取会话的模块列表
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetSessionModulesResponse>> GetSessionModulesAsync(int sessionId)
        {
            return await this.GetResponseAsync<AppResponseBase<GetSessionModulesResponse>, GetSessionModulesResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase("用户未登录");
                }

                var session = await _sessionService.GetSessionByIdAsync(sessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase("会话不存在或无权访问");
                }

                var modules = await _sessionModuleService.GetSessionModulesAsync(sessionId);

                return new GetSessionModulesResponse
                {
                    Modules = modules.Select(AdminChatSessionModuleDto.CreateFromEntity).ToList()
                };
            });
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 生成 AI 响应（占位实现，实际应调用 AIKernel）
        /// </summary>
        private async Task<string> GenerateAIResponseAsync(int sessionId, string userMessage)
        {
            await Task.Delay(100);

            var modules = await _sessionModuleService.GetSessionModulesAsync(sessionId);
            var moduleContext = modules.Any() 
                ? $"\n\n[当前会话已加载模块: {string.Join(", ", modules.Select(m => m.ModuleName))}]" 
                : "";

            return $"这是一个占位响应。您的问题是：{userMessage}{moduleContext}\n\n" +
                   $"实际实现时，这里将调用 AIKernel 模块进行智能回复。" +
                   $"\n\n**提示**：请在此处集成 AIKernel 的 `GenerateResponseAsync` 方法，并传递会话上下文和模块信息。";
        }

        #endregion
    }

    #region 请求和响应模型

    /// <summary>
    /// 创建会话响应
    /// </summary>
    public class CreateSessionResponse
    {
        public int SessionId { get; set; }
        public string Title { get; set; }
    }

    /// <summary>
    /// 获取会话列表响应
    /// </summary>
    public class GetSessionListResponse
    {
        public List<AdminChatSessionDto> Sessions { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// 获取会话详情响应
    /// </summary>
    public class GetSessionDetailResponse
    {
        public AdminChatSessionDto Session { get; set; }
    }

    /// <summary>
    /// 发送消息响应
    /// </summary>
    public class SendMessageResponse
    {
        public AdminChatMessageDto UserMessage { get; set; }
        public AdminChatMessageDto AssistantMessage { get; set; }
    }

    /// <summary>
    /// 获取消息列表响应
    /// </summary>
    public class GetMessagesResponse
    {
        public List<AdminChatMessageDto> Messages { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// 添加模块到会话请求
    /// </summary>
    public class AddModulesRequest
    {
        public int SessionId { get; set; }
        public List<ModuleInfo> Modules { get; set; }
    }

    /// <summary>
    /// 模块信息
    /// </summary>
    public class ModuleInfo
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
    }

    /// <summary>
    /// 获取会话模块列表响应
    /// </summary>
    public class GetSessionModulesResponse
    {
        public List<AdminChatSessionModuleDto> Modules { get; set; }
    }

    #endregion
}

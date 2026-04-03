using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Config;
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
    ///AdminChatAppService: Manage background chat function API service
    ///Supports two authentication methods: Cookie and JWT
    /// </summary>
    [AdminOrJwtAuthorize("AdminOnly")]
    public class AdminChatAppService : LocalAppServiceBase
    {
        private readonly AdminChatSessionService _sessionService;
        private readonly AdminChatMessageService _messageService;
        private readonly AdminChatSessionModuleService _sessionModuleService;
        private readonly AdminChatAiService _chatAiService;

        public AdminChatAppService(
            IServiceProvider serviceProvider,
            AdminChatSessionService sessionService,
            AdminChatMessageService messageService,
            AdminChatSessionModuleService sessionModuleService,
            AdminChatAiService chatAiService) : base(serviceProvider)
        {
            _sessionService = sessionService;
            _messageService = messageService;
            _sessionModuleService = sessionModuleService;
            _chatAiService = chatAiService;
        }

        #region 会话管理

        /// <summary>
        ///Create new chat session
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

                var moduleUidSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (request.ModuleUids != null && request.ModuleUids.Any())
                {
                    foreach (var uid in request.ModuleUids.Where(z => !string.IsNullOrWhiteSpace(z)))
                    {
                        moduleUidSet.Add(uid);
                    }
                }

                moduleUidSet.Add(SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID);

                var modules = new List<(string uid, string name, string version)>();
                foreach (var uid in moduleUidSet)
                {
                    var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == uid);
                    modules.Add((uid, register?.Name ?? uid, register?.Version ?? ""));
                }

                await _sessionModuleService.AddModulesToSessionAsync(session.Id, modules);

                if (!string.IsNullOrEmpty(request.InitialMessage))
                {
                    await _messageService.AddMessageAsync(
                        session.Id,
                        ChatMessageRoleType.User,
                        request.InitialMessage);

                    var (aiResponse, modelIdentifier) = await _chatAiService.GenerateResponseAsync(session.Id, userId, request.InitialMessage);
                    await _messageService.AddMessageAsync(
                        session.Id,
                        ChatMessageRoleType.Assistant,
                        aiResponse,
                        modelIdentifier);
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
        /// Get the user's session list
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
        /// Get session details (including messages and modules)
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
                sessionDto.Modules = modules.Select(z => MapModuleDtoWithRegisterInfo(AdminChatSessionModuleDto.CreateFromEntity(z))).ToList();

                return new GetSessionDetailResponse
                {
                    Session = sessionDto
                };
            });
        }

        /// <summary>
        ///delete session
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
        ///send message
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

                var (aiResponse, modelIdentifier) = await _chatAiService.GenerateResponseAsync(request.SessionId, userId, request.Content);

                var assistantMessage = await _messageService.AddMessageAsync(
                    request.SessionId,
                    ChatMessageRoleType.Assistant,
                    aiResponse,
                    modelIdentifier);

                logger.Append($"发送消息: SessionId={request.SessionId}, MessageId={userMessage.Id}");

                return new SendMessageResponse
                {
                    UserMessage = AdminChatMessageDto.CreateFromEntity(userMessage),
                    AssistantMessage = AdminChatMessageDto.CreateFromEntity(assistantMessage)
                };
            });
        }

        /// <summary>
        /// Get the message list of the session
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
        ///Set message feedback
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

        /// <summary>
        ///Delete messages in batches
        /// </summary>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Delete)]
        public async Task<StringAppResponse> DeleteMessagesAsync(int sessionId, string messageIds)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
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

                var parsedMessageIds = (messageIds ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id, out var value) ? value : 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                if (!parsedMessageIds.Any())
                {
                    throw new NcfExceptionBase("请至少选择一条消息");
                }

                var deletedCount = await _messageService.DeleteMessagesAsync(sessionId, parsedMessageIds);
                logger.Append($"批量删除消息: SessionId={sessionId}, DeletedCount={deletedCount}");
                return $"删除成功，共删除 {deletedCount} 条消息";
            });
        }

        #endregion

        #region 模块管理

        /// <summary>
        ///Add module to session
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
        /// Get the module list of the session
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
                    Modules = modules.Select(z => MapModuleDtoWithRegisterInfo(AdminChatSessionModuleDto.CreateFromEntity(z))).ToList()
                };
            });
        }

        private static AdminChatSessionModuleDto MapModuleDtoWithRegisterInfo(AdminChatSessionModuleDto dto)
        {
            if (dto == null)
            {
                return null;
            }

            var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid == dto.XncfModuleUid);
            if (register != null)
            {
                dto.ModuleName = string.IsNullOrWhiteSpace(dto.ModuleName) ? register.Name : dto.ModuleName;
                dto.ModuleVersion = string.IsNullOrWhiteSpace(dto.ModuleVersion) ? register.Version : dto.ModuleVersion;
                dto.MenuName = register.MenuName;
                dto.ModuleDescription = register.Description;
                dto.DisplayName = !string.IsNullOrWhiteSpace(register.MenuName) ? register.MenuName : register.Name;
            }

            if (string.IsNullOrWhiteSpace(dto.DisplayName))
            {
                dto.DisplayName = dto.ModuleName;
            }

            return dto;
        }

        #endregion

        #region 私有辅助方法

        #endregion
    }

    #region 请求和响应模型

    /// <summary>
    ///Create session response
    /// </summary>
    public class CreateSessionResponse
    {
        public int SessionId { get; set; }
        public string Title { get; set; }
    }

    /// <summary>
    /// Get session list response
    /// </summary>
    public class GetSessionListResponse
    {
        public List<AdminChatSessionDto> Sessions { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// Get session details response
    /// </summary>
    public class GetSessionDetailResponse
    {
        public AdminChatSessionDto Session { get; set; }
    }

    /// <summary>
    ///Send message response
    /// </summary>
    public class SendMessageResponse
    {
        public AdminChatMessageDto UserMessage { get; set; }
        public AdminChatMessageDto AssistantMessage { get; set; }
    }

    /// <summary>
    /// Get message list response
    /// </summary>
    public class GetMessagesResponse
    {
        public List<AdminChatMessageDto> Messages { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// Add module to session request
    /// </summary>
    public class AddModulesRequest
    {
        public int SessionId { get; set; }
        public List<ModuleInfo> Modules { get; set; }
    }

    /// <summary>
    ///module information
    /// </summary>
    public class ModuleInfo
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
    }

    /// <summary>
    /// Get session module list response
    /// </summary>
    public class GetSessionModulesResponse
    {
        public List<AdminChatSessionModuleDto> Modules { get; set; }
    }

    #endregion
}

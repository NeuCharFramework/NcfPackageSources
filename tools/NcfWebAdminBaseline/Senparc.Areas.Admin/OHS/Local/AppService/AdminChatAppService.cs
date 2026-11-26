/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminChatAppService.cs
    File description: Related implementation.

    Created by: Senparc - 20260325

    Modified by: Senparc - 20260724
    Change description: v0.1.0 enhance batch module updates and improve the multilingual management interface

    Modified by: Senparc - 20260726
    Change description: v0.1.0 Updated module behavior and related support.

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20260808
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20260822
    Change description: v0.6.0 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.10.1 0.10.1 Enhanced Senparc.Areas.Admin functionality and compatibility

    Modified by: Senparc - 20261009
    Change description: v0.11.0 Updated module behavior and related support.

    Modified by: Senparc - 20261021
    Change description: v0.11.1 Updated module behavior and related support.

    Modified by: Senparc - 20261102
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.12.15 Updated module behavior and related support.

    Modified by: Senparc - 20261105
    Change description: v0.12.26 Updated module behavior and related support.

    Modified by: Senparc - 20261106
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261106
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261107
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261110
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261110
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261111
    Change description: Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261124
    Change description: v0.12.29 Improved AdminChat, system log analysis, workflow commands, and workspace asset operations.

    Modified by: Senparc - 20261125
    Change description: v0.12.29 Improved AdminChat conversation loading and command parameter feedback.

----------------------------------------------------------------*/
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;
using Senparc.Areas.Admin.Domain.Services;
using Senparc.Xncf.NeuCharWorkflow.Abstractions.Workflow;
using Senparc.CO2NET;
using Senparc.CO2NET.WebApi;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Areas.Admin.OHS.Local.Events;
using Senparc.Xncf.Workspace.Abstractions;
using Senparc.Xncf.PromptRange.Abstractions.Shotting;
using Senparc.Xncf.PromptRange.Abstractions.References;
using Senparc.Xncf.PromptRange.Abstractions.SystemMessages;
using Senparc.Ncf.XncfBase.ChatTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.OHS.Local.AppService
{
    /// <summary>
    /// Provides administrator-scoped chat session, memory, workflow, and workspace
    /// operations for the AdminChat experience.
    /// </summary>
    [AdminOrJwtAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class AdminChatAppService : LocalAppServiceBase
    {
        private const string AgentsManagerModuleUid = "D858D7FA-775A-4690-9023-CFB0B3B84994";

        private readonly AdminChatSessionService _sessionService;
        private readonly AdminChatMessageService _messageService;
        private readonly AdminChatSessionModuleService _sessionModuleService;
        private readonly AdminChatSessionWorkflowService _sessionWorkflowService;
        private readonly AdminChatTrajectoryService _trajectoryService;
        private readonly AdminChatAiService _chatAiService;
        private readonly AdminUserInfoService _adminUserInfoService;
        private readonly IStringLocalizer<AdminResource> _localizer;
        private readonly IEventBus _eventBus;
        private readonly IWorkspaceStorage _workspaceStorage;

        /// <summary>
        /// Initializes the AdminChat application service and its session, message,
        /// AI, event, and workspace dependencies.
        /// </summary>
        /// <param name="serviceProvider">The request service provider.</param>
        /// <param name="sessionService">The chat-session persistence service.</param>
        /// <param name="messageService">The chat-message persistence service.</param>
        /// <param name="sessionModuleService">The service linking modules to sessions.</param>
        /// <param name="sessionWorkflowService">The service linking workflows to sessions.</param>
        /// <param name="trajectoryService">The trajectory and execution-history service.</param>
        /// <param name="chatAiService">The AI service used by chat operations.</param>
        /// <param name="adminUserInfoService">The administrator account service.</param>
        /// <param name="localizer">The localized AdminChat message provider.</param>
        /// <param name="workspaceStorage">Optional workspace storage.</param>
        public AdminChatAppService(
            IServiceProvider serviceProvider,
            AdminChatSessionService sessionService,
            AdminChatMessageService messageService,
            AdminChatSessionModuleService sessionModuleService,
            AdminChatSessionWorkflowService sessionWorkflowService,
            AdminChatTrajectoryService trajectoryService,
            AdminChatAiService chatAiService,
            AdminUserInfoService adminUserInfoService,
            IStringLocalizer<AdminResource> localizer,
            IWorkspaceStorage workspaceStorage = null) : base(serviceProvider)
        {
            _sessionService = sessionService;
            _messageService = messageService;
            _sessionModuleService = sessionModuleService;
            _sessionWorkflowService = sessionWorkflowService;
            _trajectoryService = trajectoryService;
            _chatAiService = chatAiService;
            _adminUserInfoService = adminUserInfoService;
            _localizer = localizer;
            _eventBus = serviceProvider.GetService<IEventBus>();
            _workspaceStorage = workspaceStorage;
        }

        /// <summary>
        /// Returns dynamic option values for a registered chat-tool parameter.
        /// </summary>
        /// <param name="command">The chat command whose parameter is being queried.</param>
        /// <param name="parameter">The parameter name.</param>
        /// <param name="query">Optional text used to filter option labels.</param>
        /// <param name="limit">The maximum number of options to return.</param>
        /// <returns>A response containing the tool parameter options.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<ChatToolParameterOptionsResult>> GetChatToolParameterOptionsAsync(
            string command,
            string parameter,
            string query = null,
            int limit = 50)
        {
            return await this.GetResponseAsync<
                AppResponseBase<ChatToolParameterOptionsResult>,
                ChatToolParameterOptionsResult>(
                async (_, _) => await ChatToolRegistry.GetParameterOptionsAsync(
                    ServiceProvider,
                    new ChatToolParameterOptionsRequest
                    {
                        Command = command,
                        Parameter = parameter,
                        Query = query,
                        Limit = limit
                    }).ConfigureAwait(false));
        }

        /// <summary>
        /// Gets the current administrator's AdminChat memory profile for a session.
        /// </summary>
        /// <param name="sessionId">The session identifier, or zero for the default profile.</param>
        /// <returns>A response containing memory content, mode, and revision.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<AdminChatMemoryView>> GetMemoryAsync(int sessionId = 0)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminChatMemoryView>, AdminChatMemoryView>(
                async (response, logger) => await ServiceProvider.GetRequiredService<AdminChatMemoryService>()
                    .GetAsync(GetCurrentAdminUserInfoId(), sessionId));
        }

        /// <summary>
        /// Saves a revised AdminChat memory profile after validating its revision.
        /// </summary>
        /// <param name="request">The session, revision, Markdown, and system-message values.</param>
        /// <returns>A response containing the saved memory profile.</returns>
        /// <exception cref="NcfExceptionBase">Converted into an error response when the request is missing.</exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AdminChatMemoryView>> SaveMemoryAsync([FromBody] SaveAdminChatMemoryRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminChatMemoryView>, AdminChatMemoryView>(
                async (response, logger) =>
                {
                    if (request == null) throw new NcfExceptionBase("Memory request required.");
                    return await ServiceProvider.GetRequiredService<AdminChatMemoryService>().SaveAsync(
                        GetCurrentAdminUserInfoId(), request.SessionId, request.Revision, request.Markdown, request.SystemMessage);
                });
        }

        /// <summary>
        /// Changes the current administrator's memory mode for a session.
        /// </summary>
        /// <param name="request">The session, revision, and requested memory mode.</param>
        /// <returns>A response containing the updated memory profile.</returns>
        /// <exception cref="NcfExceptionBase">Converted into an error response when the request is missing.</exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AdminChatMemoryView>> SetMemoryEnabledAsync([FromBody] SetAdminChatMemoryRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminChatMemoryView>, AdminChatMemoryView>(
                async (response, logger) =>
                {
                    if (request == null) throw new NcfExceptionBase("Memory request required.");
                    return await ServiceProvider.GetRequiredService<AdminChatMemoryService>().SetModeAsync(
                        GetCurrentAdminUserInfoId(), request.SessionId, request.Revision,
                        request.Mode ?? (request.Enabled ? AdminChatMemoryMode.Learn : AdminChatMemoryMode.Off));
                });
        }

        /// <summary>
        /// Edits one structured memory entry for the current administrator.
        /// </summary>
        /// <param name="request">The session, revision, and replacement entry data.</param>
        /// <returns>A response containing the updated memory profile.</returns>
        /// <exception cref="NcfExceptionBase">Converted into an error response when the request is missing.</exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AdminChatMemoryView>> EditMemoryEntryAsync([FromBody] EditAdminChatMemoryRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminChatMemoryView>, AdminChatMemoryView>(
                async (response, logger) =>
                {
                    if (request == null) throw new NcfExceptionBase("Memory request required.");
                    return await ServiceProvider.GetRequiredService<AdminChatMemoryService>().EditAsync(
                        GetCurrentAdminUserInfoId(), request.SessionId, request.Revision, request.Entry);
                });
        }

        /// <summary>
        /// Returns the tenant-wide AdminChat system message when the caller is a
        /// super administrator.
        /// </summary>
        /// <returns>A response containing the current global revision and message.</returns>
        /// <exception cref="NcfExceptionBase">
        /// Converted into an error response when the caller is not a super administrator.
        /// </exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<AdminChatSystemMessageView>> GetGlobalChatSystemMessageAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<AdminChatSystemMessageView>, AdminChatSystemMessageView>(
                async (response, logger) =>
                {
                    if (!IsCurrentAdminSuperAdmin()) throw new NcfExceptionBase("Super administrator required.");
                    var profile = await ServiceProvider.GetRequiredService<AdminChatMemoryService>().GetGlobalAsync();
                    return new AdminChatSystemMessageView(profile.Revision, profile.SystemMessage);
                });
        }

        /// <summary>
        /// Saves the tenant-wide AdminChat system message for a super administrator
        /// using optimistic revision control.
        /// </summary>
        /// <param name="request">The expected revision and replacement system message.</param>
        /// <returns>The updated system-message view.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<AdminChatSystemMessageView>> SaveGlobalChatSystemMessageAsync(
            [FromBody] AdminChatSystemMessageView request)
        {
            return await this.GetResponseAsync<AppResponseBase<AdminChatSystemMessageView>, AdminChatSystemMessageView>(
                async (response, logger) =>
                {
                    if (!IsCurrentAdminSuperAdmin()) throw new NcfExceptionBase("Super administrator required.");
                    if (request == null) throw new NcfExceptionBase("SystemMessage request required.");
                    var profile = await ServiceProvider.GetRequiredService<AdminChatMemoryService>()
                        .SaveGlobalAsync(request.Revision, request.SystemMessage);
                    logger.Append("Updated tenant-wide AdminChat SystemMessage.");
                    return new AdminChatSystemMessageView(profile.Revision, profile.SystemMessage);
                });
        }

        /// <summary>
        /// Ensures the default AdminChat system-message PromptRange prompt exists
        /// and returns its prompt code.
        /// </summary>
        /// <returns>The initialized prompt descriptor.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<AdminChatSystemMessagePromptView>> EnsureAdminChatSystemMessagePromptAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<AdminChatSystemMessagePromptView>, AdminChatSystemMessagePromptView>(
                async (response, logger) =>
                {
                    var provider = ServiceProvider.GetService<IAdminChatSystemMessageProvider>()
                        ?? throw new NcfExceptionBase("PromptRange 模块不可用，无法管理 AdminChat 默认 SystemMessage。");
                    var prompt = await provider.GetAsync();
                    logger.Append($"确保 AdminChat SystemMessage PromptRange 已初始化: PromptCode={prompt.PromptCode}");
                    return new AdminChatSystemMessagePromptView(prompt.PromptCode, "AdminChat.SystemMessage");
                });
        }

        /// <summary>
        /// Lists available PromptRange shotting ranges for AdminChat scoring tools.
        /// </summary>
        /// <returns>The available shotting ranges.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<IReadOnlyList<PromptShottingRange>>> GetPromptShottingRangesAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<IReadOnlyList<PromptShottingRange>>, IReadOnlyList<PromptShottingRange>>(
                async (response, logger) =>
                {
                    var provider = ServiceProvider.GetService<IPromptShottingService>()
                        ?? throw new NcfExceptionBase(_localizer["AdminChat.PromptShotting.Unavailable"].Value);
                    return await provider.GetRangesAsync().ConfigureAwait(false);
                });
        }

        /// <summary>
        /// Saves a PromptRange shotting result and optionally attaches its reference
        /// to the originating AdminChat message.
        /// </summary>
        /// <param name="request">The shotting payload and optional message/reference data.</param>
        /// <returns>The saved shotting response and optional updated message.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<PromptShottingSaveResponse>> SavePromptShotAsync(
            [FromBody] AdminChatPromptShottingRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<PromptShottingSaveResponse>, PromptShottingSaveResponse>(
                async (response, logger) =>
                {
                    if (request == null) throw new NcfExceptionBase("Prompt shot request is required.");
                    var provider = ServiceProvider.GetService<IPromptShottingService>()
                        ?? throw new NcfExceptionBase(_localizer["AdminChat.PromptShotting.Unavailable"].Value);
                    var result = await provider.SaveAsync(request.Shotting).ConfigureAwait(false);
                    AdminChatMessageDto message = null;
                    if (request.MessageId > 0)
                    {
                        message = await _messageService.AttachPromptReferenceAsync(
                            GetCurrentAdminUserInfoId(),
                            request.MessageId,
                            new PromptReference
                            {
                                PromptItemId = result.PromptItemId,
                                PromptCode = result.PromptCode,
                                Start = request.Start,
                                Length = request.Length
                            }).ConfigureAwait(false);
                    }
                    logger.Append($"AdminChat Prompt shot saved: {result.PromptCode}, Run={request.Shotting?.Run == true}");
                    return new PromptShottingSaveResponse(result, message);
                });
        }

        /// <summary>
        /// Lists workspaces owned by the current super administrator.
        /// </summary>
        /// <returns>The owner-scoped workspace options.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetWorkspaceOptionsResponse>> GetWorkspaceOptionsAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<GetWorkspaceOptionsResponse>, GetWorkspaceOptionsResponse>(async (response, logger) =>
            {
                if (!IsCurrentAdminSuperAdmin() || _workspaceStorage == null)
                {
                    throw new NcfExceptionBase("只有超级管理员可以使用 Workspace。");
                }

                var ownerId = GetCurrentAdminUserInfoId();
                if (ownerId <= 0)
                {
                    throw new NcfExceptionBase("当前管理员身份无效。");
                }

                var owned = WorkspaceOwnership.SelectOwned(
                    await _workspaceStorage.ListWorkspacesAsync().ConfigureAwait(false),
                    ownerId);
                logger.Append($"Workspace 选项按所有者返回: OwnerId={ownerId}, Count={owned.Count}");
                return new GetWorkspaceOptionsResponse
                {
                    Workspaces = owned.Select(WorkspaceOptionDto.CreateFromDescriptor).ToList()
                };
            });
        }

        /// <summary>
        /// Creates a workspace owned by the current super administrator.
        /// </summary>
        /// <param name="request">The non-empty workspace name.</param>
        /// <returns>The created workspace option.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<WorkspaceOptionDto>> CreateWorkspaceAsync([FromBody] CreateWorkspaceRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<WorkspaceOptionDto>, WorkspaceOptionDto>(async (response, logger) =>
            {
                if (!IsCurrentAdminSuperAdmin() || _workspaceStorage == null)
                {
                    throw new NcfExceptionBase("只有超级管理员可以创建 Workspace。");
                }

                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                {
                    throw new NcfExceptionBase("Workspace 名称不能为空。");
                }

                var descriptor = await _workspaceStorage.CreateAsync(
                    new WorkspaceCreateRequest(GetCurrentAdminUserInfoId(), request.Name.Trim())).ConfigureAwait(false);
                return WorkspaceOptionDto.CreateFromDescriptor(descriptor);
            });
        }

        /// <summary>
        /// Lists entries at an owner-authorized session workspace path.
        /// </summary>
        /// <param name="sessionId">The owner-scoped chat session.</param>
        /// <param name="relativePath">The workspace-relative directory path.</param>
        /// <returns>Workspace entries and the current path.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<WorkspaceEntriesResponse>> GetWorkspaceEntriesAsync(
            int sessionId,
            string relativePath = "")
        {
            return await this.GetResponseAsync<AppResponseBase<WorkspaceEntriesResponse>, WorkspaceEntriesResponse>(async (response, logger) =>
            {
                var session = await GetWorkspaceSessionAsync(sessionId).ConfigureAwait(false);
                var entries = await _workspaceStorage.ListAsync(
                    session.WorkspaceId,
                    relativePath ?? string.Empty).ConfigureAwait(false);
                return new WorkspaceEntriesResponse
                {
                    CurrentPath = relativePath ?? string.Empty,
                    Entries = entries
                };
            });
        }

        /// <summary>
        /// Suggests owner-authorized workspace paths matching a bounded safe query.
        /// </summary>
        /// <param name="sessionId">Optional session used to resolve its workspace.</param>
        /// <param name="workspaceId">Optional owner-authorized workspace identifier.</param>
        /// <param name="query">The relative path query.</param>
        /// <returns>Matching path suggestions.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<WorkspacePathSuggestionsResponse>> SuggestWorkspacePathsAsync(
            int sessionId = 0,
            string workspaceId = "",
            string query = "")
        {
            return await this.GetResponseAsync<AppResponseBase<WorkspacePathSuggestionsResponse>, WorkspacePathSuggestionsResponse>(async (response, logger) =>
            {
                var normalizedQuery = (query ?? string.Empty).Replace('\\', '/').Trim().TrimStart('/');
                if (normalizedQuery.Length > 200
                    || normalizedQuery.Contains("..", StringComparison.Ordinal)
                    || normalizedQuery.IndexOfAny(['\0', '\r', '\n']) >= 0)
                {
                    throw new NcfExceptionBase("路径筛选无效。");
                }

                var resolvedWorkspaceId = await ResolveSuggestionWorkspaceIdAsync(sessionId, workspaceId)
                    .ConfigureAwait(false);
                var results = await _workspaceStorage.SearchAsync(
                    resolvedWorkspaceId,
                    new WorkspaceSearchRequest(
                        NameQuery: normalizedQuery,
                        MaxResults: 20,
                        SuggestPaths: true)).ConfigureAwait(false);
                logger.Append($"Workspace 路径候选: WorkspaceId={resolvedWorkspaceId}, Query={normalizedQuery}, Count={results.Count}");
                return new WorkspacePathSuggestionsResponse
                {
                    Items = results.Select(item => new WorkspacePathSuggestionDto
                    {
                        RelativePath = item.RelativePath,
                        IsDirectory = item.MatchType == "directory",
                        Name = string.IsNullOrEmpty(item.Preview) ? item.RelativePath : item.Preview
                    }).ToList()
                };
            });
        }

        /// <summary>
        /// Resolves the workspace identifier used by path suggestions and enforces
        /// the super-administrator and ownership requirements for the selected source.
        /// A positive session identifier takes precedence over the explicit workspace
        /// identifier so that suggestions cannot switch a session to another workspace.
        /// </summary>
        /// <param name="sessionId">The session whose workspace should be used, or zero to use <paramref name="workspaceId"/>.</param>
        /// <param name="workspaceId">An explicitly selected workspace identifier when no session is supplied.</param>
        /// <returns>The owner-authorized workspace identifier.</returns>
        /// <exception cref="NcfExceptionBase">
        /// Thrown when Workspace is unavailable, the session has no workspace, the
        /// explicit identifier is missing, or the workspace belongs to another owner.
        /// </exception>
        private async Task<string> ResolveSuggestionWorkspaceIdAsync(int sessionId, string workspaceId)
        {
            if (!IsCurrentAdminSuperAdmin() || _workspaceStorage == null)
            {
                throw new NcfExceptionBase("只有超级管理员可以使用 Workspace。");
            }

            if (sessionId > 0)
            {
                var session = await GetWorkspaceSessionAsync(sessionId).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(session.WorkspaceId))
                {
                    throw new NcfExceptionBase("当前会话没有 Workspace。");
                }

                return session.WorkspaceId;
            }

            workspaceId = workspaceId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(workspaceId))
            {
                throw new NcfExceptionBase("请先选择 Workspace。");
            }

            var descriptor = await _workspaceStorage.GetAsync(workspaceId).ConfigureAwait(false);
            var ownerId = GetCurrentAdminUserInfoId();
            if (!WorkspaceOwnership.IsOwnedBy(descriptor, ownerId))
            {
                throw new NcfExceptionBase("无权访问该 Workspace。");
            }

            return descriptor.Id;
        }

        /// <summary>
        /// Reads a bounded preview of one owner-authorized workspace file.
        /// </summary>
        /// <param name="sessionId">The owner-scoped chat session.</param>
        /// <param name="relativePath">The workspace-relative file path.</param>
        /// <returns>A file preview DTO or a standard application error.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<WorkspaceFilePreviewDto>> GetWorkspaceFilePreviewAsync(
            int sessionId,
            string relativePath)
        {
            return await this.GetResponseAsync<AppResponseBase<WorkspaceFilePreviewDto>, WorkspaceFilePreviewDto>(async (response, logger) =>
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    throw new NcfExceptionBase("文件路径不能为空。");
                }

                var session = await GetWorkspaceSessionAsync(sessionId).ConfigureAwait(false);
                var contentType = WorkspaceTextFiles.ResolveContentType(relativePath);
                try
                {
                    var file = await _workspaceStorage.ReadFileBytesAsync(
                        session.WorkspaceId,
                        relativePath,
                        WorkspaceTextFiles.MaxEditableBytes).ConfigureAwait(false);
                    return CreateWorkspaceFilePreview(relativePath, file.Content, file.Metadata.ETag);
                }
                catch (WorkspaceLimitException) when (WorkspaceTextFiles.IsText(contentType))
                {
                    logger.Append($"Workspace 大文本改为片段预览: Path={relativePath}");
                    return await CreateTextWindowPreviewAsync(session.WorkspaceId, relativePath, contentType, 1)
                        .ConfigureAwait(false);
                }
                catch (WorkspaceLimitException) when (WorkspaceTextFiles.Is3D(relativePath))
                {
                    var metadata = await _workspaceStorage.GetFileInfoAsync(
                        session.WorkspaceId, relativePath).ConfigureAwait(false);
                    return new WorkspaceFilePreviewDto
                    {
                        Path = relativePath,
                        ContentType = contentType,
                        Size = metadata.Length,
                        IsText = false,
                        Etag = metadata.ETag ?? string.Empty,
                        Windowed = false,
                        StartLine = 1
                    };
                }
                catch (WorkspaceLimitException)
                {
                    throw new NcfExceptionBase("文件超过 2 MB，请下载后查看。");
                }
            });
        }

        /// <summary>
        /// Reads a bounded line window from one owner-authorized workspace text file.
        /// </summary>
        /// <param name="sessionId">The owner-scoped chat session.</param>
        /// <param name="relativePath">The workspace-relative text file path.</param>
        /// <param name="startLine">The one-based starting line.</param>
        /// <returns>A text-window preview DTO.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<WorkspaceFilePreviewDto>> GetWorkspaceTextWindowAsync(
            int sessionId,
            string relativePath,
            int startLine = 1)
        {
            return await this.GetResponseAsync<AppResponseBase<WorkspaceFilePreviewDto>, WorkspaceFilePreviewDto>(async (response, logger) =>
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    throw new NcfExceptionBase("文件路径不能为空。");
                }

                var contentType = WorkspaceTextFiles.ResolveContentType(relativePath);
                if (!WorkspaceTextFiles.IsText(contentType))
                {
                    throw new NcfExceptionBase("该文件不支持文本片段预览。");
                }

                var session = await GetWorkspaceSessionAsync(sessionId).ConfigureAwait(false);
                logger.Append($"Workspace 文本片段: Path={relativePath}, StartLine={startLine}");
                return await CreateTextWindowPreviewAsync(session.WorkspaceId, relativePath, contentType, startLine)
                    .ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Validates and saves an owner-authorized workspace text edit, returning
        /// the resulting preview and change metadata.
        /// </summary>
        /// <param name="request">The relative path and replacement content.</param>
        /// <returns>The saved file preview DTO.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<WorkspaceFilePreviewDto>> SaveWorkspaceFileAsync(
            [FromBody] SaveWorkspaceFileRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<WorkspaceFilePreviewDto>, WorkspaceFilePreviewDto>(async (response, logger) =>
            {
                if (request == null) throw new NcfExceptionBase("保存请求不能为空。");
                WorkspaceTextEdit edit;
                try
                {
                    edit = WorkspaceTextFiles.PrepareEdit(request.RelativePath, request.Content);
                }
                catch (ArgumentException error)
                {
                    throw new NcfExceptionBase(error.Message);
                }

                if (_workspaceStorage == null) throw new NcfExceptionBase("Workspace 模块尚不可用。");
                var session = await GetWorkspaceSessionAsync(request.SessionId).ConfigureAwait(false);
                try
                {
                    var result = await _workspaceStorage.WriteFileBytesAsync(
                        session.WorkspaceId,
                        new WorkspaceWriteFileRequest(
                            edit.Path,
                            edit.Content,
                            Create: false,
                            Overwrite: true,
                            ExpectedETag: request.ExpectedEtag ?? string.Empty)).ConfigureAwait(false);
                    return CreateWorkspaceFilePreview(edit.Path, edit.Content, result.Metadata.ETag);
                }
                catch (WorkspaceConflictException)
                {
                    throw new NcfExceptionBase("文件已在其他地方被修改，请重新打开后再保存。");
                }
                catch (FileNotFoundException)
                {
                    throw new NcfExceptionBase("文件不存在或已被删除，无法保存。");
                }
            });
        }

        /// <summary>
        /// Creates the non-windowed preview representation for bytes read from a
        /// workspace file. Text bytes are decoded as UTF-8 while all bytes remain
        /// available through the Base64 representation for binary consumers.
        /// </summary>
        /// <param name="path">The workspace-relative path represented by the preview.</param>
        /// <param name="content">The complete content returned by the storage provider.</param>
        /// <param name="etag">The storage version token associated with the content.</param>
        /// <returns>A preview DTO containing content, size, type, and version metadata.</returns>
        private static WorkspaceFilePreviewDto CreateWorkspaceFilePreview(string path, byte[] content, string etag)
        {
            var contentType = WorkspaceTextFiles.ResolveContentType(path);
            var isText = WorkspaceTextFiles.IsText(contentType);
            return new WorkspaceFilePreviewDto
            {
                Path = path,
                ContentType = contentType,
                Size = content.LongLength,
                IsText = isText,
                Content = isText ? Encoding.UTF8.GetString(content) : null,
                ContentBase64 = Convert.ToBase64String(content),
                Etag = etag ?? string.Empty,
                Windowed = false,
                StartLine = 1,
                LineCount = 0,
                HasMore = false,
                Truncated = false
            };
        }

        /// <summary>
        /// Reads a bounded text window from Workspace and maps storage-specific
        /// failures to the application errors exposed by the AdminChat API.
        /// </summary>
        /// <param name="workspaceId">The owner-authorized workspace identifier.</param>
        /// <param name="relativePath">The workspace-relative text-file path.</param>
        /// <param name="contentType">The already-resolved media type for the file.</param>
        /// <param name="startLine">The one-based first line requested by the caller.</param>
        /// <returns>A windowed text preview with truncation and continuation metadata.</returns>
        /// <exception cref="NcfExceptionBase">
        /// Thrown when the path is not a readable previewable text file or no longer exists.
        /// </exception>
        private async Task<WorkspaceFilePreviewDto> CreateTextWindowPreviewAsync(
            string workspaceId,
            string relativePath,
            string contentType,
            int startLine)
        {
            WorkspaceTextWindow window;
            try
            {
                window = await _workspaceStorage.ReadTextWindowAsync(
                    workspaceId,
                    new WorkspaceTextWindowRequest(
                        relativePath,
                        Math.Max(1, startLine),
                        WorkspaceTextFiles.PreviewWindowLines,
                        WorkspaceTextFiles.PreviewWindowBytes)).ConfigureAwait(false);
            }
            catch (ArgumentException)
            {
                throw new NcfExceptionBase("该位置不是可预览的文本。");
            }
            catch (UnauthorizedAccessException)
            {
                throw new NcfExceptionBase("该文件不能预览。");
            }
            catch (FileNotFoundException)
            {
                throw new NcfExceptionBase("文件不存在或已被删除。");
            }

            return new WorkspaceFilePreviewDto
            {
                Path = window.RelativePath,
                ContentType = contentType,
                Size = window.FileLength,
                IsText = true,
                Content = window.Content,
                ContentBase64 = string.Empty,
                Etag = window.ETag,
                Windowed = true,
                StartLine = window.StartLine,
                LineCount = window.LineCount,
                HasMore = window.HasMore,
                Truncated = window.Truncated
            };
        }

        /// <summary>
        /// Loads an AdminChat session through the Workspace asset service so that
        /// the current HTTP user is applied to the service's ownership checks.
        /// </summary>
        /// <param name="sessionId">The session identifier to resolve.</param>
        /// <returns>The owner-authorized session and its workspace association.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the request has no registered HTTP context accessor.
        /// </exception>
        private async Task<AdminChatSession> GetWorkspaceSessionAsync(int sessionId)
        {
            var user = ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>().HttpContext.User;
            return await ServiceProvider.GetRequiredService<AdminChatWorkspaceAssetService>()
                .GetSessionAsync(sessionId, user).ConfigureAwait(false);
        }

        #region Legacy section

        /// <summary>
        /// Creates an administrator-owned chat session and optionally links selected
        /// modules, workflows, memory mode, and Workspace access.
        /// </summary>
        /// <param name="request">The session title, initial message, and selected capabilities.</param>
        /// <returns>A response containing the new session identifier and effective settings.</returns>
        /// <exception cref="NcfExceptionBase">
        /// Converted into an error response when the administrator is not authenticated,
        /// Workspace ownership is invalid, or a requested capability cannot be linked.
        /// </exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<CreateSessionResponse>> CreateSessionAsync([FromBody] CreateChatSessionInputDto request)
        {
            return await this.GetResponseAsync<AppResponseBase<CreateSessionResponse>, CreateSessionResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var titleSource = string.IsNullOrEmpty(request.Title) ? request.InitialMessage : request.Title;
                var title = string.IsNullOrEmpty(titleSource)
                    ? _localizer["AdminChat.NewConversation"]
                    : (titleSource.Length > 50 ? titleSource.Substring(0, 50) + "..." : titleSource);

                var workspaceId = request.WorkspaceId?.Trim();
                if (!string.IsNullOrWhiteSpace(workspaceId))
                {
                    if (!IsCurrentAdminSuperAdmin())
                    {
                        throw new NcfExceptionBase("只有超级管理员可以使用 Workspace。");
                    }

                    var workspace = _workspaceStorage == null
                        ? null
                        : await _workspaceStorage.GetAsync(workspaceId).ConfigureAwait(false);
                    if (!WorkspaceOwnership.IsOwnedBy(workspace, userId))
                    {
                        if (workspace != null)
                        {
                            logger.Append(
                                $"拒绝将会话绑定到其他管理员的 Workspace: UserId={userId}, WorkspaceId={workspaceId}, OwnerId={workspace.OwnerId}");
                        }

                        throw new NcfExceptionBase("Workspace 不存在或不可用。");
                    }
                }

                var memory = ServiceProvider.GetRequiredService<AdminChatMemoryService>();
                var memoryMode = request.MemoryMode ?? (request.UseMemory ? AdminChatMemoryMode.Learn : AdminChatMemoryMode.Off);
                await memory.PrepareModeAsync(memoryMode);
                var session = await _sessionService.CreateSessionAsync(
                    title,
                    userId,
                    workspaceId,
                    request.WorkspaceWriteEnabled,
                    request.WorkspaceCliEnabled);
                if (memoryMode != AdminChatMemoryMode.Off)
                {
                    var profile = await memory.GetAsync(userId, session.Id);
                    await memory.SetModeAsync(userId, session.Id, profile.Revision, memoryMode);
                }

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
                await EnsureAgentsManagerModuleLinkedAsync(session.Id);

                var workflowProvider = ServiceProvider.GetService<IWorkflowFunctionCallingProvider>();
                if (workflowProvider != null && request.WorkflowIds != null)
                {
                    var selectedWorkflowIds = request.WorkflowIds
                        .Where(id => id > 0)
                        .ToHashSet();
                    var availableWorkflows = await workflowProvider
                        .GetAvailableAsync(userId)
                        .ConfigureAwait(false);
                    await _sessionWorkflowService.AddWorkflowsToSessionAsync(
                        session.Id,
                        availableWorkflows
                            .Where(workflow => selectedWorkflowIds.Contains(workflow.Id))
                            .Select(workflow => (
                                workflow.Id,
                                workflow.Name,
                                workflow.Description))
                            .ToList());
                }

                logger.Append($"创建会话: SessionId={session.Id}, UserId={userId}");
                await PublishSyncEventAsync(userId, session.Id, "session-created");

                return new CreateSessionResponse
                {
                    SessionId = session.Id,
                    Title = session.Title,
                    InitialMessage = request.InitialMessage,
                    Mode = request.Mode,
                    WorkspaceId = session.WorkspaceId,
                    WorkspaceWriteEnabled = session.WorkspaceWriteEnabled,
                    WorkspaceCliEnabled = session.WorkspaceCliEnabled
                };
            });
        }

        /// <summary>
        /// Returns the current administrator's active chat sessions in a paged result.
        /// </summary>
        /// <param name="pageIndex">The one-based page index.</param>
        /// <param name="pageSize">The maximum number of sessions per page.</param>
        /// <returns>A response containing session DTOs and the total count.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetSessionListResponse>> GetSessionListAsync(int pageIndex = 1, int pageSize = 20)
        {
            return await this.GetResponseAsync<AppResponseBase<GetSessionListResponse>, GetSessionListResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var (sessions, totalCount) = await _sessionService.GetUserActiveSessionsAsync(userId, pageIndex, pageSize);
                var sessionDtos = sessions.Select(AdminChatSessionDto.CreateFromEntity).ToList();
                await AttachWorkspaceNamesAsync(sessionDtos);

                return new GetSessionListResponse
                {
                    Sessions = sessionDtos,
                    TotalCount = totalCount
                };
            });
        }

        /// <summary>
        /// Reorders one administrator-owned session relative to another session.
        /// </summary>
        /// <param name="request">The source session, target session, and insertion direction.</param>
        /// <returns>A response containing the reordered session list.</returns>
        /// <exception cref="NcfExceptionBase">Converted into an error response when the request is missing.</exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<List<AdminChatSessionDto>>> ReorderSessionAsync(
            [FromBody] ReorderSessionRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<List<AdminChatSessionDto>>, List<AdminChatSessionDto>>(
                async (response, logger) =>
                {
                    if (request == null) throw new NcfExceptionBase("Session order request required.");
                    var userId = GetCurrentAdminUserInfoId();
                    var sessions = await _sessionService.ReorderSessionAsync(
                        userId, request.SessionId, request.TargetSessionId, request.InsertAfter);
                    logger.Append($"Reordered session: SessionId={request.SessionId}, UserId={userId}");
                    await PublishSyncEventAsync(userId, request.SessionId, "session-reordered");
                    return sessions.Select(AdminChatSessionDto.CreateFromEntity).ToList();
                });
        }

        /// <summary>
        /// Generates a title from the first user message in an administrator-owned session.
        /// </summary>
        /// <param name="request">The session identifier and optional AI model identifier.</param>
        /// <returns>A response containing the generated or existing session title.</returns>
        /// <exception cref="NcfExceptionBase">
        /// Converted into an error response when the administrator is unauthenticated
        /// or the session is missing or inaccessible.
        /// </exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<GenerateSessionTitleResponse>> GenerateSessionTitleAsync(
            [FromBody] GenerateSessionTitleRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<GenerateSessionTitleResponse>, GenerateSessionTitleResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(request?.SessionId ?? 0, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                var (messages, _) = await _messageService.GetSessionMessagesAsync(session.Id);
                var firstUserMessage = messages
                    .Where(message => message.RoleType == ChatMessageRoleType.User)
                    .OrderBy(message => message.Sequence)
                    .FirstOrDefault();
                if (firstUserMessage == null)
                {
                    return new GenerateSessionTitleResponse { Title = session.Title };
                }

                var title = await _chatAiService.GenerateSessionTitleAsync(
                    firstUserMessage.Content,
                    request?.AiModelId ?? 0);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    await _sessionService.UpdateSessionTitleAsync(session.Id, userId, title);
                }

                logger.Append($"生成会话标题: SessionId={session.Id}, UserId={userId}");
                return new GenerateSessionTitleResponse
                {
                    Title = string.IsNullOrWhiteSpace(title) ? session.Title : title
                };
            });
        }

        /// <summary>
        /// Gets one administrator-owned session with its messages, linked modules,
        /// and linked workflows.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>A response containing the complete session detail.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetSessionDetailResponse>> GetSessionDetailAsync(int sessionId)
        {
            return await this.GetResponseAsync<AppResponseBase<GetSessionDetailResponse>, GetSessionDetailResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(sessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                await EnsureAgentsManagerModuleLinkedAsync(sessionId);

                var (messages, _) = await _messageService.GetSessionMessagesAsync(sessionId);
                var modules = await _sessionModuleService.GetSessionModulesAsync(sessionId);
                var workflows = await _sessionWorkflowService.GetSessionWorkflowsAsync(sessionId);

                var sessionDto = AdminChatSessionDto.CreateFromEntity(session);
                await AttachWorkspaceNamesAsync(new[] { sessionDto });
                sessionDto.Messages = messages.Select(AdminChatMessageDto.CreateFromEntity).ToList();
                sessionDto.Modules = modules.Select(z => MapModuleDtoWithRegisterInfo(AdminChatSessionModuleDto.CreateFromEntity(z))).ToList();
                sessionDto.Workflows = await MapWorkflowDtosAsync(workflows, userId).ConfigureAwait(false);

                return new GetSessionDetailResponse
                {
                    Session = sessionDto
                };
            });
        }

        /// <summary>
        /// Permanently deletes an administrator-owned chat session.
        /// </summary>
        /// <param name="sessionId">The session identifier to delete.</param>
        /// <returns>A localized success message.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Delete)]
        public async Task<StringAppResponse> DeleteSessionAsync(int sessionId)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var success = await _sessionService.DeleteSessionAsync(sessionId, userId);
                if (!success)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrNoDeletePermission"]);
                }

                logger.Append($"删除会话: SessionId={sessionId}, UserId={userId}");
                await PublishSyncEventAsync(userId, sessionId, "session-deleted");
                return _localizer["AdminChat.DeleteSessionSuccess"];
            });
        }

        /// <summary>
        /// Archives an administrator-owned session without deleting its messages.
        /// </summary>
        /// <param name="request">The session identifier to archive.</param>
        /// <returns>A localized success message.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> ArchiveSessionAsync([FromBody] ArchiveSessionRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var sessionId = request?.SessionId ?? 0;
                var success = await _sessionService.ArchiveSessionAsync(sessionId, userId);
                if (!success)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                logger.Append($"归档会话: SessionId={sessionId}, UserId={userId}");
                await PublishSyncEventAsync(userId, sessionId, "session-archived");
                return _localizer["AdminChat.ArchiveSessionSuccess"];
            });
        }

        /// <summary>
        /// Restores an archived administrator-owned session to the active list.
        /// </summary>
        /// <param name="request">The session identifier to restore.</param>
        /// <returns>A localized success message.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> RestoreSessionAsync([FromBody] ArchiveSessionRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var sessionId = request?.SessionId ?? 0;
                var success = await _sessionService.RestoreSessionAsync(sessionId, userId);
                if (!success)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                logger.Append($"还原会话: SessionId={sessionId}, UserId={userId}");
                await PublishSyncEventAsync(userId, sessionId, "session-restored");
                return _localizer["AdminChat.Archive.RestoreSuccess"];
            });
        }

        /// <summary>
        /// Sets or clears the unread marker for an administrator-owned session.
        /// </summary>
        /// <param name="request">The session identifier and desired unread state.</param>
        /// <returns>A string response indicating that the state was saved.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> SetSessionUnreadAsync([FromBody] SetSessionUnreadRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var sessionId = request?.SessionId ?? 0;
                var success = await _sessionService.SetSessionUnreadAsync(sessionId, userId, request?.Unread == true);
                if (!success)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                logger.Append($"会话未读状态: SessionId={sessionId}, UserId={userId}, Unread={request?.Unread == true}");
                return "saved";
            });
        }

        /// <summary>
        /// Returns the current administrator's archived sessions in a paged result.
        /// </summary>
        /// <param name="pageIndex">The one-based page index.</param>
        /// <param name="pageSize">The maximum number of sessions per page.</param>
        /// <returns>A response containing archived session DTOs and total count.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetSessionListResponse>> GetArchivedSessionListAsync(int pageIndex = 1, int pageSize = 20)
        {
            return await this.GetResponseAsync<AppResponseBase<GetSessionListResponse>, GetSessionListResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var (sessions, totalCount) = await _sessionService.GetUserArchivedSessionsAsync(userId, pageIndex, pageSize);
                var sessionDtos = sessions.Select(AdminChatSessionDto.CreateFromEntity).ToList();
                await AttachWorkspaceNamesAsync(sessionDtos);

                return new GetSessionListResponse
                {
                    Sessions = sessionDtos,
                    TotalCount = totalCount
                };
            });
        }

        /// <summary>
        /// Returns session and message statistics for all users.
        /// </summary>
        /// <returns>A response containing per-user AdminChat statistics.</returns>
        /// <exception cref="NcfExceptionBase">
        /// Converted into an error response when the caller is not a super administrator.
        /// </exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetUserSessionStatsResponse>> GetUserSessionStatsAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<GetUserSessionStatsResponse>, GetUserSessionStatsResponse>(async (response, logger) =>
            {
                var currentUserId = GetCurrentAdminUserInfoId();
                if (currentUserId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                if (!IsCurrentAdminSuperAdmin())
                {
                    logger.Append($"拒绝非超级管理员访问用户统计: UserId={currentUserId}");
                    throw new NcfExceptionBase(_localizer["AdminChat.SuperAdminOnly"]);
                }

                var sessionStats = await _sessionService.GetUserSessionStatsAsync();
                var messageCounts = await _messageService.GetMessageCountByUserAsync();
                var messageCountMap = messageCounts.ToDictionary(x => x.UserId, x => x.Count);

                var userIds = sessionStats.Select(s => s.UserId).ToList();
                var users = _adminUserInfoService.GetAdminUserInfo(userIds);
                var userMap = users.ToDictionary(u => u.Id);

                var items = sessionStats
                    .OrderByDescending(s => s.LastActiveTime)
                    .Select(s =>
                    {
                        userMap.TryGetValue(s.UserId, out var user);
                        return new AdminChatUserStatDto
                        {
                            UserId = s.UserId,
                            UserName = user?.UserName,
                            RealName = user?.RealName,
                            TotalSessionCount = s.TotalSessionCount,
                            ActiveSessionCount = s.ActiveSessionCount,
                            ArchivedSessionCount = s.ArchivedSessionCount,
                            DeletedSessionCount = s.DeletedSessionCount,
                            MessageCount = messageCountMap.TryGetValue(s.UserId, out var count) ? count : 0,
                            LastActiveTime = s.LastActiveTime
                        };
                    })
                    .ToList();

                logger.Append($"超级管理员用户统计: CurrentUserId={currentUserId}, UserCount={items.Count}");
                return new GetUserSessionStatsResponse
                {
                    Stats = items
                };
            });
        }

        #endregion

        /// <summary>
        /// Enriches owner-visible session DTOs with names resolved from the current
        /// super administrator's owned Workspace descriptors.
        /// </summary>
        /// <param name="sessions">The session DTOs to enrich in place.</param>
        private async Task AttachWorkspaceNamesAsync(IEnumerable<AdminChatSessionDto> sessions)
        {
            if (_workspaceStorage == null || !IsCurrentAdminSuperAdmin())
            {
                return;
            }

            var items = sessions?.Where(session => !string.IsNullOrWhiteSpace(session.WorkspaceId)).ToList();
            if (items == null || items.Count == 0)
            {
                return;
            }

            var ownerId = GetCurrentAdminUserInfoId();
            var names = WorkspaceOwnership.SelectOwned(
                    await _workspaceStorage.ListWorkspacesAsync().ConfigureAwait(false),
                    ownerId)
                .ToDictionary(workspace => workspace.Id, workspace => workspace.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var session in items)
            {
                if (names.TryGetValue(session.WorkspaceId, out var name))
                {
                    session.WorkspaceName = name;
                }
            }
        }

        #region Legacy section

        /// <summary>
        /// Sends a user message, invokes the selected chat mode, and persists the
        /// assistant response and any generated trajectory artifacts.
        /// </summary>
        /// <param name="request">
        /// The session, message, model, mode, attachments, and harness options.
        /// </param>
        /// <returns>A response containing the persisted user and assistant messages.</returns>
        /// <exception cref="NcfExceptionBase">
        /// Converted into an error response when the administrator is unauthenticated,
        /// the session is inaccessible, or no content/attachment is supplied.
        /// </exception>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<SendMessageResponse>> SendMessageAsync([FromBody] ChatMessageInputDto request)
        {
            return await this.GetResponseAsync<AppResponseBase<SendMessageResponse>, SendMessageResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(request.SessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                if (string.IsNullOrWhiteSpace(request.Content) && request.Attachments?.Count is not > 0)
                    throw new NcfExceptionBase(_localizer["AdminChat.StreamInputRequired"]);
                if (request.Attachments?.Count > 0) await GetWorkspaceSessionAsync(request.SessionId);
                var attachments = await ServiceProvider.GetRequiredService<AdminChatAttachmentService>()
                    .ValidateAsync(session, request.Attachments);
                var content = string.IsNullOrWhiteSpace(request.Content)
                    ? AdminChatAttachmentService.AttachmentOnlyMessage
                    : request.PromptReferences?.Count > 0 ? request.Content : request.Content.Trim();
                var userMessage = await _messageService.AddMessageAsync(
                    request.SessionId,
                    ChatMessageRoleType.User,
                    content, attachments: attachments, promptReferences: request.PromptReferences);

                await _sessionService.UpdateLastMessageTimeAsync(request.SessionId);

                var mode = request.Mode;
                string aiResponse;
                string modelIdentifier;
                AdminChatHarnessResult harness = null;

                if (mode != AdminChatMode.Simple)
                {
                    (aiResponse, modelIdentifier, harness) = await _chatAiService.GenerateNativeHarnessResponseAsync(
                        request.SessionId,
                        userId,
                        content,
                        request.AiModelId,
                        attachments: attachments,
                        requestedMode: mode == AdminChatMode.Execute ? "execute" : "plan",
                        harnessOptions: request.HarnessOptions);
                }
                else
                {
                    (aiResponse, modelIdentifier) = await _chatAiService.GenerateResponseAsync(
                        request.SessionId,
                        userId,
                        content,
                        request.AiModelId,
                        generationOptions: request.MaxOutputTokens > 0
                            ? new AdminChatGenerationOptions { MaxOutputTokens = request.MaxOutputTokens }
                            : null,
                        attachments: attachments);
                }

                var assistantMessage = await _messageService.AddMessageAsync(
                    request.SessionId,
                    ChatMessageRoleType.Assistant,
                    aiResponse,
                    modelIdentifier,
                    harness?.TrajectoryId,
                    harness?.TrajectorySequence,
                    await _messageService.CollectWorkspaceArtifactsAsync(request.SessionId, harness));

                logger.Append($"发送消息: SessionId={request.SessionId}, MessageId={userMessage.Id}, Mode={mode}");
                await PublishSyncEventAsync(userId, request.SessionId, "messages-changed");

                return new SendMessageResponse
                {
                    MemoryUpdateStatus = _chatAiService.MemoryUpdateStatus,
                    MemoryUpdateResult = _chatAiService.MemoryUpdateResult,
                    MemoryUsage = _chatAiService.MemoryUsage,
                    UserMessage = AdminChatMessageDto.CreateFromEntity(userMessage),
                    AssistantMessage = AdminChatMessageDto.CreateFromEntity(assistantMessage),
                    Mode = mode,
                    HarnessSteps = harness?.Steps,
                    TrajectoryId = harness?.TrajectoryId ?? 0,
                    TrajectorySequence = harness?.TrajectorySequence ?? 0,
                    TrajectoryStatus = harness?.Status ?? default,
                    TrajectoryEvents = harness?.TrajectoryEvents,
                    PendingApprovals = harness?.PendingApprovals
                };
            });
        }

        /// <summary>
        /// Searches active conversation history or tool calls for the current administrator.
        /// </summary>
        /// <param name="keyword">The text to search for.</param>
        /// <param name="limit">The maximum number of hits.</param>
        /// <param name="scope">The search scope, typically <c>chat</c> or <c>tool</c>.</param>
        /// <returns>A response containing matching history hits.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<SearchHistoryResponse>> SearchHistoryAsync(
            string keyword,
            int limit = 10,
            string scope = "chat")
        {
            return await this.GetResponseAsync<AppResponseBase<SearchHistoryResponse>, SearchHistoryResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var toolScope = string.Equals(scope, "tool", StringComparison.OrdinalIgnoreCase);
                var hits = toolScope
                    ? await _messageService.SearchActiveToolCallsAsync(userId, keyword, limit)
                    : await _messageService.SearchActiveHistoryAsync(userId, keyword, limit);
                logger.Append($"搜索未归档对话: UserId={userId}, Scope={(toolScope ? "tool" : "chat")}, Hits={hits.Count}");
                return new SearchHistoryResponse
                {
                    Hits = hits.ToList()
                };
            });
        }

        /// <summary>
        /// Returns one page of messages from an administrator-owned session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="pageIndex">The zero-based message page index.</param>
        /// <param name="pageSize">The maximum number of messages per page.</param>
        /// <returns>A response containing message DTOs and total count.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetMessagesResponse>> GetMessagesAsync(int sessionId, int pageIndex = 0, int pageSize = 50)
        {
            return await this.GetResponseAsync<AppResponseBase<GetMessagesResponse>, GetMessagesResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(sessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
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
        /// Returns trajectory metadata and a collapsed event stream for an owned trajectory.
        /// </summary>
        /// <param name="trajectoryId">The trajectory identifier.</param>
        /// <param name="query">Optional event-text filter.</param>
        /// <param name="beforeSequence">Optional exclusive sequence boundary.</param>
        /// <param name="pageSize">The maximum number of trajectory events.</param>
        /// <returns>A response containing trajectory metadata and events.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetTrajectoryResponse>> GetTrajectoryAsync(
            int trajectoryId,
            string query = null,
            int beforeSequence = 0,
            int pageSize = 200)
        {
            return await this.GetResponseAsync<AppResponseBase<GetTrajectoryResponse>, GetTrajectoryResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var trajectory = await _trajectoryService.GetAsync(trajectoryId, userId);
                if (trajectory == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.TrajectoryNotFound"]);
                }

                var events = await _trajectoryService.GetEventsAsync(
                    trajectoryId,
                    userId,
                    beforeSequence > 0 ? beforeSequence : null,
                    query,
                    pageSize);

                return new GetTrajectoryResponse
                {
                    Trajectory = AdminChatTrajectoryDto.CreateFromEntity(trajectory),
                    Events = AdminChatTrajectoryEventDto.CollapseAssistantTextChunks(
                        events.Select(AdminChatTrajectoryEventDto.CreateFromEntity)).ToList()
                };
            });
        }

        /// <summary>
        /// Searches owned trajectories by text and returns matching trajectory metadata.
        /// </summary>
        /// <param name="query">The text or identifier to search for.</param>
        /// <param name="pageSize">The maximum number of results.</param>
        /// <returns>A response containing matching trajectories.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<SearchTrajectoriesResponse>> SearchTrajectoriesAsync(
            string query,
            int pageSize = 50)
        {
            return await this.GetResponseAsync<AppResponseBase<SearchTrajectoriesResponse>, SearchTrajectoriesResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var trajectories = await _trajectoryService.SearchAsync(userId, query, pageSize);
                return new SearchTrajectoriesResponse
                {
                    Trajectories = trajectories.Select(AdminChatTrajectoryDto.CreateFromEntity).ToList()
                };
            });
        }

        /// <summary>
        /// Lists trajectories belonging to an administrator-owned session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>A response containing the session trajectories.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<SearchTrajectoriesResponse>> GetSessionTrajectoriesAsync(int sessionId)
        {
            return await this.GetResponseAsync<AppResponseBase<SearchTrajectoriesResponse>, SearchTrajectoriesResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                if (await _sessionService.GetSessionByIdAsync(sessionId, userId) == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                var trajectories = await _trajectoryService.GetSessionTrajectoriesAsync(sessionId, userId);
                return new SearchTrajectoriesResponse
                {
                    Trajectories = trajectories.Select(AdminChatTrajectoryDto.CreateFromEntity).ToList()
                };
            });
        }

        /// <summary>
        /// Resumes a stored native harness trajectory with a new user instruction.
        /// </summary>
        /// <param name="request">The trajectory, instruction, and optional model identifier.</param>
        /// <returns>A response containing the new user/assistant messages and harness state.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<SendTrajectoryResponse>> ResumeTrajectoryAsync(
            [FromBody] ResumeTrajectoryRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<SendTrajectoryResponse>, SendTrajectoryResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                var trajectory = await _trajectoryService.GetAsync(request?.TrajectoryId ?? 0, userId);
                if (trajectory == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.TrajectoryNotFound"]);
                }

                var userMessage = await _messageService.AddMessageAsync(
                    trajectory.SessionId,
                    ChatMessageRoleType.User,
                    request.Instruction ?? string.Empty);
                var (aiResponse, modelIdentifier, harness) = await _chatAiService.ResumeNativeHarnessResponseAsync(
                    trajectory.Id,
                    userId,
                    request.Instruction,
                    request.AiModelId);
                var assistantMessage = await _messageService.AddMessageAsync(
                    trajectory.SessionId,
                    ChatMessageRoleType.Assistant,
                    aiResponse,
                    modelIdentifier,
                    harness.TrajectoryId,
                    harness.TrajectorySequence,
                    await _messageService.CollectWorkspaceArtifactsAsync(trajectory.SessionId, harness));
                await _sessionService.UpdateLastMessageTimeAsync(trajectory.SessionId);
                await PublishSyncEventAsync(userId, trajectory.SessionId, "messages-changed");

                return new SendTrajectoryResponse
                {
                    MemoryUpdateStatus = _chatAiService.MemoryUpdateStatus,
                    MemoryUpdateResult = _chatAiService.MemoryUpdateResult,
                    MemoryUsage = _chatAiService.MemoryUsage,
                    SessionId = trajectory.SessionId,
                    UserMessage = AdminChatMessageDto.CreateFromEntity(userMessage),
                    AssistantMessage = AdminChatMessageDto.CreateFromEntity(assistantMessage),
                    Harness = harness
                };
            });
        }

        /// <summary>
        /// Creates a new session and forks a native harness trajectory from an existing
        /// trajectory sequence.
        /// </summary>
        /// <param name="request">The source trajectory, branch sequence, instruction, and model.</param>
        /// <returns>A response containing the branch session messages and harness state.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<SendTrajectoryResponse>> ForkTrajectoryAsync(
            [FromBody] ForkTrajectoryRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<SendTrajectoryResponse>, SendTrajectoryResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                var parent = await _trajectoryService.GetAsync(request?.TrajectoryId ?? 0, userId);
                if (parent == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.TrajectoryNotFound"]);
                }

                var branchTitle = string.IsNullOrWhiteSpace(request.Instruction)
                    ? $"分叉：{parent.Title}"
                    : (request.Instruction.Length > 50
                        ? request.Instruction.Substring(0, 50) + "..."
                        : request.Instruction);
                var branchSession = await _sessionService.CreateSessionAsync(branchTitle, userId);
                var parentModules = await _sessionModuleService.GetSessionModulesAsync(parent.SessionId);
                await _sessionModuleService.AddModulesToSessionAsync(
                    branchSession.Id,
                    parentModules.Select(module => (
                        module.XncfModuleUid,
                        module.ModuleName,
                        module.ModuleVersion)).ToList());
                var parentWorkflows = await _sessionWorkflowService.GetSessionWorkflowsAsync(parent.SessionId);
                await _sessionWorkflowService.AddWorkflowsToSessionAsync(
                    branchSession.Id,
                    parentWorkflows.Select(workflow => (
                        workflow.WorkflowId,
                        workflow.WorkflowName,
                        workflow.WorkflowDescription)));

                var userMessage = await _messageService.AddMessageAsync(
                    branchSession.Id,
                    ChatMessageRoleType.User,
                    request.Instruction ?? string.Empty);
                var (aiResponse, modelIdentifier, harness) = await _chatAiService.ForkNativeHarnessResponseAsync(
                    parent.Id,
                    userId,
                    branchSession.Id,
                    request.ForkFromSequence,
                    request.Instruction,
                    request.AiModelId);
                var assistantMessage = await _messageService.AddMessageAsync(
                    branchSession.Id,
                    ChatMessageRoleType.Assistant,
                    aiResponse,
                    modelIdentifier,
                    harness.TrajectoryId,
                    harness.TrajectorySequence,
                    await _messageService.CollectWorkspaceArtifactsAsync(branchSession.Id, harness));
                await _sessionService.UpdateLastMessageTimeAsync(branchSession.Id);
                await PublishSyncEventAsync(userId, branchSession.Id, "messages-changed");

                return new SendTrajectoryResponse
                {
                    MemoryUpdateStatus = _chatAiService.MemoryUpdateStatus,
                    MemoryUpdateResult = _chatAiService.MemoryUpdateResult,
                    MemoryUsage = _chatAiService.MemoryUsage,
                    SessionId = branchSession.Id,
                    UserMessage = AdminChatMessageDto.CreateFromEntity(userMessage),
                    AssistantMessage = AdminChatMessageDto.CreateFromEntity(assistantMessage),
                    Harness = harness
                };
            });
        }

        /// <summary>
        /// Approves or rejects a pending tool call and resumes the native harness trajectory.
        /// </summary>
        /// <param name="request">The trajectory, approval, tool-call, and optional reason data.</param>
        /// <returns>A response containing the resumed assistant message and harness state.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<AppResponseBase<SendTrajectoryResponse>> RespondTrajectoryApprovalAsync(
            [FromBody] TrajectoryApprovalRequest request)
        {
            return await this.GetResponseAsync<AppResponseBase<SendTrajectoryResponse>, SendTrajectoryResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                var trajectory = await _trajectoryService.GetAsync(request?.TrajectoryId ?? 0, userId);
                if (trajectory == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.TrajectoryNotFound"]);
                }

                var (aiResponse, modelIdentifier, harness) = await _chatAiService.RespondNativeHarnessApprovalAsync(
                    trajectory.Id,
                    userId,
                    request.RequestId,
                    request.ToolCallId,
                    request.ToolName,
                    request.ArgumentsJson,
                    request.Approved,
                    request.Reason,
                    request.AiModelId);
                var assistantMessage = await _messageService.AddMessageAsync(
                    trajectory.SessionId,
                    ChatMessageRoleType.Assistant,
                    aiResponse,
                    modelIdentifier,
                    harness.TrajectoryId,
                    harness.TrajectorySequence,
                    await _messageService.CollectWorkspaceArtifactsAsync(trajectory.SessionId, harness));
                await _sessionService.UpdateLastMessageTimeAsync(trajectory.SessionId);
                await PublishSyncEventAsync(userId, trajectory.SessionId, "messages-changed");

                return new SendTrajectoryResponse
                {
                    MemoryUpdateStatus = "unchanged",
                    MemoryUpdateResult = _chatAiService.MemoryUpdateResult,
                    MemoryUsage = _chatAiService.MemoryUsage,
                    SessionId = trajectory.SessionId,
                    AssistantMessage = AdminChatMessageDto.CreateFromEntity(assistantMessage),
                    Harness = harness
                };
            });
        }

        /// <summary>
        /// Sets feedback for one administrator-owned chat message.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="feedback">The feedback value to persist.</param>
        /// <returns>A localized success message.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Put)]
        public async Task<StringAppResponse> SetMessageFeedbackAsync(int messageId, MessageFeedbackType feedback)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                //Legacy comment translated to English.
                var success = await _messageService.SetMessageFeedbackAsync(messageId, userId, feedback);
                if (!success)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.MessageNotFound"]);
                }

                logger.Append($"设置反馈: MessageId={messageId}, UserId={userId}, Feedback={feedback}");
                return _localizer["AdminChat.SetFeedbackSuccess"];
            });
        }

        /// <summary>
        /// Deletes selected messages after verifying that the session belongs to the caller.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="messageIds">A comma-separated list of message identifiers.</param>
        /// <returns>A localized message containing the deleted count.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Delete)]
        public async Task<StringAppResponse> DeleteMessagesAsync(int sessionId, string messageIds)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(sessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                var parsedMessageIds = (messageIds ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id, out var value) ? value : 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                if (!parsedMessageIds.Any())
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SelectAtLeastOneMessage"]);
                }

                var deletedCount = await _messageService.DeleteMessagesAsync(sessionId, parsedMessageIds);
                logger.Append($"批量删除消息: SessionId={sessionId}, DeletedCount={deletedCount}");
                await PublishSyncEventAsync(userId, sessionId, "messages-changed");
                return _localizer["AdminChat.DeleteMessagesSuccess", deletedCount];
            });
        }

        #endregion

        #region Legacy section

        /// <summary>
        /// Adds one or more modules to an administrator-owned session.
        /// </summary>
        /// <param name="request">The session identifier and module descriptors.</param>
        /// <returns>A localized success message.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> AddModulesToSessionAsync([FromBody] AddModulesRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(request.SessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                var modules = (request.Modules ?? new List<ModuleInfo>())
                    .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Uid))
                    .GroupBy(m => m.Uid, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .Select(m => (m.Uid, m.Name, m.Version ?? ""))
                    .ToList();
                if (!modules.Any())
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SelectAtLeastOneModule"]);
                }

                await _sessionModuleService.AddModulesToSessionAsync(request.SessionId, modules);

                logger.Append($"添加模块: SessionId={request.SessionId}, Count={modules.Count}");
                await PublishSyncEventAsync(userId, request.SessionId, "modules-changed");
                return _localizer["AdminChat.AddModulesSuccess"];
            });
        }

        /// <summary>
        /// Replaces the module associations for a session while retaining required modules.
        /// </summary>
        /// <param name="request">The session identifier and desired module list.</param>
        /// <returns>A localized success message.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> SetSessionModulesAsync([FromBody] AddModulesRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(request.SessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                var requestedUids = (request.Modules ?? new List<ModuleInfo>())
                    .Where(module => module != null && !string.IsNullOrWhiteSpace(module.Uid))
                    .Select(module => module.Uid)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                requestedUids.Add(SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID);

                var registers = XncfRegisterManager.RegisterList
                    .Where(register => register != null && requestedUids.Contains(register.Uid))
                    .GroupBy(register => register.Uid, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToArray();
                var availabilityService = ServiceProvider.GetRequiredService<INeuBellModuleAvailabilityService>();
                var openModuleUids = await availabilityService.GetOpenModuleUidsAsync(registers.Select(register => register.Uid));
                openModuleUids = openModuleUids
                    .Append(SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var desiredModules = registers
                    .Where(register => openModuleUids.Contains(register.Uid))
                    .Select(register => (register.Uid, register.Name, register.Version ?? string.Empty))
                    .ToList();
                if (desiredModules.All(module => !string.Equals(
                        module.Uid,
                        SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    desiredModules.Add((
                        SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID,
                        SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID,
                        string.Empty));
                }

                var desiredUidSet = desiredModules
                    .Select(module => module.Uid)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var existingModules = await _sessionModuleService.GetSessionModulesAsync(request.SessionId);
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                await _sessionModuleService.AddModulesToSessionAsync(request.SessionId, desiredModules);
                foreach (var existingModule in existingModules.Where(module => !desiredUidSet.Contains(module.XncfModuleUid)))
                {
                    await _sessionModuleService.RemoveModuleFromSessionAsync(request.SessionId, existingModule.XncfModuleUid);
                }

                logger.Append($"更新模块关联: SessionId={request.SessionId}, Count={desiredModules.Count}");
                await PublishSyncEventAsync(userId, request.SessionId, "modules-changed");
                return _localizer["AdminChat.AddModulesSuccess"];
            });
        }

        /// <summary>
        /// Gets the available modules linked to an administrator-owned session.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns>A response containing mapped session-module DTOs.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetSessionModulesResponse>> GetSessionModulesAsync(int sessionId)
        {
            return await this.GetResponseAsync<AppResponseBase<GetSessionModulesResponse>, GetSessionModulesResponse>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(sessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                var modules = await _sessionModuleService.GetSessionModulesAsync(sessionId);

                return new GetSessionModulesResponse
                {
                    Modules = modules.Select(z => MapModuleDtoWithRegisterInfo(AdminChatSessionModuleDto.CreateFromEntity(z))).ToList()
                };
            });
        }

        /// <summary>
        /// Replaces the Workflow associations for an administrator-owned session.
        /// </summary>
        /// <param name="request">The session identifier and desired Workflow identifiers.</param>
        /// <returns>A localized success message.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
        public async Task<StringAppResponse> SetSessionWorkflowsAsync([FromBody] SetWorkflowsRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var userId = GetCurrentAdminUserInfoId();
                if (userId <= 0)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                }

                var session = await _sessionService.GetSessionByIdAsync(request.SessionId, userId);
                if (session == null)
                {
                    throw new NcfExceptionBase(_localizer["AdminChat.SessionNotFoundOrForbidden"]);
                }

                var provider = ServiceProvider.GetService<IWorkflowFunctionCallingProvider>();
                var available = provider == null
                    ? Array.Empty<WorkflowFunctionCallingDescriptor>()
                    : await provider.GetAvailableAsync(userId).ConfigureAwait(false);
                var requestedIds = (request.WorkflowIds ?? new List<int>())
                    .Where(id => id > 0)
                    .ToHashSet();
                var desired = available
                    .Where(workflow => requestedIds.Contains(workflow.Id))
                    .Select(workflow => (
                        workflow.Id,
                        workflow.Name,
                        workflow.Description))
                    .ToList();

                await _sessionWorkflowService.SetSessionWorkflowsAsync(request.SessionId, desired);
                logger.Append($"更新 Workflow 关联: SessionId={request.SessionId}, Count={desired.Count}");
                await PublishSyncEventAsync(userId, request.SessionId, "workflows-changed");
                return _localizer["AdminChat.AddModulesSuccess"];
            });
        }

        /// <summary>
        /// Lists registered modules that are open and available for AdminChat sessions.
        /// </summary>
        /// <returns>A response containing available module metadata.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetAvailableModulesResponse>> GetAvailableModulesAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<GetAvailableModulesResponse>, GetAvailableModulesResponse>(async (response, logger) =>
            {
                var registers = XncfRegisterManager.RegisterList
                    .Where(register => register != null && !string.IsNullOrWhiteSpace(register.Uid))
                    .GroupBy(register => register.Uid, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToArray();
                var availabilityService = ServiceProvider.GetRequiredService<INeuBellModuleAvailabilityService>();
                var openModuleUids = await availabilityService.GetOpenModuleUidsAsync(registers.Select(register => register.Uid));

                var modules = registers
                    .Where(register => openModuleUids.Contains(register.Uid))
                    .Select(register => new AvailableModuleDto
                    {
                        Uid = register.Uid,
                        Name = register.Name,
                        DisplayName = string.IsNullOrWhiteSpace(register.MenuName) ? register.Name : register.MenuName,
                        Version = register.Version ?? string.Empty,
                        Description = register.Description ?? string.Empty,
                        Icon = register.Icon ?? string.Empty,
                        IsRequired = string.Equals(
                            register.Uid,
                            SiteConfig.SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID,
                            StringComparison.OrdinalIgnoreCase)
                    })
                    .OrderBy(module => module.DisplayName)
                    .ToList();

                return new GetAvailableModulesResponse
                {
                    Modules = modules
                };
            });
        }

        /// <summary>
        /// Lists Workflows available to the current administrator.
        /// </summary>
        /// <returns>A response containing available Workflow descriptors.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetAvailableWorkflowsResponse>> GetAvailableWorkflowsAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<GetAvailableWorkflowsResponse>, GetAvailableWorkflowsResponse>(
                async (response, logger) =>
                {
                    var userId = GetCurrentAdminUserInfoId();
                    if (userId <= 0)
                    {
                        throw new NcfExceptionBase(_localizer["AdminChat.UserNotLoggedIn"]);
                    }

                    var provider = ServiceProvider.GetService<IWorkflowFunctionCallingProvider>();
                    var workflows = provider == null
                        ? Array.Empty<WorkflowFunctionCallingDescriptor>()
                        : await provider.GetAvailableAsync(userId).ConfigureAwait(false);
                    return new GetAvailableWorkflowsResponse
                    {
                        Workflows = workflows.Select(MapAvailableWorkflowDto).ToList()
                    };
                });
        }

        /// <summary>
        /// Lists the fallback and configured AI chat models available to AdminChat.
        /// </summary>
        /// <returns>A response containing model options and AIKernel availability.</returns>
        [ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
        public async Task<AppResponseBase<GetAiModelOptionsResponse>> GetAiModelOptionsAsync()
        {
            return await this.GetResponseAsync<AppResponseBase<GetAiModelOptionsResponse>, GetAiModelOptionsResponse>(async (response, logger) =>
            {
                var optionList = new List<AiModelOptionDto>
                {
                    new AiModelOptionDto
                    {
                        Id = 0,
                        Name = _localizer["AdminChat.FallbackModelName"].Value,
                        Description = _localizer["AdminChat.FallbackModelDescription"].Value,
                        IsDefault = true
                    }
                };

                var aiKernelRegister = XncfRegisterManager.RegisterList.FirstOrDefault(z =>
                    string.Equals(z.Name, "Senparc.Xncf.AIKernel", StringComparison.OrdinalIgnoreCase));

                var aiKernelAvailable = false;
                if (aiKernelRegister != null)
                {
                    var registerManager = new XncfRegisterManager(ServiceProvider);
                    aiKernelAvailable = await registerManager.CheckXncfAvailable(aiKernelRegister);
                }

                if (aiKernelAvailable)
                {
                    var aiModelService = ServiceProvider.GetService(typeof(AIModelService)) as AIModelService;
                    if (aiModelService != null)
                    {
                        var aiModels = await aiModelService.GetFullListAsync(z =>
                            z.Show && z.ConfigModelType == Senparc.Xncf.AIKernel.Domain.Models.ConfigModelType.Chat);

                        optionList.AddRange(aiModels
                            .OrderByDescending(z => z.Show)
                            .ThenBy(z => z.Alias)
                            .Select(z => new AiModelOptionDto
                            {
                                Id = z.Id,
                                Name = !string.IsNullOrWhiteSpace(z.Alias)
                                    ? $"{z.Alias} ({z.ModelId})"
                                    : $"{z.DeploymentName} ({z.ModelId})",
                                Description = string.IsNullOrWhiteSpace(z.Note)
                                    ? $"{z.AiPlatform} | {z.Endpoint}"
                                    : z.Note,
                                IsDefault = false
                            }));
                    }
                }

                return new GetAiModelOptionsResponse
                {
                    AiKernelAvailable = aiKernelAvailable,
                    Models = optionList
                };
            });
        }

        /// <summary>
        /// Enriches a session-module DTO with the current registered module metadata
        /// and fills its display name when the persisted value is empty.
        /// </summary>
        /// <param name="dto">The persisted session-module DTO, or <see langword="null"/>.</param>
        /// <returns>The same DTO instance after enrichment, or <see langword="null"/>.</returns>
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

        /// <summary>
        /// Links the registered AgentsManager module to a session when the module is
        /// present and currently available through the NeuBell module catalog.
        /// </summary>
        /// <param name="sessionId">The session that should receive the module link.</param>
        /// <returns>A task that completes after the conditional link operation.</returns>
        private async Task EnsureAgentsManagerModuleLinkedAsync(int sessionId)
        {
            var register = XncfRegisterManager.RegisterList.FirstOrDefault(module =>
                string.Equals(module.Uid, AgentsManagerModuleUid, StringComparison.OrdinalIgnoreCase));
            if (register == null)
            {
                return;
            }

            var availabilityService = ServiceProvider.GetRequiredService<INeuBellModuleAvailabilityService>();
            var openModuleUids = await availabilityService.GetOpenModuleUidsAsync(new[] { register.Uid });
            if (openModuleUids.Contains(register.Uid, StringComparer.OrdinalIgnoreCase))
            {
                await _sessionModuleService.AddModuleToSessionAsync(
                    sessionId,
                    register.Uid,
                    register.Name,
                    register.Version ?? string.Empty);
            }
        }

        /// <summary>
        /// Maps persisted session-workflow links to DTOs and enriches them with
        /// user-authorized function-calling descriptors when the provider is available.
        /// </summary>
        /// <param name="workflows">The persisted workflow links to map.</param>
        /// <param name="userId">The administrator whose available workflows must be used.</param>
        /// <returns>The mapped workflow DTO list, preserving the input order.</returns>
        private async Task<List<AdminChatSessionWorkflowDto>> MapWorkflowDtosAsync(
            IReadOnlyList<AdminChatSessionWorkflow> workflows,
            int userId)
        {
            var provider = ServiceProvider.GetService<IWorkflowFunctionCallingProvider>();
            var available = provider == null
                ? Array.Empty<WorkflowFunctionCallingDescriptor>()
                : await provider.GetAvailableAsync(userId).ConfigureAwait(false);
            var availableById = available.ToDictionary(workflow => workflow.Id);

            return workflows.Select(workflow =>
            {
                var dto = AdminChatSessionWorkflowDto.CreateFromEntity(workflow);
                if (availableById.TryGetValue(workflow.WorkflowId, out var descriptor))
                {
                    dto.WorkflowName = descriptor.Name;
                    dto.WorkflowDescription = descriptor.Description;
                    dto.Parameters = descriptor.Parameters
                        .Select(parameter => new WorkflowFunctionCallingParameterDto
                        {
                            Name = parameter.Name,
                            Description = parameter.Description
                        })
                        .ToList();
                }
                return dto;
            }).ToList();
        }

        /// <summary>
        /// Converts a workflow function-calling descriptor into the DTO used by
        /// the AdminChat available-workflows endpoint.
        /// </summary>
        /// <param name="workflow">The descriptor exposed by the workflow provider.</param>
        /// <returns>A DTO containing identity, display text, and parameter descriptions.</returns>
        private static AvailableWorkflowDto MapAvailableWorkflowDto(
            WorkflowFunctionCallingDescriptor workflow) =>
            new()
            {
                Id = workflow.Id,
                Name = workflow.Name,
                Description = workflow.Description ?? string.Empty,
                Parameters = workflow.Parameters
                    .Select(parameter => new WorkflowParameterDto
                    {
                        Name = parameter.Name,
                        Description = parameter.Description ?? string.Empty
                    })
                    .ToList()
            };

        #endregion

        #region Legacy section

        #endregion

        /// <summary>
        /// Publishes a lightweight AdminChat synchronization event when an event bus
        /// is configured; otherwise completes synchronously without side effects.
        /// </summary>
        /// <param name="userId">The administrator whose client views should be synchronized.</param>
        /// <param name="sessionId">The affected session identifier.</param>
        /// <param name="action">The client synchronization action name.</param>
        /// <returns>A value task representing the optional event publication.</returns>
        private ValueTask PublishSyncEventAsync(int userId, int sessionId, string action)
        {
            return _eventBus?.PublishAsync(new AdminChatSyncEvent(userId, sessionId, action))
                   ?? ValueTask.CompletedTask;
        }
    }

    #region Legacy section

    /// <summary>Contains the result of creating an AdminChat session.</summary>
    public class CreateSessionResponse
    {
        /// <summary>Gets or sets the new session identifier.</summary>
        public int SessionId { get; set; }
        /// <summary>Gets or sets the effective session title.</summary>
        public string Title { get; set; }
        /// <summary>Gets or sets the initial user message, when supplied.</summary>
        public string InitialMessage { get; set; }
        /// <summary>Gets or sets the selected chat mode.</summary>
        public AdminChatMode Mode { get; set; }
        /// <summary>Gets or sets the linked Workspace identifier.</summary>
        public string WorkspaceId { get; set; }
        /// <summary>Gets or sets whether Workspace writes are enabled.</summary>
        public bool WorkspaceWriteEnabled { get; set; }
        /// <summary>Gets or sets whether Workspace CLI access is enabled.</summary>
        public bool WorkspaceCliEnabled { get; set; }
    }

    /// <summary>Identifies a system-message prompt and its PromptRange alias.</summary>
    public sealed record AdminChatSystemMessagePromptView(string PromptCode, string RangeAlias);

    /// <summary>Describes a relative reorder operation between two sessions.</summary>
    public class ReorderSessionRequest
    {
        /// <summary>Gets or sets the session being moved.</summary>
        public int SessionId { get; set; }
        /// <summary>Gets or sets the session used as the insertion reference.</summary>
        public int TargetSessionId { get; set; }
        /// <summary>Gets or sets whether the source is inserted after the target.</summary>
        public bool InsertAfter { get; set; }
    }

    /// <summary>Identifies a session to archive or restore.</summary>
    public class ArchiveSessionRequest
    {
        /// <summary>Gets or sets the session identifier.</summary>
        public int SessionId { get; set; }
    }

    /// <summary>Identifies a session and its unread state.</summary>
    public class SetSessionUnreadRequest
    {
        /// <summary>Gets or sets the session identifier.</summary>
        public int SessionId { get; set; }
        /// <summary>Gets or sets whether the session should be marked unread.</summary>
        public bool Unread { get; set; }
    }

    /// <summary>Contains a paged collection of AdminChat sessions.</summary>
    public class GetSessionListResponse
    {
        /// <summary>Gets or sets the returned session DTOs.</summary>
        public List<AdminChatSessionDto> Sessions { get; set; }
        /// <summary>Gets or sets the total number of matching sessions.</summary>
        public int TotalCount { get; set; }
    }

    /// <summary>Contains Workspace choices available to the caller.</summary>
    public class GetWorkspaceOptionsResponse
    {
        /// <summary>Gets or sets the available Workspaces.</summary>
        public IReadOnlyList<WorkspaceOptionDto> Workspaces { get; set; } = Array.Empty<WorkspaceOptionDto>();
    }

    /// <summary>Requests creation of a Workspace with a display name.</summary>
    public class CreateWorkspaceRequest
    {
        /// <summary>Gets or sets the requested Workspace name.</summary>
        public string Name { get; set; }
    }

    /// <summary>Requests AI title generation for a session.</summary>
    public class GenerateSessionTitleRequest
    {
        /// <summary>Gets or sets the session identifier.</summary>
        public int SessionId { get; set; }
        /// <summary>Gets or sets the optional AI model identifier.</summary>
        public int AiModelId { get; set; }
    }

    /// <summary>Contains a generated or existing session title.</summary>
    public class GenerateSessionTitleResponse
    {
        /// <summary>Gets or sets the title.</summary>
        public string Title { get; set; }
    }

    /// <summary>Describes a Workspace exposed to the administrator UI.</summary>
    public class WorkspaceOptionDto
    {
        public string Id { get; set; }
        public int OwnerId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime LastAccessedUtc { get; set; }

        /// <summary>Creates a DTO from a Workspace storage descriptor.</summary>
        /// <param name="descriptor">The source Workspace descriptor.</param>
        /// <returns>The mapped Workspace option DTO.</returns>
        public static WorkspaceOptionDto CreateFromDescriptor(WorkspaceDescriptor descriptor)
        {
            return new WorkspaceOptionDto
            {
                Id = descriptor.Id,
                OwnerId = descriptor.OwnerId,
                Name = descriptor.Name,
                CreatedUtc = descriptor.CreatedUtc,
                LastAccessedUtc = descriptor.LastAccessedUtc
            };
        }
    }

    public class WorkspaceEntriesResponse
    {
        public string CurrentPath { get; set; } = string.Empty;
        public IReadOnlyList<WorkspaceEntry> Entries { get; set; } = Array.Empty<WorkspaceEntry>();
    }

    public class WorkspacePathSuggestionsResponse
    {
        public IReadOnlyList<WorkspacePathSuggestionDto> Items { get; set; } = Array.Empty<WorkspacePathSuggestionDto>();
    }

    public class WorkspacePathSuggestionDto
    {
        public string RelativePath { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
    }

    public class WorkspaceFilePreviewDto
    {
        public string Path { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public long Size { get; set; }
        public bool IsText { get; set; }
        public string Content { get; set; }
        public string ContentBase64 { get; set; } = string.Empty;
        public string Etag { get; set; } = string.Empty;
        public bool Windowed { get; set; }
        public int StartLine { get; set; } = 1;
        public int LineCount { get; set; }
        public bool HasMore { get; set; }
        public bool Truncated { get; set; }
    }

    public class SaveWorkspaceFileRequest
    {
        public int SessionId { get; set; }
        public string RelativePath { get; set; } = string.Empty;
        public string Content { get; set; }
        public string ExpectedEtag { get; set; } = string.Empty;
    }

    /// <summary>Contains per-user AdminChat session statistics.</summary>
    public class AdminChatUserStatDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string RealName { get; set; }
        public int TotalSessionCount { get; set; }
        public int ActiveSessionCount { get; set; }
        public int ArchivedSessionCount { get; set; }
        public int DeletedSessionCount { get; set; }
        public int MessageCount { get; set; }
        public DateTime LastActiveTime { get; set; }
    }

    /// <summary>Contains the user-statistics response collection.</summary>
    public class GetUserSessionStatsResponse
    {
        public List<AdminChatUserStatDto> Stats { get; set; } = new List<AdminChatUserStatDto>();
    }

    /// <summary>Contains detailed session information.</summary>
    public class GetSessionDetailResponse
    {
        public AdminChatSessionDto Session { get; set; }
    }

    /// <summary>Contains messages and execution metadata returned by SendMessage.</summary>
    public class SendMessageResponse
    {
        public string MemoryUpdateStatus { get; set; }
        public AdminChatMemoryUpdateResult MemoryUpdateResult { get; set; }
        public AdminChatMemoryUsage MemoryUsage { get; set; }
        public AdminChatMessageDto UserMessage { get; set; }
        public AdminChatMessageDto AssistantMessage { get; set; }

        /// <summary>Gets or sets the chat mode used for the response.</summary>
        public AdminChatMode Mode { get; set; } = AdminChatMode.Simple;
        public int MaxOutputTokens { get; set; } = 2000;
        public AdminChatHarnessOptions HarnessOptions { get; set; } = new();

        /// <summary>Gets or sets the harness steps produced for the request.</summary>
        public IReadOnlyList<AdminChatHarnessStep> HarnessSteps { get; set; }

        /// <summary>
        /// Native MAF Harness Trajectory identifier.
        /// </summary>
        public int TrajectoryId { get; set; }

        /// <summary>
        /// Native MAF Harness Trajectory status.
        /// </summary>
        public AdminChatTrajectoryStatus TrajectoryStatus { get; set; }

        public int TrajectorySequence { get; set; }

        /// <summary>
        /// Events produced during the current Harness request.
        /// </summary>
        public IReadOnlyList<AdminChatTrajectoryEventDto> TrajectoryEvents { get; set; }

        /// <summary>
        /// Tool approvals waiting for the user.
        /// </summary>
        public IReadOnlyList<AdminChatApprovalRequestDto> PendingApprovals { get; set; }
    }

    /// <summary>Contains history-search hits.</summary>
    public class SearchHistoryResponse
    {
        public List<AdminChatHistorySearchHit> Hits { get; set; } = new();
    }

    /// <summary>Contains a page of chat messages and its total count.</summary>
    public class GetMessagesResponse
    {
        public List<AdminChatMessageDto> Messages { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>Contains trajectory metadata and its event page.</summary>
    public class GetTrajectoryResponse
    {
        public AdminChatTrajectoryDto Trajectory { get; set; }
        public List<AdminChatTrajectoryEventDto> Events { get; set; }
    }

    /// <summary>Contains trajectory search results.</summary>
    public class SearchTrajectoriesResponse
    {
        public List<AdminChatTrajectoryDto> Trajectories { get; set; }
    }

    public class ResumeTrajectoryRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int TrajectoryId { get; set; }
        public string Instruction { get; set; }
        public int AiModelId { get; set; }
    }

    public class ForkTrajectoryRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int TrajectoryId { get; set; }
        public int ForkFromSequence { get; set; }
        public string Instruction { get; set; }
        public int AiModelId { get; set; }
    }

    public class TrajectoryApprovalRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int TrajectoryId { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string RequestId { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string ToolCallId { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string ToolName { get; set; }
        public string ArgumentsJson { get; set; }
        public bool Approved { get; set; }
        public string Reason { get; set; }
        public int AiModelId { get; set; }
    }

    public class SendTrajectoryResponse
    {
        public string MemoryUpdateStatus { get; set; }
        public AdminChatMemoryUpdateResult MemoryUpdateResult { get; set; }
        public AdminChatMemoryUsage MemoryUsage { get; set; }
        public int SessionId { get; set; }
        public AdminChatMessageDto UserMessage { get; set; }
        public AdminChatMessageDto AssistantMessage { get; set; }
        public AdminChatHarnessResult Harness { get; set; }
    }

    /// <summary>Requests messages/modules for a trajectory branch.</summary>
    public class AddModulesRequest
    {
        public int SessionId { get; set; }
        public List<ModuleInfo> Modules { get; set; }
    }

    public class SetWorkflowsRequest
    {
        public int SessionId { get; set; }
        public List<int> WorkflowIds { get; set; } = new();
    }

    /// <summary>Describes one XNCF module linked to an AdminChat session.</summary>
    public class ModuleInfo
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
    }

    /// <summary>Contains the modules linked to a session.</summary>
    public class GetSessionModulesResponse
    {
        public List<AdminChatSessionModuleDto> Modules { get; set; }
    }

    /// <summary>Contains modules available for session linking.</summary>
    public class GetAvailableModulesResponse
    {
        public List<AvailableModuleDto> Modules { get; set; }
    }

    public class GetAvailableWorkflowsResponse
    {
        public List<AvailableWorkflowDto> Workflows { get; set; } = new();
    }

    public class AvailableWorkflowDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<WorkflowParameterDto> Parameters { get; set; } = new();
    }

    public class WorkflowParameterDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    /// <summary>Describes one module available for session linking.</summary>
    public class AvailableModuleDto
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public bool IsRequired { get; set; }
    }

    /// <summary>Contains modules available for AI model selection.</summary>
    public class GetAiModelOptionsResponse
    {
        public bool AiKernelAvailable { get; set; }
        public List<AiModelOptionDto> Models { get; set; }
    }

    /// <summary>Describes one selectable AI model option.</summary>
    public class AiModelOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsDefault { get; set; }
    }

    public sealed class AdminChatPromptShottingRequest
    {
        public PromptShottingRequest Shotting { get; set; }
        public int MessageId { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
    }

    public sealed record PromptShottingSaveResponse(
        PromptShottingResult Shotting,
        AdminChatMessageDto Message);

    #endregion
}

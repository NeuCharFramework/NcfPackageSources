/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：MainWindowViewModel.AdminChat.cs
    文件功能描述：DesktopBridge + Admin JWT 保护的快捷聊天状态与命令

    创建标识：Senparc - 20260726
----------------------------------------------------------------*/

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NcfDesktopApp.GUI.Models;

namespace NcfDesktopApp.GUI.ViewModels;

public partial class MainWindowViewModel
{
    [ObservableProperty]
    private string _adminUserName = string.Empty;

    [ObservableProperty]
    private string _adminPassword = string.Empty;

    [ObservableProperty]
    private bool _isAdminAuthenticated;

    [ObservableProperty]
    private bool _isAdminChatBusy;

    [ObservableProperty]
    private bool _isDesktopBridgeAvailableForChat;

    [ObservableProperty]
    private string _adminChatStatusText = "启动 NCF 并连接 DesktopBridge 后可登录。";

    [ObservableProperty]
    private string _chatInput = string.Empty;

    [ObservableProperty]
    private AdminChatSessionSummary? _selectedAdminChatSession;

    private DesktopBridgeCapabilities? _desktopBridgeCapabilities;
    private readonly SemaphoreSlim _adminChatRefreshLock = new(1, 1);
    private int _optimisticMessageId;
    private int _activeStreamingAssistantId;

    public ObservableCollection<AdminChatSessionSummary> AdminChatSessions { get; } = new();

    public ObservableCollection<AdminChatMessage> AdminChatMessages { get; } = new();

    public bool IsAdminLoginVisible => IsDesktopBridgeAvailableForChat && !IsAdminAuthenticated;

    public bool IsAdminChatActive => IsDesktopBridgeAvailableForChat && IsAdminAuthenticated;

    public bool IsAdminChatUnavailable => !IsDesktopBridgeAvailableForChat;

    public NcfMascotPose AdminChatMascotPose => IsAdminChatBusy
        ? NcfMascotPose.Thinking
        : !IsDesktopBridgeAvailableForChat
            ? NcfMascotPose.Warning
            : IsAdminAuthenticated
                ? NcfMascotPose.Idle
                : NcfMascotPose.Wave;

    public string AdminChatAccountText => _adminChatClient.Authentication?.UserName ?? string.Empty;

    public string AdminChatDisabledReason
    {
        get
        {
            if (!_isNcfRunning || string.IsNullOrWhiteSpace(SiteUrl) || SiteUrl == "未启动")
            {
                return "请先启动 NCF 站点。";
            }

            if (_desktopBridgeCapabilities is { SupportsAuthorizedSync: false })
            {
                return "当前 DesktopBridge 版本没有授权同步能力，请更新模块后重启站点。";
            }

            return "快捷聊天只在 DesktopBridge 实时连接时启用；NCF 本身仍可正常使用。";
        }
    }

    partial void OnAdminUserNameChanged(string value)
    {
        AdminLoginCommand.NotifyCanExecuteChanged();
    }

    partial void OnAdminPasswordChanged(string value)
    {
        AdminLoginCommand.NotifyCanExecuteChanged();
    }

    partial void OnChatInputChanged(string value)
    {
        SendAdminChatMessageCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsAdminAuthenticatedChanged(bool value)
    {
        NotifyAdminChatStateChanged();
    }

    partial void OnIsAdminChatBusyChanged(bool value)
    {
        NotifyAdminChatStateChanged();
    }

    partial void OnIsDesktopBridgeAvailableForChatChanged(bool value)
    {
        NotifyAdminChatStateChanged();
    }

    partial void OnSelectedAdminChatSessionChanged(AdminChatSessionSummary? value)
    {
        if (value != null && IsAdminChatActive)
        {
            _ = LoadAdminChatMessagesAsync(value.Id);
        }
    }

    [RelayCommand(CanExecute = nameof(CanAdminLogin))]
    private async Task AdminLogin()
    {
        if (_desktopBridgeSessionToken == null || _desktopBridgeCapabilities?.AuthorizedSyncEndpoint == null)
        {
            AdminChatStatusText = "DesktopBridge 授权同步接口尚未就绪。";
            return;
        }

        IsAdminChatBusy = true;
        AdminChatStatusText = "正在验证 AdminOnly 身份…";
        try
        {
            var authentication = await _adminChatClient.AuthenticateAsync(
                SiteUrl,
                AdminUserName,
                AdminPassword,
                _cancellationTokenSource?.Token ?? CancellationToken.None);

            // 密码只用于本次请求，成功后立即从 ViewModel 清除。
            AdminPassword = string.Empty;
            IsAdminAuthenticated = true;
            AdminChatStatusText = $"{authentication.UserName} 已通过 AdminOnly 验证";
            OnPropertyChanged(nameof(AdminChatAccountText));

            await _desktopBridgeClient.StartAuthorizedSyncAsync(
                SiteUrl,
                _desktopBridgeSessionToken,
                authentication.AccessToken,
                _desktopBridgeCapabilities.AuthorizedSyncEndpoint,
                _cancellationTokenSource?.Token ?? CancellationToken.None);

            await RefreshAdminChatSessionsAsync(loadSelectedMessages: true);
            AddLog($"🔐 管理员 {authentication.UserName} 已连接快捷聊天（令牌仅保存在内存中）");
        }
        catch (AdminChatApiException ex)
        {
            _adminChatClient.ClearAuthentication();
            IsAdminAuthenticated = false;
            AdminChatStatusText = ex.Message;
            AddLog($"🔒 Admin Chat 登录失败: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            await _desktopBridgeClient.StopAuthorizedSyncAsync();
            _adminChatClient.ClearAuthentication();
            IsAdminAuthenticated = false;
            AdminChatStatusText = "登录已取消。";
        }
        catch (Exception ex)
        {
            _adminChatClient.ClearAuthentication();
            IsAdminAuthenticated = false;
            AdminChatStatusText = $"登录失败：{ex.Message}";
        }
        finally
        {
            // 无论成功或失败都不在可绑定的界面状态中保留密码。
            AdminPassword = string.Empty;
            IsAdminChatBusy = false;
        }
    }

    private bool CanAdminLogin()
    {
        return IsDesktopBridgeAvailableForChat &&
               !IsAdminAuthenticated &&
               !IsAdminChatBusy &&
               !string.IsNullOrWhiteSpace(AdminUserName) &&
               !string.IsNullOrEmpty(AdminPassword);
    }

    [RelayCommand]
    private async Task AdminLogout()
    {
        await _desktopBridgeClient.StopAuthorizedSyncAsync();
        _adminChatClient.ClearAuthentication();
        IsAdminAuthenticated = false;
        AdminPassword = string.Empty;
        AdminChatSessions.Clear();
        AdminChatMessages.Clear();
        SelectedAdminChatSession = null;
        AdminChatStatusText = "已退出；JWT 已从内存中清除。";
        OnPropertyChanged(nameof(AdminChatAccountText));
    }

    [RelayCommand(CanExecute = nameof(CanUseAdminChat))]
    private async Task NewAdminChatSession()
    {
        IsAdminChatBusy = true;
        try
        {
            var sessionId = await _adminChatClient.CreateSessionAsync(
                SiteUrl,
                _cancellationTokenSource?.Token ?? CancellationToken.None);
            await RefreshAdminChatSessionsAsync(loadSelectedMessages: false, preferredSessionId: sessionId);
            AdminChatStatusText = "新会话已创建。";
        }
        catch (AdminChatApiException ex)
        {
            HandleAdminChatApiFailure(ex);
        }
        finally
        {
            IsAdminChatBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSendAdminChatMessage))]
    private async Task SendAdminChatMessage()
    {
        var content = ChatInput.Trim();
        if (content.Length == 0)
        {
            return;
        }

        IsAdminChatBusy = true;
        AdminChatStatusText = "Agent 正在处理…";
        try
        {
            var sessionId = SelectedAdminChatSession?.Id ?? 0;
            if (sessionId <= 0)
            {
                sessionId = await _adminChatClient.CreateSessionAsync(
                    SiteUrl,
                    _cancellationTokenSource?.Token ?? CancellationToken.None);
            }

            ChatInput = string.Empty;
            var optimisticUserId = await Dispatcher.UIThread.InvokeAsync(() =>
                AddOptimisticUserMessage(sessionId, content));
            _activeStreamingAssistantId = 0;

            await _adminChatClient.SendMessageStreamingAsync(
                SiteUrl,
                sessionId,
                content,
                onUserMessage: message => ReconcileUserMessage(optimisticUserId, message),
                onToken: chunk => AppendStreamingAssistantChunk(sessionId, chunk),
                onAssistantMessage: message => CompleteStreamingAssistantMessage(message),
                cancellationToken: _cancellationTokenSource?.Token ?? CancellationToken.None);

            await RefreshAdminChatSessionsAsync(loadSelectedMessages: true, preferredSessionId: sessionId);
            AdminChatStatusText = "消息已通过 Admin Chat API 完成，并由 EventBus 通知同步。";
            _activeStreamingAssistantId = 0;
        }
        catch (AdminChatApiException ex)
        {
            RemovePendingStreamingMessages();
            ChatInput = content;
            HandleAdminChatApiFailure(ex);
        }
        catch (OperationCanceledException)
        {
            RemovePendingStreamingMessages();
            ChatInput = content;
            AdminChatStatusText = "发送已取消。";
        }
        finally
        {
            IsAdminChatBusy = false;
        }
    }

    private bool CanSendAdminChatMessage()
    {
        return CanUseAdminChat() && !string.IsNullOrWhiteSpace(ChatInput);
    }

    private bool CanUseAdminChat()
    {
        return IsAdminChatActive && !IsAdminChatBusy;
    }

    private async Task RefreshAdminChatSessionsAsync(
        bool loadSelectedMessages,
        int? preferredSessionId = null)
    {
        if (!IsAdminChatActive)
        {
            return;
        }

        await _adminChatRefreshLock.WaitAsync();
        try
        {
            var selectedId = preferredSessionId ?? SelectedAdminChatSession?.Id;
            var sessions = await _adminChatClient.GetSessionsAsync(
                SiteUrl,
                _cancellationTokenSource?.Token ?? CancellationToken.None);

            AdminChatSessionSummary? selected = null;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AdminChatSessions.Clear();
                foreach (var session in sessions)
                {
                    AdminChatSessions.Add(session);
                }

                selected = AdminChatSessions.FirstOrDefault(item => item.Id == selectedId) ??
                           AdminChatSessions.FirstOrDefault();
                SelectedAdminChatSession = selected;
            });

            if (loadSelectedMessages && selected != null)
            {
                await LoadAdminChatMessagesCoreAsync(selected.Id);
            }
        }
        finally
        {
            _adminChatRefreshLock.Release();
        }
    }

    private async Task LoadAdminChatMessagesAsync(int sessionId)
    {
        await _adminChatRefreshLock.WaitAsync();
        try
        {
            await LoadAdminChatMessagesCoreAsync(sessionId);
        }
        catch (AdminChatApiException ex)
        {
            HandleAdminChatApiFailure(ex);
        }
        finally
        {
            _adminChatRefreshLock.Release();
        }
    }

    private async Task LoadAdminChatMessagesCoreAsync(int sessionId)
    {
        if (!IsAdminChatActive)
        {
            return;
        }

        var messages = await _adminChatClient.GetSessionMessagesAsync(
            SiteUrl,
            sessionId,
            _cancellationTokenSource?.Token ?? CancellationToken.None);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (SelectedAdminChatSession?.Id != sessionId)
            {
                return;
            }

            AdminChatMessages.Clear();
            foreach (var message in messages)
            {
                AdminChatMessages.Add(message);
            }
        });
    }

    private void UpdateAdminChatBridgeState(DesktopBridgeProbeResult result)
    {
        if (result.Capabilities != null)
        {
            _desktopBridgeCapabilities = result.Capabilities;
        }

        IsDesktopBridgeAvailableForChat = result.IsAvailable &&
                                          _desktopBridgeCapabilities?.SupportsAuthorizedSync == true &&
                                          !string.IsNullOrWhiteSpace(_desktopBridgeCapabilities.AuthorizedSyncEndpoint);

        if (!IsDesktopBridgeAvailableForChat)
        {
            AdminChatStatusText = AdminChatDisabledReason;
        }
        else if (!IsAdminAuthenticated)
        {
            AdminChatStatusText = "DesktopBridge 已连接，请使用后台管理员账号登录。";
        }

        OnPropertyChanged(nameof(AdminChatDisabledReason));
    }

    private void OnDesktopAuthorizedSyncReceived(DesktopAuthorizedSyncMessage message)
    {
        if (!string.Equals(message.Channel, "admin-chat", StringComparison.OrdinalIgnoreCase) ||
            !IsAdminChatActive)
        {
            return;
        }

        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var sessionId = int.TryParse(message.ResourceId, out var parsed) ? parsed : (int?)null;
                await RefreshAdminChatSessionsAsync(
                    loadSelectedMessages: sessionId == SelectedAdminChatSession?.Id,
                    preferredSessionId: SelectedAdminChatSession?.Id);
                AdminChatStatusText = "已收到 EventBus 同步通知。";
            }
            catch (AdminChatApiException ex)
            {
                HandleAdminChatApiFailure(ex);
            }
        });
    }

    private void OnDesktopAuthorizedSyncAuthorizationFailed(string message)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            await _desktopBridgeClient.StopAuthorizedSyncAsync();
            _adminChatClient.ClearAuthentication();
            IsAdminAuthenticated = false;
            AdminChatSessions.Clear();
            AdminChatMessages.Clear();
            SelectedAdminChatSession = null;
            AdminChatStatusText = message;
            OnPropertyChanged(nameof(AdminChatAccountText));
        });
    }

    private void HandleAdminChatApiFailure(AdminChatApiException ex)
    {
        AdminChatStatusText = ex.Message;
        if (!ex.IsAuthenticationFailure)
        {
            return;
        }

        _adminChatClient.ClearAuthentication();
        IsAdminAuthenticated = false;
        AdminChatSessions.Clear();
        AdminChatMessages.Clear();
        SelectedAdminChatSession = null;
        OnPropertyChanged(nameof(AdminChatAccountText));
    }

    private void ResetAdminChatState()
    {
        _adminChatClient.ClearAuthentication();
        _desktopBridgeCapabilities = null;
        Dispatcher.UIThread.Post(() =>
        {
            IsAdminAuthenticated = false;
            IsDesktopBridgeAvailableForChat = false;
            AdminPassword = string.Empty;
            AdminChatSessions.Clear();
            AdminChatMessages.Clear();
            SelectedAdminChatSession = null;
            AdminChatStatusText = "启动 NCF 并连接 DesktopBridge 后可登录。";
            OnPropertyChanged(nameof(AdminChatAccountText));
            OnPropertyChanged(nameof(AdminChatDisabledReason));
        });
    }

    private void NotifyAdminChatStateChanged()
    {
        OnPropertyChanged(nameof(AdminChatMascotPose));
        OnPropertyChanged(nameof(IsAdminLoginVisible));
        OnPropertyChanged(nameof(IsAdminChatActive));
        OnPropertyChanged(nameof(IsAdminChatUnavailable));
        OnPropertyChanged(nameof(AdminChatDisabledReason));
        AdminLoginCommand.NotifyCanExecuteChanged();
        NewAdminChatSessionCommand.NotifyCanExecuteChanged();
        SendAdminChatMessageCommand.NotifyCanExecuteChanged();
    }

    private int AddOptimisticUserMessage(int sessionId, string content)
    {
        var id = -Interlocked.Increment(ref _optimisticMessageId);
        var sequence = AdminChatMessages.Count == 0
            ? 1
            : AdminChatMessages.Max(message => message.Sequence) + 1;
        AdminChatMessages.Add(new AdminChatMessage(
            id,
            sessionId,
            0,
            content,
            sequence,
            DateTime.Now,
            null));
        return id;
    }

    private void ReconcileUserMessage(int optimisticId, AdminChatMessage message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var optimisticIndex = FindMessageIndex(optimisticId);
            if (optimisticIndex >= 0 && optimisticIndex < AdminChatMessages.Count)
            {
                AdminChatMessages.RemoveAt(optimisticIndex);
            }

            AddOrReplaceAdminChatMessage(message);
        });
    }

    private void AppendStreamingAssistantChunk(int sessionId, string chunk)
    {
        if (string.IsNullOrEmpty(chunk))
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (_activeStreamingAssistantId == 0)
            {
                _activeStreamingAssistantId = -Interlocked.Increment(ref _optimisticMessageId);
                var sequence = AdminChatMessages.Count == 0
                    ? 1
                    : AdminChatMessages.Max(message => message.Sequence) + 1;
                AdminChatMessages.Add(new AdminChatMessage(
                    _activeStreamingAssistantId,
                    sessionId,
                    1,
                    chunk,
                    sequence,
                    DateTime.Now,
                    null));
                return;
            }

            var index = FindMessageIndex(_activeStreamingAssistantId);
            if (index < 0 || index >= AdminChatMessages.Count)
            {
                return;
            }

            var existing = AdminChatMessages[index];
            AdminChatMessages[index] = existing with { Content = existing.Content + chunk };
        });
    }

    private void CompleteStreamingAssistantMessage(AdminChatMessage message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            RemoveMessageById(_activeStreamingAssistantId);
            AddOrReplaceAdminChatMessage(message);
            _activeStreamingAssistantId = 0;
        });
    }

    private void RemovePendingStreamingMessages()
    {
        Dispatcher.UIThread.Post(() =>
        {
            RemoveMessageById(_activeStreamingAssistantId);
            _activeStreamingAssistantId = 0;
            for (var index = AdminChatMessages.Count - 1; index >= 0; index--)
            {
                if (AdminChatMessages[index].Id < 0 && AdminChatMessages[index].IsUser)
                {
                    AdminChatMessages.RemoveAt(index);
                }
            }
        });
    }

    private void AddOrReplaceAdminChatMessage(AdminChatMessage message)
    {
        var index = FindMessageIndex(message.Id);
        if (index >= 0 && index < AdminChatMessages.Count)
        {
            AdminChatMessages[index] = message;
        }
        else
        {
            AdminChatMessages.Add(message);
        }
    }

    private void RemoveMessageById(int id)
    {
        if (id == 0)
        {
            return;
        }

        var index = FindMessageIndex(id);
        if (index >= 0 && index < AdminChatMessages.Count)
        {
            AdminChatMessages.RemoveAt(index);
        }
    }

    private int FindMessageIndex(int id)
    {
        for (var index = 0; index < AdminChatMessages.Count; index++)
        {
            if (AdminChatMessages[index].Id == id)
            {
                return index;
            }
        }

        return -1;
    }
}

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using NcfDesktopApp.GUI.ViewModels;
using System;
using System.Collections.Specialized;

namespace NcfDesktopApp.GUI.Views;

public partial class SettingsView : UserControl
{
    private bool _isUserScrolling = false;
    private bool _isChatUserScrolling;
    private INotifyCollectionChanged? _chatMessages;
    
    public SettingsView()
    {
        InitializeComponent();
    }

    private void SettingsView_OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_chatMessages != null)
        {
            _chatMessages.CollectionChanged -= ChatMessages_OnCollectionChanged;
            _chatMessages = null;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            _chatMessages = viewModel.AdminChatMessages;
            _chatMessages.CollectionChanged += ChatMessages_OnCollectionChanged;
            ScheduleChatScrollToEnd();
        }
    }

    private void ChatMessages_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_isChatUserScrolling)
        {
            ScheduleChatScrollToEnd();
        }
    }

    private void ChatScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        var scrollableHeight = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
        var distanceFromBottom = Math.Max(0, scrollableHeight - scrollViewer.Offset.Y);
        if (e.ExtentDelta.Y == 0 && e.OffsetDelta.Y != 0)
        {
            _isChatUserScrolling = distanceFromBottom > 24;
        }
        else if (distanceFromBottom <= 24)
        {
            _isChatUserScrolling = false;
        }

        if (e.ExtentDelta.Y != 0 && !_isChatUserScrolling)
        {
            ScheduleChatScrollToEnd();
        }
    }

    private void ScheduleChatScrollToEnd()
    {
        Dispatcher.UIThread.Post(() =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!_isChatUserScrolling && ChatScrollViewer.Extent.Height > ChatScrollViewer.Viewport.Height)
                {
                    ChatScrollViewer.ScrollToEnd();
                }
            }, DispatcherPriority.Render);
        }, DispatcherPriority.Background);
    }

    private void ChatInput_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter ||
            (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) == 0 ||
            DataContext is not MainWindowViewModel viewModel ||
            !viewModel.SendAdminChatMessageCommand.CanExecute(null))
        {
            return;
        }

        e.Handled = true;
        viewModel.SendAdminChatMessageCommand.Execute(null);
    }
    
    /// <summary>
    /// 当滚动条位置改变时触发
    /// </summary>
    private void LogScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer) return;
        
        try
        {
            // 检查用户是否手动滚动（不是在底部）
            var scrollableHeight = scrollViewer.Extent.Height - scrollViewer.Viewport.Height;
            if (scrollableHeight > 0)
            {
                var distanceFromBottom = scrollableHeight - scrollViewer.Offset.Y;
                
                // 如果距离底部超过 20 像素，说明用户在查看历史日志
                _isUserScrolling = distanceFromBottom > 20;
            }
            else
            {
                _isUserScrolling = false;
            }
        }
        catch
        {
            // 忽略滚动检查错误
        }
    }
    
    /// <summary>
    /// 获取是否应该自动滚动到底部
    /// </summary>
    public bool ShouldAutoScroll => !_isUserScrolling;
}

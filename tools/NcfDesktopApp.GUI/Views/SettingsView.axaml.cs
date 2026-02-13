using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using System;

namespace NcfDesktopApp.GUI.Views;

public partial class SettingsView : UserControl
{
    private bool _isUserScrolling = false;
    
    public SettingsView()
    {
        InitializeComponent();
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
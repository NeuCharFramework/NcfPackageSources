/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：DesktopRobotViewModel.cs
    文件功能描述：桌面机器人显示状态与兼容模式日志映射

    创建标识：Senparc - 20260725
----------------------------------------------------------------*/

using System;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using NcfDesktopApp.GUI.Models;

namespace NcfDesktopApp.GUI.ViewModels;

public partial class DesktopRobotViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _emoji = "🤖";

    [ObservableProperty]
    private string _title = "NCF 桌面助手";

    [ObservableProperty]
    private string _detail = "等待启动 NCF";

    [ObservableProperty]
    private string _stateText = "待机";

    [ObservableProperty]
    private string _stateColor = "#6C757D";

    [ObservableProperty]
    private string _connectionText = "尚未连接";

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isProgressVisible;

    public void SetProcessState(string state, string detail, bool isError = false)
    {
        RunOnUi(() =>
        {
            Title = "NCF 桌面助手";
            Detail = detail;
            StateText = state;
            Emoji = isError ? "🛠️" : state switch
            {
                "运行中" => "🤖",
                "启动中" => "🚀",
                "已完成" => "✅",
                "已停止" => "💤",
                _ => "🤖"
            };
            StateColor = isError ? "#DC3545" : state switch
            {
                "运行中" => "#007ACC",
                "启动中" => "#6F42C1",
                "已完成" => "#28A745",
                "已停止" => "#6C757D",
                _ => "#6C757D"
            };
            IsProgressVisible = false;
        });
    }

    public void SetBridgeAvailability(DesktopBridgeProbeResult result)
    {
        RunOnUi(() =>
        {
            ConnectionText = result.Availability switch
            {
                DesktopBridgeAvailability.Available => "DesktopBridge 实时模式",
                DesktopBridgeAvailability.NotInstalled => "兼容模式 · 未安装 Bridge",
                DesktopBridgeAvailability.Incompatible => "兼容模式 · Bridge 待更新",
                DesktopBridgeAvailability.Unauthorized => "兼容模式 · 会话无效",
                DesktopBridgeAvailability.Inactive => "兼容模式 · Bridge 未启用",
                DesktopBridgeAvailability.Unavailable => "兼容模式 · 后台重连中",
                _ => "兼容模式"
            };
        });
    }

    public void ApplyActivity(DesktopActivityMessage activity)
    {
        RunOnUi(() =>
        {
            Title = string.IsNullOrWhiteSpace(activity.Source)
                ? activity.Title
                : $"{activity.Source} · {activity.Title}";
            Detail = string.IsNullOrWhiteSpace(activity.Detail) ? "NCF 正在处理系统任务" : activity.Detail;
            StateText = activity.State switch
            {
                "Working" => "工作中",
                "Succeeded" => "已完成",
                "Failed" => "发生错误",
                "Cancelled" => "已取消",
                _ => "新动态"
            };
            Emoji = activity.State switch
            {
                "Working" => "⚙️",
                "Succeeded" => "✅",
                "Failed" => "🛠️",
                "Cancelled" => "⏹️",
                _ => "🤖"
            };
            StateColor = activity.State switch
            {
                "Working" => "#007ACC",
                "Succeeded" => "#28A745",
                "Failed" => "#DC3545",
                "Cancelled" => "#6C757D",
                _ => "#6F42C1"
            };
            IsProgressVisible = activity.Progress.HasValue;
            Progress = activity.Progress ?? 0;
        });
    }

    public void ApplyCompatibilityLog(string message, bool isError)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var looksLikeError = isError || ContainsAny(message, "error", "exception", "失败", "错误");
        var looksLikeCompleted = ContainsAny(message, "completed", "complete", "成功", "完成", "已启动");
        var looksLikeWork = ContainsAny(message, "starting", "processing", "running", "开始", "正在", "处理中");
        if (!looksLikeError && !looksLikeCompleted && !looksLikeWork)
        {
            return;
        }

        var detail = message.Length <= 180 ? message : message[..180] + "…";
        RunOnUi(() =>
        {
            Detail = detail;
            IsProgressVisible = false;
            if (looksLikeError)
            {
                Emoji = "🛠️";
                StateText = "发生错误";
                StateColor = "#DC3545";
            }
            else if (looksLikeCompleted)
            {
                Emoji = "✅";
                StateText = "已完成";
                StateColor = "#28A745";
            }
            else
            {
                Emoji = "⚙️";
                StateText = "工作中";
                StateColor = "#007ACC";
            }
        });
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }

    private static void RunOnUi(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Dispatcher.UIThread.Post(action);
        }
    }
}

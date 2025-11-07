# 阶段 4️⃣: 功能完善和优化

## 📋 步骤信息
- **步骤ID**: step-04
- **步骤名称**: 功能完善和性能优化
- **预计时间**: 5.5 小时
- **优先级**: 中
- **状态**: ⏳ 待开始

## 🎯 目标
完善内嵌浏览器的用户体验，包括加载进度显示、错误处理、内存优化等功能。

## 📂 涉及文件
- `Views/BrowserView.axaml` - 添加进度条和状态显示
- `Views/Controls/WebViewLoadingOverlay.cs` - 新建，加载遮罩层
- `ViewModels/MainWindowViewModel.cs` - 优化状态管理
- `Services/WebViewResourceManager.cs` - 新建，资源管理

## 🔨 实施步骤

### 1. 实现加载进度显示 (1小时)

**修改 `Views/BrowserView.axaml`**：

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:controls="using:NcfDesktopApp.GUI.Views.Controls"
             x:Class="NcfDesktopApp.GUI.Views.BrowserView">
    
    <Grid>
        <!-- WebView 内容 -->
        <controls:EmbeddedWebView Name="WebViewControl"
                                   Source="{Binding SiteUrl}"/>
        
        <!-- 加载进度遮罩 -->
        <Border Name="LoadingOverlay"
                Background="#CC000000"
                IsVisible="{Binding IsPageLoading}"
                IsHitTestVisible="True">
            <StackPanel HorizontalAlignment="Center"
                        VerticalAlignment="Center"
                        Spacing="20">
                
                <!-- 加载动画 -->
                <Viewbox Width="60" Height="60">
                    <Canvas Width="100" Height="100">
                        <Path Name="LoadingSpinner"
                              Stroke="White"
                              StrokeThickness="8"
                              Data="M 50,10 A 40,40 0 1,1 49.9,10">
                            <Path.RenderTransform>
                                <RotateTransform Angle="0" CenterX="50" CenterY="50"/>
                            </Path.RenderTransform>
                        </Path>
                    </Canvas>
                </Viewbox>
                
                <!-- 进度文本 -->
                <TextBlock Text="{Binding LoadingMessage}"
                           FontSize="16"
                           Foreground="White"
                           HorizontalAlignment="Center"/>
                
                <!-- 进度条 -->
                <ProgressBar Value="{Binding LoadingProgress}"
                             Width="300"
                             Height="4"
                             IsIndeterminate="{Binding IsLoadingIndeterminate}"
                             Foreground="#4CAF50"/>
                
                <!-- 取消按钮 -->
                <Button Content="取消加载"
                        Command="{Binding CancelLoadingCommand}"
                        Padding="20,8"
                        Background="#F44336"
                        Foreground="White"/>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

**添加旋转动画** (在 `BrowserView.axaml.cs`)：

```csharp
private void StartLoadingAnimation()
{
    var spinner = this.FindControl<Path>("LoadingSpinner");
    if (spinner != null)
    {
        var animation = new Animation
        {
            Duration = TimeSpan.FromSeconds(1),
            IterationCount = IterationCount.Infinite
        };

        var keyFrame = new KeyFrame
        {
            Setters = { new Setter(RotateTransform.AngleProperty, 360.0) },
            Cue = new Cue(1.0)
        };

        animation.Children.Add(keyFrame);
        animation.RunAsync(spinner.RenderTransform);
    }
}
```

### 2. 添加错误处理和重试机制 (1.5小时)

**新建 `Views/Controls/WebViewErrorView.cs`**：

```csharp
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace NcfDesktopApp.GUI.Views.Controls;

/// <summary>
/// WebView 错误显示视图
/// </summary>
public class WebViewErrorView : UserControl
{
    public static readonly StyledProperty<string> ErrorMessageProperty =
        AvaloniaProperty.Register<WebViewErrorView, string>(nameof(ErrorMessage), "");

    public static readonly StyledProperty<string> ErrorCodeProperty =
        AvaloniaProperty.Register<WebViewErrorView, string>(nameof(ErrorCode), "");

    public string ErrorMessage
    {
        get => GetValue(ErrorMessageProperty);
        set => SetAndRaise(ErrorMessageProperty, value);
    }

    public string ErrorCode
    {
        get => GetValue(ErrorCodeProperty);
        set => SetAndRaise(ErrorCodeProperty, value);
    }

    public event EventHandler? RetryClicked;
    public event EventHandler? OpenExternalClicked;

    public WebViewErrorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var mainPanel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 20,
            Margin = new Thickness(40)
        };

        // 错误图标
        var errorIcon = new TextBlock
        {
            Text = "⚠️",
            FontSize = 64,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // 错误标题
        var errorTitle = new TextBlock
        {
            Text = "页面加载失败",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = new SolidColorBrush(Color.Parse("#F44336"))
        };

        // 错误消息
        var errorMsgText = new TextBlock
        {
            [!TextBlock.TextProperty] = this[!ErrorMessageProperty],
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            MaxWidth = 500,
            Foreground = Brushes.Gray
        };

        // 错误代码
        var errorCodeText = new TextBlock
        {
            [!TextBlock.TextProperty] = this[!ErrorCodeProperty],
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.DarkGray,
            FontFamily = new FontFamily("Consolas, monospace")
        };

        // 建议面板
        var suggestionsPanel = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#FFF3CD")),
            BorderBrush = new SolidColorBrush(Color.Parse("#FFE69C")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(15),
            Margin = new Thickness(0, 10, 0, 0)
        };

        var suggestionsList = new StackPanel { Spacing = 5 };
        suggestionsList.Children.Add(CreateSuggestion("检查 NCF 应用是否正常运行"));
        suggestionsList.Children.Add(CreateSuggestion("确认端口号是否正确"));
        suggestionsList.Children.Add(CreateSuggestion("检查防火墙设置"));
        suggestionsList.Children.Add(CreateSuggestion("尝试在外部浏览器中打开"));

        suggestionsPanel.Child = suggestionsList;

        // 操作按钮
        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 10,
            Margin = new Thickness(0, 20, 0, 0)
        };

        var retryButton = new Button
        {
            Content = "🔄 重试",
            Padding = new Thickness(25, 10),
            Background = new SolidColorBrush(Color.Parse("#4CAF50")),
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4)
        };
        retryButton.Click += (s, e) => RetryClicked?.Invoke(this, EventArgs.Empty);

        var externalButton = new Button
        {
            Content = "🌍 外部浏览器",
            Padding = new Thickness(25, 10),
            Background = new SolidColorBrush(Color.Parse("#2196F3")),
            Foreground = Brushes.White,
            CornerRadius = new CornerRadius(4)
        };
        externalButton.Click += (s, e) => OpenExternalClicked?.Invoke(this, EventArgs.Empty);

        buttonsPanel.Children.Add(retryButton);
        buttonsPanel.Children.Add(externalButton);

        // 组装
        mainPanel.Children.Add(errorIcon);
        mainPanel.Children.Add(errorTitle);
        mainPanel.Children.Add(errorMsgText);
        mainPanel.Children.Add(errorCodeText);
        mainPanel.Children.Add(suggestionsPanel);
        mainPanel.Children.Add(buttonsPanel);

        Content = mainPanel;
    }

    private Control CreateSuggestion(string text)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 5
        };

        panel.Children.Add(new TextBlock
        {
            Text = "•",
            Foreground = new SolidColorBrush(Color.Parse("#856404"))
        });

        panel.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.Parse("#856404"))
        });

        return panel;
    }
}
```

### 3. 优化内存和资源管理 (2小时)

**新建 `Services/WebViewResourceManager.cs`**：

```csharp
using System;
using System.Diagnostics;
using System.Timers;

namespace NcfDesktopApp.GUI.Services;

/// <summary>
/// WebView 资源管理器
/// 负责监控和优化 WebView 的资源使用
/// </summary>
public class WebViewResourceManager : IDisposable
{
    private Timer? _memoryCheckTimer;
    private long _lastMemoryUsage = 0;
    private const long MemoryWarningThreshold = 500 * 1024 * 1024; // 500MB
    private bool _disposed = false;

    public event EventHandler<MemoryUsageEventArgs>? MemoryWarningTriggered;

    public WebViewResourceManager()
    {
        StartMemoryMonitoring();
    }

    private void StartMemoryMonitoring()
    {
        _memoryCheckTimer = new Timer(30000); // 每30秒检查一次
        _memoryCheckTimer.Elapsed += OnMemoryCheck;
        _memoryCheckTimer.Start();
        
        Debug.WriteLine("✅ 内存监控已启动");
    }

    private void OnMemoryCheck(object? sender, ElapsedEventArgs e)
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            var memoryUsage = currentProcess.WorkingSet64;
            var memoryChange = memoryUsage - _lastMemoryUsage;

            Debug.WriteLine($"📊 内存使用: {FormatBytes(memoryUsage)} (变化: {FormatBytes(memoryChange)})");

            // 如果内存使用超过阈值，触发警告
            if (memoryUsage > MemoryWarningThreshold)
            {
                Debug.WriteLine($"⚠️ 内存使用过高: {FormatBytes(memoryUsage)}");
                MemoryWarningTriggered?.Invoke(this, new MemoryUsageEventArgs
                {
                    CurrentUsage = memoryUsage,
                    Threshold = MemoryWarningThreshold,
                    ShouldCollect = true
                });

                // 触发垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Debug.WriteLine("🧹 已执行垃圾回收");
            }

            _lastMemoryUsage = memoryUsage;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ 内存检查失败: {ex.Message}");
        }
    }

    public MemoryInfo GetCurrentMemoryInfo()
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            return new MemoryInfo
            {
                WorkingSet = currentProcess.WorkingSet64,
                PrivateMemory = currentProcess.PrivateMemorySize64,
                VirtualMemory = currentProcess.VirtualMemorySize64,
                ManagedMemory = GC.GetTotalMemory(false)
            };
        }
        catch
        {
            return new MemoryInfo();
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _memoryCheckTimer?.Stop();
            _memoryCheckTimer?.Dispose();
            _disposed = true;
            Debug.WriteLine("🛑 资源管理器已清理");
        }
    }
}

public class MemoryUsageEventArgs : EventArgs
{
    public long CurrentUsage { get; set; }
    public long Threshold { get; set; }
    public bool ShouldCollect { get; set; }
}

public class MemoryInfo
{
    public long WorkingSet { get; set; }
    public long PrivateMemory { get; set; }
    public long VirtualMemory { get; set; }
    public long ManagedMemory { get; set; }

    public override string ToString()
    {
        return $"Working Set: {FormatBytes(WorkingSet)}, " +
               $"Private: {FormatBytes(PrivateMemory)}, " +
               $"Virtual: {FormatBytes(VirtualMemory)}, " +
               $"Managed: {FormatBytes(ManagedMemory)}";
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
```

### 4. 添加开发者工具支持（可选） (1小时)

**为 Windows WebView2 添加开发者工具快捷键**：

```csharp
// 在 WindowsWebView2Control.cs 中添加
public void OpenDevTools()
{
#if WINDOWS
    if (_webView2?.CoreWebView2 != null)
    {
        _webView2.CoreWebView2.OpenDevToolsWindow();
        Debug.WriteLine("🔧 开发者工具已打开");
    }
#endif
}

// 在 MainWindow 中添加快捷键处理
protected override void OnKeyDown(KeyEventArgs e)
{
    base.OnKeyDown(e);
    
    // F12 打开开发者工具
    if (e.Key == Key.F12)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.OpenDevTools();
        }
    }
}
```

## ✅ 验收标准

### 功能验收
- [ ] 加载进度正确显示
- [ ] 错误页面友好展示
- [ ] 重试功能正常工作
- [ ] 内存监控有效
- [ ] 开发者工具可用（Windows）

### 技术验收
- [ ] 代码编译通过
- [ ] 动画流畅
- [ ] 资源正确释放

### 质量验收
- [ ] 用户体验友好
- [ ] 性能良好
- [ ] 错误提示清晰

## 🧪 测试方法

1. **加载进度测试**：访问大型页面，观察进度显示
2. **错误处理测试**：访问无效URL，检查错误页面
3. **内存测试**：长时间使用，监控内存变化
4. **开发者工具**：按F12测试开发者工具

## 📝 注意事项

- 进度显示要平滑自然
- 错误信息要准确友好
- 内存监控不能影响性能
- 开发者工具仅在需要时启用

---

**状态**: ⏳ 待开始  
**优先级**: 中  
**依赖**: step-01, step-02, step-03  
**预计时间**: 5.5小时


using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

#if WINDOWS
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
#endif

namespace NcfDesktopApp.GUI.Views.Controls;

/// <summary>
/// Windows 原生 WebView2 控件封装（仅在 Windows 平台可用）
/// 这是真正的浏览器控件，基于 Chromium Edge
/// </summary>
public class WindowsWebView2Control : NativeControlHost
{
    public static readonly DirectProperty<WindowsWebView2Control, string> SourceProperty =
        AvaloniaProperty.RegisterDirect<WindowsWebView2Control, string>(
            nameof(Source),
            o => o.Source,
            (o, v) => o.Source = v);

    private string _source = "";
    public string Source
    {
        get => _source;
        set
        {
            SetAndRaise(SourceProperty, ref _source, value);
            if (_isInitialized && !string.IsNullOrEmpty(value))
            {
                _ = NavigateAsync(value);
            }
        }
    }

#if WINDOWS
    private WebView2? _webView2;
#endif
    private bool _isInitialized = false;

    public WindowsWebView2Control()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            throw new PlatformNotSupportedException("WindowsWebView2Control 只能在 Windows 平台使用");
        }
        
        Debug.WriteLine("🪟 WindowsWebView2Control 构造函数");
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
#if WINDOWS
        Debug.WriteLine("🔨 CreateNativeControlCore 开始创建 WebView2");
        
        _webView2 = new WebView2
        {
            CreationProperties = new CoreWebView2CreationProperties
            {
                // 使用默认的用户数据文件夹
                // UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NCFDesktopApp")
            }
        };
        
        // 订阅 Loaded 事件，在控件加载后初始化 CoreWebView2
        _webView2.Loaded += async (s, e) =>
        {
            Debug.WriteLine("📍 WebView2 Loaded 事件触发");
            await InitializeWebView2Async();
        };
        
        Debug.WriteLine($"✅ WebView2 控件已创建，Handle: {_webView2.Handle}");
        return new PlatformHandle(_webView2.Handle, "HWND");
#else
        throw new PlatformNotSupportedException("此控件仅支持 Windows 平台");
#endif
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
#if WINDOWS
        Debug.WriteLine("🗑️ DestroyNativeControlCore 销毁 WebView2");
        if (_webView2 != null)
        {
            try
            {
                _webView2.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ WebView2 销毁异常: {ex.Message}");
            }
            _webView2 = null;
        }
#endif
        base.DestroyNativeControlCore(control);
    }

#if WINDOWS
    private async Task InitializeWebView2Async()
    {
        try
        {
            Debug.WriteLine("🚀 开始初始化 WebView2 CoreWebView2...");
            
            if (_webView2 == null)
            {
                Debug.WriteLine("❌ _webView2 为 null，无法初始化");
                OnNavigationFailed("WebView2 控件未创建");
                return;
            }
            
            // 确保 CoreWebView2 已初始化
            await _webView2.EnsureCoreWebView2Async();
            
            if (_webView2.CoreWebView2 == null)
            {
                Debug.WriteLine("❌ CoreWebView2 初始化失败");
                OnNavigationFailed("CoreWebView2 初始化失败");
                return;
            }
            
            Debug.WriteLine("✅ CoreWebView2 初始化成功");
            
            // 配置 WebView2 设置
            _webView2.CoreWebView2.Settings.IsWebMessageEnabled = true;
            _webView2.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
            _webView2.CoreWebView2.Settings.AreHostObjectsAllowed = true;
            _webView2.CoreWebView2.Settings.IsScriptEnabled = true;
            _webView2.CoreWebView2.Settings.AreDevToolsEnabled = true;
            _webView2.CoreWebView2.Settings.IsZoomControlEnabled = true;
            _webView2.CoreWebView2.Settings.IsStatusBarEnabled = false;
            
            Debug.WriteLine("✅ WebView2 设置已配置");
            
            // 订阅导航事件
            _webView2.CoreWebView2.NavigationStarting += (s, e) =>
            {
                Debug.WriteLine($"🚢 导航开始: {e.Uri}");
                OnNavigationStarted(e.Uri);
            };
            
            _webView2.CoreWebView2.NavigationCompleted += (s, e) =>
            {
                Debug.WriteLine($"✅ 导航完成: {e.Uri}, IsSuccess: {e.IsSuccess}");
                if (e.IsSuccess)
                {
                    OnNavigationCompleted(e.Uri ?? "");
                }
                else
                {
                    Debug.WriteLine($"❌ 导航失败: WebErrorStatus={e.WebErrorStatus}");
                    OnNavigationFailed($"导航失败: {e.WebErrorStatus}");
                }
            };
            
            _webView2.CoreWebView2.DOMContentLoaded += (s, e) =>
            {
                Debug.WriteLine($"📄 DOM 内容已加载: {e.Uri}");
            };
            
            _webView2.CoreWebView2.ProcessFailed += (s, e) =>
            {
                Debug.WriteLine($"💥 WebView2 进程失败: {e.Reason}");
                OnNavigationFailed($"WebView2 进程失败: {e.Reason}");
            };
            
            Debug.WriteLine("✅ WebView2 事件订阅完成");
            
            _isInitialized = true;
            
            // 如果有初始 URL，导航到它
            if (!string.IsNullOrEmpty(Source))
            {
                Debug.WriteLine($"🎯 准备导航到初始 URL: {Source}");
                await NavigateAsync(Source);
            }
            else
            {
                Debug.WriteLine("⚠️ 没有初始 URL，等待设置 Source 属性");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ WebView2 初始化失败: {ex.GetType().Name}");
            Debug.WriteLine($"   错误消息: {ex.Message}");
            Debug.WriteLine($"   堆栈跟踪: {ex.StackTrace}");
            OnNavigationFailed($"WebView2 初始化错误: {ex.Message}");
        }
    }

    public async Task NavigateAsync(string url)
    {
        if (_webView2?.CoreWebView2 == null)
        {
            Debug.WriteLine($"⚠️ 无法导航，CoreWebView2 未初始化: {url}");
            return;
        }
        
        if (string.IsNullOrEmpty(url))
        {
            Debug.WriteLine("⚠️ URL 为空，跳过导航");
            return;
        }
        
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            Debug.WriteLine($"⚠️ URL 格式不正确: {url}");
            OnNavigationFailed($"URL 格式不正确: {url}");
            return;
        }
        
        try
        {
            Debug.WriteLine($"🚀 开始导航到: {url}");
            _webView2.CoreWebView2.Navigate(url);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ 导航异常: {ex.Message}");
            OnNavigationFailed($"导航异常: {ex.Message}");
        }
    }
#else
    public async Task NavigateAsync(string url)
    {
        await Task.CompletedTask;
        Debug.WriteLine("⚠️ NavigateAsync 调用被忽略（非 Windows 平台）");
    }
#endif

    public void Refresh()
    {
#if WINDOWS
        _webView2?.CoreWebView2?.Reload();
        Debug.WriteLine("🔄 刷新页面");
#endif
    }

    public void GoBack()
    {
#if WINDOWS
        if (_webView2?.CoreWebView2?.CanGoBack == true)
        {
            _webView2.CoreWebView2.GoBack();
            Debug.WriteLine("⬅️ 后退");
        }
#endif
    }

    public void GoForward()
    {
#if WINDOWS
        if (_webView2?.CoreWebView2?.CanGoForward == true)
        {
            _webView2.CoreWebView2.GoForward();
            Debug.WriteLine("➡️ 前进");
        }
#endif
    }

#if WINDOWS
    public bool CanGoBack => _webView2?.CoreWebView2?.CanGoBack ?? false;
    public bool CanGoForward => _webView2?.CoreWebView2?.CanGoForward ?? false;
#else
    public bool CanGoBack => false;
    public bool CanGoForward => false;
#endif

    // 导航事件
    public event EventHandler<string>? NavigationStarted;
    public event EventHandler<string>? NavigationCompleted;
    public event EventHandler<string>? NavigationFailed;

    protected virtual void OnNavigationStarted(string url) => NavigationStarted?.Invoke(this, url);
    protected virtual void OnNavigationCompleted(string url) => NavigationCompleted?.Invoke(this, url);
    protected virtual void OnNavigationFailed(string error) => NavigationFailed?.Invoke(this, error);
}


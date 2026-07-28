/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WebViewEditBridge.cs
    文件功能描述：WebViewEditBridge.cs 相关实现
    
    
    创建标识：Senparc - 20260726
    
    修改标识：Senparc - 20260729
    修改描述：v0.3.3 修复 macOS WebView 编辑桥接并清理应用包构建残留

----------------------------------------------------------------*/

using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.WebView.MacCatalyst.Core;
using AppKit;
using ObjCRuntime;
using WebView = AvaloniaWebView.WebView;

namespace NcfDesktopApp.GUI.Services;

/// <summary>
/// 为内嵌 WebView（尤其是 macOS WKWebView）补齐 Cut/Copy/Paste/SelectAll。
/// macOS 不会像桌面浏览器那样自动把 ⌘C/⌘V 交给 WKWebView，需要应用侧显式桥接。
/// </summary>
internal static class WebViewEditBridge
{
    /// <summary>
    /// WebView.Avalonia.MacCatalyst 11.0.0.1 的 ExecuteScriptAsync 会在原生完成回调中
    /// 对空 result 调用 ToString；该异常无法由调用方捕获并会终止整个进程。
    /// macOS 改用 WKWebView 原生 responder action，只禁用这条不安全的 JavaScript 路径。
    /// </summary>
    public static bool IsScriptBridgeSupported => IsScriptBridgeSupportedForPlatform(OperatingSystem.IsMacOS());

    internal static bool IsScriptBridgeSupportedForPlatform(bool isMacOS) => !isMacOS;

    /// <summary>
    /// 注入到页面的键盘补丁：仅处理修饰键快捷键，不拦截普通打字。
    /// Paste 仍由原生侧完成（页面 JS 通常无权直接读系统剪贴板）。
    /// </summary>
    public const string KeyboardPatchScript = """
(function () {
  if (window.__ncfEditBridgeInstalled) { return 'already'; }
  window.__ncfEditBridgeInstalled = true;
  document.addEventListener('keydown', function (e) {
    var meta = e.metaKey || e.ctrlKey;
    if (!meta || e.altKey) { return; }
    var key = (e.key || '').toLowerCase();
    if (key === 'c') {
      try { document.execCommand('copy'); } catch (_) {}
      return;
    }
    if (key === 'x') {
      try { document.execCommand('cut'); } catch (_) {}
      return;
    }
    if (key === 'a') {
      try {
        if (document.execCommand('selectAll')) {
          e.preventDefault();
        }
      } catch (_) {}
    }
  }, true);
  return 'ok';
})();
""";

    public static async Task<bool> TrySelectAllAsync(WebView? webView)
    {
        if (webView is null)
        {
            return false;
        }

        if (OperatingSystem.IsMacOS())
        {
            return TryExecuteNativeMacEditCommand(webView, EditCommand.SelectAll);
        }

        try
        {
            var result = await webView.ExecuteScriptAsync("""
(function () {
  try { return document.execCommand('selectAll') ? '1' : '0'; }
  catch (e) { return '0'; }
})();
""").ConfigureAwait(true);
            Debug.WriteLine($"[WebViewEditBridge] SelectAll => {result}");
            return IsTruthy(result);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewEditBridge] SelectAll 失败: {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> TryCopyAsync(WebView? webView, IClipboard? clipboard)
    {
        if (webView is null)
        {
            return false;
        }

        if (OperatingSystem.IsMacOS())
        {
            return TryExecuteNativeMacEditCommand(webView, EditCommand.Copy);
        }

        try
        {
            // 先让页面执行 copy（可处理富文本/页面自身逻辑），再把选区文本写入系统剪贴板。
            await webView.ExecuteScriptAsync("try{document.execCommand('copy');}catch(e){}").ConfigureAwait(true);
            var selected = await GetSelectedTextAsync(webView).ConfigureAwait(true);
            if (!string.IsNullOrEmpty(selected) && clipboard is not null)
            {
                await clipboard.SetTextAsync(selected).ConfigureAwait(true);
                Debug.WriteLine($"[WebViewEditBridge] Copy 已写入剪贴板，长度={selected.Length}");
                return true;
            }

            Debug.WriteLine("[WebViewEditBridge] Copy：未获取到选区文本");
            return !string.IsNullOrEmpty(selected);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewEditBridge] Copy 失败: {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> TryCutAsync(WebView? webView, IClipboard? clipboard)
    {
        if (webView is null)
        {
            return false;
        }

        if (OperatingSystem.IsMacOS())
        {
            return TryExecuteNativeMacEditCommand(webView, EditCommand.Cut);
        }

        try
        {
            var selected = await GetSelectedTextAsync(webView).ConfigureAwait(true);
            await webView.ExecuteScriptAsync("try{document.execCommand('cut');}catch(e){}").ConfigureAwait(true);

            if (!string.IsNullOrEmpty(selected) && clipboard is not null)
            {
                await clipboard.SetTextAsync(selected).ConfigureAwait(true);
            }

            // cut 在部分站点会失败，失败时退化为“复制 + 删除选区”。
            if (string.IsNullOrEmpty(selected))
            {
                return false;
            }

            await webView.ExecuteScriptAsync("""
(function () {
  var el = document.activeElement;
  if (!el) { return; }
  if (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA') {
    var start = el.selectionStart || 0;
    var end = el.selectionEnd || 0;
    if (start === end) { return; }
    var value = el.value || '';
    el.value = value.slice(0, start) + value.slice(end);
    el.selectionStart = el.selectionEnd = start;
    el.dispatchEvent(new Event('input', { bubbles: true }));
    el.dispatchEvent(new Event('change', { bubbles: true }));
    return;
  }
  if (el.isContentEditable) {
    try { document.execCommand('delete'); } catch (_) {}
  }
})();
""").ConfigureAwait(true);

            Debug.WriteLine($"[WebViewEditBridge] Cut 完成，长度={selected.Length}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewEditBridge] Cut 失败: {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> TryPasteAsync(WebView? webView, IClipboard? clipboard)
    {
        if (webView is null)
        {
            return false;
        }

        if (OperatingSystem.IsMacOS())
        {
            return TryExecuteNativeMacEditCommand(webView, EditCommand.Paste);
        }

        if (clipboard is null)
        {
            return false;
        }

        try
        {
            var text = await clipboard.GetTextAsync().ConfigureAwait(true);
            if (text is null)
            {
                Debug.WriteLine("[WebViewEditBridge] Paste：剪贴板无文本");
                return false;
            }

            // 用 JSON 编码避免引号/换行破坏脚本；页面侧 JSON.parse 还原。
            var payload = JsonSerializer.Serialize(text);
            var script = $$"""
(function () {
  var text = JSON.parse({{payload}});
  var el = document.activeElement;
  if (!el) { return '0'; }

  if (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA') {
    if (el.disabled || el.readOnly) { return '0'; }
    var start = el.selectionStart || 0;
    var end = el.selectionEnd || 0;
    var value = el.value || '';
    el.value = value.slice(0, start) + text + value.slice(end);
    var caret = start + text.length;
    el.selectionStart = el.selectionEnd = caret;
    el.dispatchEvent(new Event('input', { bubbles: true }));
    el.dispatchEvent(new Event('change', { bubbles: true }));
    return '1';
  }

  if (el.isContentEditable) {
    try {
      if (document.execCommand('insertText', false, text)) { return '1'; }
    } catch (_) {}
    try {
      var sel = window.getSelection();
      if (sel && sel.rangeCount > 0) {
        var range = sel.getRangeAt(0);
        range.deleteContents();
        range.insertNode(document.createTextNode(text));
        range.collapse(false);
        sel.removeAllRanges();
        sel.addRange(range);
        return '1';
      }
    } catch (_) {}
  }

  try { return document.execCommand('paste') ? '1' : '0'; }
  catch (_) { return '0'; }
})();
""";

            var result = await webView.ExecuteScriptAsync(script).ConfigureAwait(true);
            Debug.WriteLine($"[WebViewEditBridge] Paste => {result}, 长度={text.Length}");
            return IsTruthy(result);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewEditBridge] Paste 失败: {ex.Message}");
            return false;
        }
    }

    public static async Task EnsureKeyboardPatchAsync(WebView? webView)
    {
        if (webView is null || !IsScriptBridgeSupported)
        {
            return;
        }

        try
        {
            var result = await webView.ExecuteScriptAsync(KeyboardPatchScript).ConfigureAwait(true);
            Debug.WriteLine($"[WebViewEditBridge] KeyboardPatch => {result}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewEditBridge] KeyboardPatch 失败: {ex.Message}");
        }
    }

    public static IClipboard? GetClipboard(Visual? visual)
    {
        var topLevel = visual is null ? null : TopLevel.GetTopLevel(visual);
        if (topLevel?.Clipboard is { } clipboard)
        {
            return clipboard;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.Clipboard;
        }

        return null;
    }

    public static bool IsEditableAvaloniaFocus(object? focused)
    {
        return focused is TextBox
            or ComboBox
            or AutoCompleteBox;
    }

    public static bool MatchesEditGesture(KeyEventArgs e, out EditCommand command)
    {
        command = EditCommand.None;
        var hotkeys = Application.Current?.PlatformSettings?.HotkeyConfiguration;
        if (hotkeys is not null)
        {
            if (MatchesAny(hotkeys.Copy, e))
            {
                command = EditCommand.Copy;
                return true;
            }

            if (MatchesAny(hotkeys.Cut, e))
            {
                command = EditCommand.Cut;
                return true;
            }

            if (MatchesAny(hotkeys.Paste, e))
            {
                command = EditCommand.Paste;
                return true;
            }

            if (MatchesAny(hotkeys.SelectAll, e))
            {
                command = EditCommand.SelectAll;
                return true;
            }
        }

        // 兜底：避免 PlatformSettings 不可用时快捷键完全失效。
        var modifier = e.KeyModifiers;
        var hasPrimary = OperatingSystem.IsMacOS()
            ? modifier.HasFlag(KeyModifiers.Meta)
            : modifier.HasFlag(KeyModifiers.Control);
        if (!hasPrimary || modifier.HasFlag(KeyModifiers.Alt))
        {
            return false;
        }

        command = e.Key switch
        {
            Key.C => EditCommand.Copy,
            Key.X => EditCommand.Cut,
            Key.V => EditCommand.Paste,
            Key.A => EditCommand.SelectAll,
            _ => EditCommand.None
        };
        return command != EditCommand.None;
    }

    private static bool MatchesAny(System.Collections.Generic.IReadOnlyList<KeyGesture>? gestures, KeyEventArgs e)
    {
        if (gestures is null)
        {
            return false;
        }

        for (var i = 0; i < gestures.Count; i++)
        {
            if (gestures[i].Matches(e))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 让 AppKit 把标准编辑命令发送给当前 first responder。正文和剪贴板都留在
    /// WKWebView/AppKit 边界内，无需执行页面 JavaScript，也不需要将剪贴板内容注入脚本。
    /// </summary>
    private static bool TryExecuteNativeMacEditCommand(WebView webView, EditCommand command)
    {
        try
        {
            if (webView.PlatformWebView?.PlatformViewContext is not MacCatalystWebViewCore nativeCore ||
                nativeCore.WebView is null ||
                GetNativeMacSelector(command) is not { } selectorName)
            {
                return false;
            }

            var application = NSApplication.SharedApplication;
            var selector = new Selector(selectorName);
            var target = application.TargetForAction(selector);
            var handled = target is not null && application.SendAction(selector, target, nativeCore.WebView);
            Debug.WriteLine($"[WebViewEditBridge] Native macOS {command} => {handled}");
            return handled;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WebViewEditBridge] Native macOS {command} 失败: {ex.Message}");
            return false;
        }
    }

    internal static string? GetNativeMacSelector(EditCommand command) => command switch
    {
        EditCommand.Cut => "cut:",
        EditCommand.Copy => "copy:",
        EditCommand.Paste => "paste:",
        EditCommand.SelectAll => "selectAll:",
        _ => null
    };

    private static async Task<string> GetSelectedTextAsync(WebView webView)
    {
        var result = await webView.ExecuteScriptAsync("""
(function () {
  try {
    var el = document.activeElement;
    if (el && (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA')) {
      var start = el.selectionStart || 0;
      var end = el.selectionEnd || 0;
      return (el.value || '').substring(start, end);
    }
    var sel = window.getSelection();
    return sel ? (sel.toString() || '') : '';
  } catch (e) {
    return '';
  }
})();
""").ConfigureAwait(true);

        return UnwrapScriptString(result);
    }

    private static string UnwrapScriptString(string? result)
    {
        if (string.IsNullOrEmpty(result))
        {
            return string.Empty;
        }

        // WKWebView / WebView2 可能返回带引号的 JSON 字符串。
        try
        {
            var parsed = JsonSerializer.Deserialize<string>(result);
            if (parsed is not null)
            {
                return parsed;
            }
        }
        catch (JsonException)
        {
            // ignore
        }

        return result.Trim().Trim('"');
    }

    private static bool IsTruthy(string? result)
    {
        if (string.IsNullOrWhiteSpace(result))
        {
            return false;
        }

        var value = UnwrapScriptString(result).Trim().Trim('"').ToLowerInvariant();
        return value is "1" or "true" or "ok";
    }

    public enum EditCommand
    {
        None,
        Cut,
        Copy,
        Paste,
        SelectAll
    }
}

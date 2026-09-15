/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NeuCharWorkflowChatPage.cs
    文件功能描述：Chat 触发器的自包含聊天页面模板（无外部依赖）


    创建标识：Senparc - 20260909
    创建描述：v0.4.0 新增 Chat 触发器

    修改标识：Senparc - 20260915
    修改描述：v0.4.0 增强 Chat 触发器消息持久化与恢复能力

----------------------------------------------------------------*/

namespace Senparc.Xncf.NeuCharWorkflow.OHS.Local.Controllers;

/// <summary>
/// Chat 聊天页面模板。页面内联全部 CSS 与 JS，不依赖任何外部资源；
/// 模板中的 __WORKFLOW_ID__ 占位符由控制器在渲染时替换为实际工作流 ID。
/// </summary>
public static class NeuCharWorkflowChatPage
{
    public const string Template = """
<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<meta name="robots" content="noindex" />
<title>NeuChar Workflow 聊天</title>
<style>
  :root {
    --brand: #4f46e5;
    --brand-2: #7c3aed;
    --bg: #eef1f6;
    --card: #ffffff;
    --text: #1f2430;
    --muted: #6b7280;
    --line: #e5e8ef;
  }
  * { box-sizing: border-box; }
  html, body { height: 100%; }
  body {
    margin: 0;
    background: var(--bg);
    color: var(--text);
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", "PingFang SC", "Hiragino Sans GB", "Microsoft YaHei", sans-serif;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 16px;
  }
  .chat-card {
    width: 100%;
    max-width: 860px;
    height: calc(100vh - 32px);
    max-height: 880px;
    background: var(--card);
    border-radius: 16px;
    box-shadow: 0 18px 50px rgba(31, 41, 55, 0.18);
    display: flex;
    flex-direction: column;
    overflow: hidden;
  }
  .chat-header {
    background: linear-gradient(135deg, var(--brand), var(--brand-2));
    color: #fff;
    padding: 14px 18px;
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
  }
  .chat-header .logo {
    width: 38px; height: 38px; border-radius: 10px;
    background: rgba(255,255,255,0.18);
    display: flex; align-items: center; justify-content: center;
    font-size: 20px;
    flex: none;
  }
  .chat-header .titles { flex: 1 1 220px; min-width: 0; }
  .chat-header h1 {
    margin: 0; font-size: 17px; font-weight: 600;
    white-space: nowrap; overflow: hidden; text-overflow: ellipsis;
  }
  .chat-header .subtitle { margin: 2px 0 0; font-size: 12px; opacity: 0.85; }
  .badge {
    font-size: 12px; padding: 4px 10px; border-radius: 999px;
    background: rgba(255,255,255,0.22); white-space: nowrap; flex: none;
  }
  .reset-btn {
    border: 1px solid rgba(255,255,255,0.55);
    background: transparent; color: #fff;
    font-size: 12px; padding: 6px 12px; border-radius: 999px; cursor: pointer;
    white-space: nowrap; flex: none;
  }
  .reset-btn:hover:not(:disabled) { background: rgba(255,255,255,0.16); }
  .reset-btn:disabled { opacity: 0.5; cursor: not-allowed; }
  .messages {
    flex: 1; overflow-y: auto; padding: 20px;
    display: flex; flex-direction: column; gap: 14px;
    background: radial-gradient(circle at 18% 8%, rgba(79,70,229,0.06), transparent 42%), #fafbfd;
  }
  .system-line {
    align-self: center; font-size: 12px; color: var(--muted);
    background: #eef0f5; padding: 5px 12px; border-radius: 999px;
    max-width: 90%; text-align: center; word-break: break-word;
  }
  .error-banner {
    align-self: center; max-width: 90%;
    background: #fef2f2; color: #b91c1c; border: 1px solid #fecaca;
    padding: 10px 14px; border-radius: 10px; font-size: 13px; text-align: center;
    word-break: break-word;
  }
  .row { display: flex; gap: 10px; max-width: 85%; }
  .row.user { align-self: flex-end; flex-direction: row-reverse; }
  .row.assistant, .row.error { align-self: flex-start; }
  .avatar {
    width: 32px; height: 32px; border-radius: 50%; flex: none;
    display: flex; align-items: center; justify-content: center;
    font-size: 14px; background: #e3e7f3;
  }
  .row.user .avatar { background: var(--brand); color: #fff; }
  .bubble {
    padding: 10px 14px; border-radius: 14px;
    font-size: 14px; line-height: 1.65; word-break: break-word;
    min-width: 0;
  }
  .row.user .bubble {
    background: var(--brand); color: #fff;
    border-bottom-right-radius: 4px; white-space: pre-wrap;
  }
  .row.assistant .bubble {
    background: #f1f3f7; color: var(--text);
    border-bottom-left-radius: 4px;
  }
  .row.error .bubble {
    background: #fef2f2; color: #b91c1c; border: 1px solid #fecaca;
    border-bottom-left-radius: 4px; white-space: pre-wrap;
  }
  .bubble pre {
    background: rgba(17,24,39,0.06); padding: 10px; border-radius: 8px;
    overflow-x: auto; font-size: 13px; margin: 8px 0;
  }
  .row.user .bubble pre { background: rgba(0,0,0,0.22); }
  .bubble code { font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace; }
  .bubble a { color: var(--brand); word-break: break-all; }
  .row.user .bubble a { color: #fff; text-decoration: underline; }
  .typing-dots { display: inline-flex; gap: 4px; align-items: center; }
  .typing-dots span {
    width: 6px; height: 6px; border-radius: 50%; background: #9aa3b5;
    animation: chat-bounce 1.2s infinite ease-in-out;
  }
  .typing-dots span:nth-child(2) { animation-delay: 0.15s; }
  .typing-dots span:nth-child(3) { animation-delay: 0.3s; }
  @keyframes chat-bounce {
    0%, 80%, 100% { transform: translateY(0); opacity: 0.5; }
    40% { transform: translateY(-4px); opacity: 1; }
  }
  .typing-status { font-size: 12px; color: var(--muted); margin-top: 6px; }
  .composer {
    border-top: 1px solid var(--line); padding: 12px 16px 12px; background: #fff;
  }
  .composer-inner { display: flex; gap: 10px; align-items: flex-end; }
  textarea {
    flex: 1; resize: none; border: 1px solid #d6dae4; border-radius: 12px;
    padding: 10px 12px; font-size: 14px; font-family: inherit; line-height: 1.5;
    max-height: 140px; outline: none; background: #fff;
  }
  textarea:focus { border-color: var(--brand); box-shadow: 0 0 0 3px rgba(79,70,229,0.12); }
  .send-btn {
    background: linear-gradient(135deg, var(--brand), var(--brand-2));
    color: #fff; border: none; border-radius: 12px; padding: 10px 20px;
    font-size: 14px; cursor: pointer; white-space: nowrap;
  }
  .send-btn:disabled { opacity: 0.55; cursor: not-allowed; }
  .hint { margin: 8px 2px 0; font-size: 12px; color: var(--muted); }
  @media (max-width: 640px) {
    body { padding: 0; }
    .chat-card { height: 100vh; max-height: none; border-radius: 0; }
    .row { max-width: 92%; }
  }
</style>
</head>
<body>
<div class="chat-card">
  <header class="chat-header">
    <div class="logo">&#129302;</div>
    <div class="titles">
      <h1 id="chat-title">NeuChar Workflow 聊天</h1>
      <p class="subtitle" id="chat-subtitle">加载中…</p>
    </div>
    <span class="badge" id="chat-badge">…</span>
    <button class="reset-btn" id="reset-btn" type="button" disabled>新对话</button>
  </header>
  <main class="messages" id="messages">
    <div class="system-line">正在加载会话…</div>
  </main>
  <footer class="composer">
    <div class="composer-inner">
      <textarea id="input" rows="1" placeholder="输入消息，按 Enter 发送" disabled></textarea>
      <button class="send-btn" id="send-btn" type="button" disabled>发送</button>
    </div>
    <p class="hint">Enter 发送 · Shift + Enter 换行 · 你的消息会作为输入启动工作流</p>
  </footer>
</div>
<script>
(function () {
  'use strict';
  var apiBase = '/api/Senparc.Xncf.NeuCharWorkflow/neuchar-workflow/chat';
  var workflowId = __WORKFLOW_ID__;
  var els = {
    title: document.getElementById('chat-title'),
    subtitle: document.getElementById('chat-subtitle'),
    badge: document.getElementById('chat-badge'),
    reset: document.getElementById('reset-btn'),
    messages: document.getElementById('messages'),
    input: document.getElementById('input'),
    send: document.getElementById('send-btn')
  };
  var state = {
    ready: false,
    sending: false,
    afterSequence: 0,
    timer: null,
    greeting: '',
    isGuest: false
  };

  function escapeHtml(value) {
    return String(value)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  function renderMarkdownLite(text) {
    var blocks = [];
    var html = String(text).replace(/```([\s\S]*?)```/g, function (match, code) {
      blocks.push('<pre><code>' + escapeHtml(code.replace(/^\n/, '')) + '</code></pre>');
      return '\u0000BLOCK' + (blocks.length - 1) + '\u0000';
    });
    html = escapeHtml(html);
    html = html.replace(/`([^`\n]+)`/g, '<code>$1</code>');
    html = html.replace(/\*\*([^*\n]+)\*\*/g, '<strong>$1</strong>');
    html = html.replace(/\[([^\]]+)\]\((https?:\/\/[^\s)]+)\)/g,
      '<a href="$2" target="_blank" rel="noopener noreferrer">$1</a>');
    html = html.replace(/\n/g, '<br>');
    html = html.replace(/\u0000BLOCK(\d+)\u0000/g, function (match, index) {
      return blocks[Number(index)];
    });
    return html;
  }

  function api(path, options) {
    return fetch(apiBase + path, options).then(function (response) {
      return response.text().then(function (body) {
        var data = null;
        try { data = body ? JSON.parse(body) : null; } catch (e) { data = null; }
        if (!response.ok) {
          throw new Error((data && data.errorMessage) || ('请求失败（' + response.status + '）'));
        }
        return data || {};
      });
    });
  }

  function scrollBottom() {
    els.messages.scrollTop = els.messages.scrollHeight;
  }

  function clearMessages() {
    els.messages.innerHTML = '';
  }

  function addSystemLine(text) {
    var node = document.createElement('div');
    node.className = 'system-line';
    node.textContent = text;
    els.messages.appendChild(node);
    scrollBottom();
  }

  function addErrorBanner(text) {
    var node = document.createElement('div');
    node.className = 'error-banner';
    node.textContent = text;
    els.messages.appendChild(node);
    scrollBottom();
  }

  function addBubble(role, text) {
    var row = document.createElement('div');
    row.className = 'row ' + role;
    var avatar = document.createElement('div');
    avatar.className = 'avatar';
    avatar.textContent = role === 'user' ? '我' : (role === 'error' ? '!' : '\u{1F916}');
    var bubble = document.createElement('div');
    bubble.className = 'bubble';
    if (role === 'assistant') {
      bubble.innerHTML = renderMarkdownLite(text);
    } else {
      bubble.textContent = text;
    }
    row.appendChild(avatar);
    row.appendChild(bubble);
    els.messages.appendChild(row);
    scrollBottom();
  }

  function setTyping(statusText) {
    clearTyping();
    var row = document.createElement('div');
    row.className = 'row assistant';
    row.id = 'typing-row';
    var avatar = document.createElement('div');
    avatar.className = 'avatar';
    avatar.textContent = '\u{1F916}';
    var bubble = document.createElement('div');
    bubble.className = 'bubble';
    var dots = document.createElement('span');
    dots.className = 'typing-dots';
    dots.innerHTML = '<span></span><span></span><span></span>';
    var status = document.createElement('div');
    status.className = 'typing-status';
    status.textContent = statusText || '正在处理…';
    bubble.appendChild(dots);
    bubble.appendChild(status);
    row.appendChild(avatar);
    row.appendChild(bubble);
    els.messages.appendChild(row);
    scrollBottom();
  }

  function updateTyping(statusText) {
    var row = document.getElementById('typing-row');
    if (!row) return;
    var status = row.querySelector('.typing-status');
    if (status && statusText) status.textContent = statusText;
    scrollBottom();
  }

  function clearTyping() {
    var row = document.getElementById('typing-row');
    if (row) row.remove();
  }

  function setSending(value) {
    state.sending = value;
    els.send.disabled = value || !state.ready;
    els.reset.disabled = value || !state.ready;
    els.input.disabled = !state.ready;
    els.input.placeholder = value ? '正在处理中，请稍候…' : '输入消息，按 Enter 发送';
  }

  function stopPolling() {
    if (state.timer) {
      clearTimeout(state.timer);
      state.timer = null;
    }
  }

  function pollRun(runId) {
    stopPolling();
    api('/' + workflowId + '/runs/' + runId + '?afterSequence=' + state.afterSequence)
      .then(function (data) {
        if (data.running) {
          if (data.lastSequence) state.afterSequence = data.lastSequence;
          updateTyping(data.lastNodeMessage || '正在处理…');
          state.timer = setTimeout(function () { pollRun(runId); }, 1200);
          return;
        }
        stopPolling();
        clearTyping();
        state.afterSequence = 0;
        setSending(false);
        if (data.runError) {
          addBubble('error', data.runError || '工作流执行失败。');
        } else {
          addBubble('assistant', data.finalOutput || '工作流已完成。');
        }
      })
      .catch(function () {
        state.timer = setTimeout(function () { pollRun(runId); }, 2500);
      });
  }

  function applyBootstrap(data) {
    state.ready = true;
    state.isGuest = !!data.isGuest;
    state.greeting = data.greeting || '';
    document.title = data.title || 'NeuChar Workflow 聊天';
    els.title.textContent = data.title || 'NeuChar Workflow 聊天';
    els.subtitle.textContent = '工作流 #' + (data.workflowId || workflowId) + ' · Chat 触发';
    els.badge.textContent = state.isGuest ? '访客模式' : '已登录';
    setSending(false);
    clearMessages();
    if (data.messages && data.messages.length > 0) {
      data.messages.forEach(function (message) {
        var role = message.role === 'user' ? 'user' : (message.role === 'error' ? 'error' : 'assistant');
        addBubble(role, message.content);
      });
    } else if (state.greeting) {
      addBubble('assistant', state.greeting);
    }
    if (data.hasPendingRun && data.pendingRunId) {
      setSending(true);
      state.afterSequence = 0;
      setTyping('上一条消息还在处理中…');
      pollRun(data.pendingRunId);
    }
  }

  function sendMessage() {
    if (!state.ready || state.sending) return;
    var text = els.input.value.trim();
    if (!text) return;
    addBubble('user', text);
    els.input.value = '';
    autoresize();
    setSending(true);
    setTyping('正在启动工作流…');
    api('/' + workflowId + '/messages', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ message: text })
    })
      .then(function (data) {
        state.afterSequence = 0;
        pollRun(data.runId);
      })
      .catch(function (error) {
        clearTyping();
        setSending(false);
        addBubble('error', error.message || '发送失败。');
      });
  }

  function autoresize() {
    els.input.style.height = 'auto';
    els.input.style.height = Math.min(els.input.scrollHeight, 140) + 'px';
  }

  els.send.addEventListener('click', function () { sendMessage(); });
  els.input.addEventListener('keydown', function (event) {
    if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
      event.preventDefault();
      sendMessage();
    }
  });
  els.input.addEventListener('input', autoresize);
  els.reset.addEventListener('click', function () {
    if (!state.ready || state.sending) return;
    if (!window.confirm('确定要清空当前会话吗？正在进行的工作流运行不受影响。')) return;
    api('/' + workflowId + '/reset', { method: 'POST' })
      .then(function () {
        clearMessages();
        if (state.greeting) addBubble('assistant', state.greeting);
      })
      .catch(function (error) {
        addErrorBanner(error.message || '重置失败。');
      });
  });

  api('/' + workflowId + '/bootstrap')
    .then(applyBootstrap)
    .catch(function (error) {
      clearMessages();
      addErrorBanner(error.message || '无法加载聊天会话。');
      els.subtitle.textContent = '无法加载会话';
      els.badge.textContent = '—';
      els.reset.disabled = true;
      els.send.disabled = true;
      els.input.disabled = true;
    });
})();
</script>
</body>
</html>
""";
}

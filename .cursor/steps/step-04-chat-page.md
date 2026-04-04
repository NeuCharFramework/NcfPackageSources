# Step 04: 对话任务页面 - 创建专门的 AI 对话界面

## 📋 任务概述
创建独立的对话任务页面，包含左侧对话历史记录列表和右侧 AI 对话窗口，设计符合系统风格且现代化。

## 🎯 目标
- ✅ 创建 Chat.cshtml 页面（左右分栏布局）
- ✅ 左侧：会话历史记录列表
- ✅ 右侧：当前会话的对话窗口
- ✅ 实现消息发送和接收
- ✅ 支持消息反馈（点赞/点踩）
- ✅ 实时滚动到最新消息
- ✅ 符合系统风格，重用 Element UI 组件

## 📂 涉及文件

### 新建文件
1. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/AdminChat/Chat.cshtml`
2. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/AdminChat/Chat.cshtml.cs`
3. `tools/NcfSimulatedSite/Senparc.Areas.Admin/wwwroot/js/Admin/Pages/AdminChat/Chat.js`
4. `tools/NcfSimulatedSite/Senparc.Areas.Admin/wwwroot/css/Admin/Pages/AdminChat/Chat.css`

## 🔧 实现步骤

### 1. 创建 Chat.cshtml 页面

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/AdminChat/Chat.cshtml`

**完整代码示例**:

```cshtml
@page
@model Senparc.Areas.Admin.Pages.AdminChat.ChatModel
@{
    ViewData["Title"] = "AI 智能对话";
    Layout = "_Layout_Vue";
}

@section breadcrumbs {
    <el-breadcrumb-item>AI 智能对话</el-breadcrumb-item>
}

@section style {
    <link href="~/css/Admin/Pages/AdminChat/Chat.css" rel="stylesheet" />
}

<div id="chat-app" v-cloak>
    <el-container class="chat-container">
        <!-- 左侧：会话历史列表 -->
        <el-aside width="300px" class="chat-sidebar">
            <div class="sidebar-header">
                <h3><i class="fa fa-history"></i> 对话历史</h3>
                <el-button 
                    type="primary" 
                    size="small" 
                    icon="el-icon-plus"
                    @@click="createNewSession">
                    新对话
                </el-button>
            </div>

            <el-scrollbar class="sidebar-scrollbar">
                <div class="session-list">
                    <div 
                        v-for="session in sessionList"
                        :key="session.id"
                        class="session-item"
                        :class="{ 'active': currentSessionId === session.id }"
                        @@click="switchSession(session.id)">
                        <div class="session-title">
                            <i class="fa fa-comment-o"></i>
                            {{ session.title }}
                        </div>
                        <div class="session-preview">{{ session.lastMessagePreview }}</div>
                        <div class="session-meta">
                            <span class="session-time">{{ formatTime(session.lastMessageTime) }}</span>
                            <span v-if="session.modules && session.modules.length > 0" class="session-modules">
                                <i class="fa fa-cube"></i> {{ session.modules.length }}
                            </span>
                        </div>
                        <div class="session-actions">
                            <el-button 
                                type="text" 
                                icon="el-icon-delete" 
                                size="mini"
                                @@click.stop="deleteSession(session.id)">
                            </el-button>
                        </div>
                    </div>

                    <!-- 加载更多 -->
                    <div v-if="hasMoreSessions" class="load-more">
                        <el-button 
                            type="text" 
                            size="small"
                            @@click="loadMoreSessions"
                            :loading="loadingMore">
                            加载更多
                        </el-button>
                    </div>
                </div>
            </el-scrollbar>
        </el-aside>

        <!-- 右侧：对话窗口 -->
        <el-main class="chat-main">
            <div class="chat-header">
                <div class="chat-title">
                    <h3>{{ currentSessionTitle }}</h3>
                    <div v-if="currentSessionModules.length > 0" class="header-modules">
                        <el-tag 
                            v-for="module in currentSessionModules"
                            :key="module.xncfModuleUid"
                            size="small"
                            type="info">
                            {{ module.moduleName }}
                        </el-tag>
                    </div>
                </div>
                <div class="chat-actions">
                    <el-button 
                        type="text" 
                        icon="el-icon-refresh"
                        @@click="refreshMessages">
                        刷新
                    </el-button>
                </div>
            </div>

            <!-- 消息列表 -->
            <el-scrollbar ref="messageScrollbar" class="message-scrollbar">
                <div class="message-list" v-loading="loadingMessages">
                    <div 
                        v-for="message in messageList"
                        :key="message.id"
                        class="message-item"
                        :class="`message-${message.roleType === 0 ? 'user' : 'assistant'}`">
                        
                        <div class="message-avatar">
                            <i :class="message.roleType === 0 ? 'fa fa-user' : 'fa fa-robot'"></i>
                        </div>

                        <div class="message-content-wrapper">
                            <div class="message-header">
                                <span class="message-role">
                                    {{ message.roleType === 0 ? '我' : 'AI 助手' }}
                                </span>
                                <span class="message-time">{{ formatTime(message.createdTime) }}</span>
                            </div>
                            <div class="message-content">
                                <div class="message-text" v-html="formatMessageContent(message.content)"></div>
                            </div>
                            
                            <!-- AI 消息的反馈按钮 -->
                            <div v-if="message.roleType === 1" class="message-feedback">
                                <el-button 
                                    type="text" 
                                    size="mini"
                                    :class="{ 'active': message.userFeedback === true }"
                                    @@click="setFeedback(message.id, true)">
                                    <i class="fa fa-thumbs-up"></i>
                                </el-button>
                                <el-button 
                                    type="text" 
                                    size="mini"
                                    :class="{ 'active': message.userFeedback === false }"
                                    @@click="setFeedback(message.id, false)">
                                    <i class="fa fa-thumbs-down"></i>
                                </el-button>
                            </div>
                        </div>
                    </div>

                    <!-- 加载中提示 -->
                    <div v-if="isAiTyping" class="message-item message-assistant">
                        <div class="message-avatar">
                            <i class="fa fa-robot"></i>
                        </div>
                        <div class="message-content-wrapper">
                            <div class="message-content">
                                <div class="typing-indicator">
                                    <span></span>
                                    <span></span>
                                    <span></span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </el-scrollbar>

            <!-- 输入区域 -->
            <div class="chat-input-area">
                <div class="input-toolbar">
                    <span class="char-count">{{ inputMessage.length }} / 2000</span>
                </div>
                <div class="input-wrapper">
                    <el-input
                        v-model="inputMessage"
                        type="textarea"
                        :rows="3"
                        :maxlength="2000"
                        placeholder="输入您的问题... (Ctrl+Enter 发送)"
                        @@keydown.ctrl.enter.native="sendMessage"
                        :disabled="isAiTyping">
                    </el-input>
                    <el-button 
                        type="primary" 
                        icon="el-icon-s-promotion"
                        @@click="sendMessage"
                        :disabled="!inputMessage || inputMessage.trim().length === 0 || isAiTyping"
                        :loading="isAiTyping"
                        class="send-button">
                        发送
                    </el-button>
                </div>
            </div>
        </el-main>
    </el-container>
</div>

@section scripts {
    <script src="~/js/Admin/Pages/AdminChat/Chat.js"></script>
}
```

**关键技术点**：
- 使用 Element UI 的 Container、Aside、Main 组件实现左右分栏
- Scrollbar 组件实现平滑滚动
- v-for 循环渲染消息列表
- v-loading 显示加载状态
- 条件渲染 (v-if) 控制显示逻辑

---

### 2. 创建 Chat.cshtml.cs 后端页面模型

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/AdminChat/Chat.cshtml.cs`

**完整代码示例**:

```csharp
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Domain;
using System;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.Pages.AdminChat
{
    public class ChatModel : BaseAdminPageModel
    {
        public ChatModel(IServiceProvider serviceProvider) 
            : base(serviceProvider)
        {
        }

        /// <summary>
        /// 当前会话ID（从 QueryString 获取）
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// 初始消息（从 QueryString 获取，用于首次进入）
        /// </summary>
        public string InitialMessage { get; set; }

        /// <summary>
        /// 当前用户ID
        /// </summary>
        public int CurrentUserId { get; set; }

        public IActionResult OnGet(int? sessionId, string initialMessage)
        {
            // 获取当前登录用户
            CurrentUserId = GetCurrentUserId();

            if (CurrentUserId <= 0)
            {
                return RedirectToPage("/Login");
            }

            SessionId = sessionId ?? 0;
            InitialMessage = initialMessage;

            return Page();
        }

        /// <summary>
        /// 获取当前登录用户的 ID
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }
    }
}
```

**关键技术点**：
- 继承自 `BaseAdminPageModel`
- 从 QueryString 获取参数
- 验证用户登录状态

---

### 3. 创建 Chat.js 前端交互逻辑

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/wwwroot/js/Admin/Pages/AdminChat/Chat.js`

**完整代码示例**:

```javascript
var chatApp = new Vue({
  el: '#chat-app',
  data() {
    return {
      // 会话列表
      sessionList: [],
      currentSessionId: 0,
      currentSessionTitle: '新对话',
      currentSessionModules: [],
      
      // 消息列表
      messageList: [],
      
      // 输入和状态
      inputMessage: '',
      isAiTyping: false,
      loadingMessages: false,
      loadingMore: false,
      
      // 分页
      pageIndex: 1,
      pageSize: 20,
      hasMoreSessions: false,
      
      // 用户信息
      currentUserId: 0,
      initialMessage: ''
    };
  },
  
  mounted() {
    // 从页面获取初始数据
    const urlParams = new URLSearchParams(window.location.search);
    this.currentSessionId = parseInt(urlParams.get('sessionId')) || 0;
    this.initialMessage = urlParams.get('initialMessage') || '';
    this.currentUserId = this.getCurrentUserId();

    // 加载会话列表
    this.loadSessions();

    // 如果有会话ID，加载消息
    if (this.currentSessionId > 0) {
      this.loadMessages(this.currentSessionId);
      
      // 如果有初始消息，自动发送
      if (this.initialMessage) {
        this.inputMessage = this.initialMessage;
        this.$nextTick(() => {
          this.sendMessage();
        });
      }
    }
  },

  methods: {
    /**
     * 加载会话列表
     */
    async loadSessions() {
      try {
        const response = await service.get(
          `/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetUserSessions`,
          {
            params: {
              userId: this.currentUserId,
              pageIndex: this.pageIndex,
              pageSize: this.pageSize
            }
          }
        );

        if (response.data && response.data.success && response.data.data) {
          const data = response.data.data;
          
          if (this.pageIndex === 1) {
            this.sessionList = data.sessions;
          } else {
            this.sessionList = this.sessionList.concat(data.sessions);
          }
          
          this.hasMoreSessions = this.sessionList.length < data.totalCount;

          // 如果没有当前会话，选择第一个
          if (this.currentSessionId === 0 && this.sessionList.length > 0) {
            this.switchSession(this.sessionList[0].id);
          }
        }
      } catch (error) {
        console.error('加载会话列表失败:', error);
        this.$message.error('加载会话列表失败');
      }
    },

    /**
     * 加载更多会话
     */
    async loadMoreSessions() {
      this.loadingMore = true;
      this.pageIndex++;
      await this.loadSessions();
      this.loadingMore = false;
    },

    /**
     * 加载消息列表
     */
    async loadMessages(sessionId) {
      this.loadingMessages = true;
      
      try {
        const response = await service.get(
          `/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetSessionMessages`,
          {
            params: { sessionId: sessionId }
          }
        );

        if (response.data && response.data.success && response.data.data) {
          this.messageList = response.data.data;
          
          // 滚动到底部
          this.$nextTick(() => {
            this.scrollToBottom();
          });
        }
      } catch (error) {
        console.error('加载消息失败:', error);
        this.$message.error('加载消息失败');
      } finally {
        this.loadingMessages = false;
      }
    },

    /**
     * 发送消息
     */
    async sendMessage() {
      if (!this.inputMessage || this.inputMessage.trim().length === 0) {
        this.$message.warning('请输入消息内容');
        return;
      }

      if (this.currentSessionId === 0) {
        this.$message.warning('请先选择或创建一个会话');
        return;
      }

      const message = this.inputMessage.trim();
      this.inputMessage = ''; // 清空输入框
      this.isAiTyping = true;

      try {
        const response = await service.post(
          '/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.SendMessage',
          {
            sessionId: this.currentSessionId,
            content: message,
            moduleUids: this.currentSessionModules.map(m => m.xncfModuleUid)
          }
        );

        if (response.data && response.data.success && response.data.data) {
          const data = response.data.data;
          
          // 添加用户消息和 AI 回复到列表
          this.messageList.push(data.userMessage);
          this.messageList.push(data.assistantMessage);

          // 滚动到底部
          this.$nextTick(() => {
            this.scrollToBottom();
          });

          // 更新会话列表中的最后消息预览
          this.updateSessionPreview(data.sessionId, data.assistantMessage.content);
        }
      } catch (error) {
        console.error('发送消息失败:', error);
        this.$message.error('发送消息失败：' + (error.response?.data?.message || '未知错误'));
        
        // 恢复输入框内容
        this.inputMessage = message;
      } finally {
        this.isAiTyping = false;
      }
    },

    /**
     * 切换会话
     */
    async switchSession(sessionId) {
      if (this.currentSessionId === sessionId) {
        return;
      }

      this.currentSessionId = sessionId;
      
      // 查找会话信息
      const session = this.sessionList.find(s => s.id === sessionId);
      if (session) {
        this.currentSessionTitle = session.title;
        this.currentSessionModules = session.modules || [];
      }

      // 加载消息
      await this.loadMessages(sessionId);

      // 更新 URL（不刷新页面）
      const newUrl = `/Admin/AdminChat/Chat?sessionId=${sessionId}`;
      window.history.pushState({ sessionId }, '', newUrl);
    },

    /**
     * 创建新会话
     */
    async createNewSession() {
      const title = '新对话 ' + new Date().toLocaleString();
      
      try {
        const response = await service.post(
          '/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.CreateSession',
          {
            userId: this.currentUserId,
            initialMessage: title
          }
        );

        if (response.data && response.data.success && response.data.data) {
          const newSession = response.data.data;
          
          // 添加到会话列表顶部
          this.sessionList.unshift(newSession);
          
          // 切换到新会话
          await this.switchSession(newSession.id);
          
          this.$message.success('已创建新对话');
        }
      } catch (error) {
        console.error('创建会话失败:', error);
        this.$message.error('创建会话失败');
      }
    },

    /**
     * 删除会话
     */
    async deleteSession(sessionId) {
      try {
        await this.$confirm('确定要删除这个对话吗？', '提示', {
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          type: 'warning'
        });

        const response = await service.delete(
          `/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.DeleteSession`,
          {
            params: { sessionId: sessionId }
          }
        );

        if (response.data && response.data.success) {
          // 从列表中移除
          const index = this.sessionList.findIndex(s => s.id === sessionId);
          if (index !== -1) {
            this.sessionList.splice(index, 1);
          }

          // 如果删除的是当前会话，切换到第一个会话
          if (this.currentSessionId === sessionId) {
            if (this.sessionList.length > 0) {
              await this.switchSession(this.sessionList[0].id);
            } else {
              this.currentSessionId = 0;
              this.messageList = [];
            }
          }

          this.$message.success('已删除对话');
        }
      } catch (error) {
        if (error !== 'cancel') {
          console.error('删除会话失败:', error);
          this.$message.error('删除会话失败');
        }
      }
    },

    /**
     * 设置消息反馈
     */
    async setFeedback(messageId, isLike) {
      try {
        const response = await service.post(
          '/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.SetMessageFeedback',
          {
            messageId: messageId,
            isLike: isLike
          }
        );

        if (response.data && response.data.success) {
          // 更新本地消息的反馈状态
          const message = this.messageList.find(m => m.id === messageId);
          if (message) {
            message.userFeedback = isLike;
          }

          this.$message.success(isLike ? '感谢您的反馈' : '我们会改进');
        }
      } catch (error) {
        console.error('设置反馈失败:', error);
      }
    },

    /**
     * 刷新消息列表
     */
    async refreshMessages() {
      if (this.currentSessionId > 0) {
        await this.loadMessages(this.currentSessionId);
        this.$message.success('已刷新');
      }
    },

    /**
     * 滚动到底部
     */
    scrollToBottom() {
      const scrollbar = this.$refs.messageScrollbar;
      if (scrollbar && scrollbar.$refs && scrollbar.$refs.wrap) {
        const wrap = scrollbar.$refs.wrap;
        wrap.scrollTop = wrap.scrollHeight;
      }
    },

    /**
     * 更新会话预览
     */
    updateSessionPreview(sessionId, content) {
      const session = this.sessionList.find(s => s.id === sessionId);
      if (session) {
        session.lastMessagePreview = content.length > 100 ? content.substring(0, 100) + '...' : content;
        session.lastMessageTime = new Date().toISOString();
      }
    },

    /**
     * 格式化时间显示
     */
    formatTime(timeString) {
      if (!timeString) return '';
      
      const date = new Date(timeString);
      const now = new Date();
      const diff = now - date;
      
      // 1分钟内
      if (diff < 60000) {
        return '刚刚';
      }
      // 1小时内
      if (diff < 3600000) {
        return Math.floor(diff / 60000) + ' 分钟前';
      }
      // 今天
      if (date.toDateString() === now.toDateString()) {
        return date.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' });
      }
      // 昨天
      const yesterday = new Date(now);
      yesterday.setDate(yesterday.getDate() - 1);
      if (date.toDateString() === yesterday.toDateString()) {
        return '昨天 ' + date.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' });
      }
      // 更早
      return date.toLocaleDateString('zh-CN') + ' ' + date.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' });
    },

    /**
     * 格式化消息内容（简单格式化）
     */
    formatMessageContent(content) {
      if (!content) return '';
      
      // 简单的格式化：换行、代码块等
      // 使用系统现有功能，不引入新的 Markdown 库
      return content
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/\n/g, '<br/>')
        .replace(/`([^`]+)`/g, '<code>$1</code>')
        .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
        .replace(/\*([^*]+)\*/g, '<em>$1</em>');
    },

    /**
     * 获取当前用户ID
     */
    getCurrentUserId() {
      // 从页面传递的数据或 Store 中获取
      return @Model.CurrentUserId;
    }
  }
});
```

**关键技术点**：
- 使用 async/await 处理异步 API 调用
- 实现消息实时追加和滚动
- 时间格式化显示（刚刚、X分钟前、昨天等）
- 消息内容简单格式化（换行、代码、加粗、斜体）
- XSS 防护（HTML 转义）
- 只使用系统现有的 Vue.js、Element UI 和 axios，不引入新依赖

---

### 4. 创建 Chat.css 样式文件

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/wwwroot/css/Admin/Pages/AdminChat/Chat.css`

**完整代码示例**:

```css
/* 对话容器 */
.chat-container {
    height: calc(100vh - 160px);
    background: #fff;
    border-radius: 8px;
    overflow: hidden;
}

/* 左侧会话列表 */
.chat-sidebar {
    border-right: 1px solid #e4e7ed;
    display: flex;
    flex-direction: column;
    background: #fafafa;
}

.sidebar-header {
    padding: 20px;
    border-bottom: 1px solid #e4e7ed;
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: #fff;
}

    .sidebar-header h3 {
        margin: 0;
        font-size: 16px;
        font-weight: 600;
        color: #333;
    }

        .sidebar-header h3 .fa {
            color: #8c52ff;
            margin-right: 8px;
        }

.sidebar-scrollbar {
    flex: 1;
    height: 0;
}

.session-list {
    padding: 10px;
}

.session-item {
    background: #fff;
    border-radius: 8px;
    padding: 15px;
    margin-bottom: 10px;
    cursor: pointer;
    transition: all 0.3s ease;
    border: 1px solid transparent;
    position: relative;
}

    .session-item:hover {
        border-color: #8c52ff;
        box-shadow: 0 2px 12px rgba(140, 82, 255, 0.1);
    }

    .session-item.active {
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white;
        border-color: #8c52ff;
    }

        .session-item.active .session-title,
        .session-item.active .session-preview,
        .session-item.active .session-time,
        .session-item.active .session-modules {
            color: white;
        }

.session-title {
    font-size: 14px;
    font-weight: 600;
    color: #333;
    margin-bottom: 6px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

    .session-title .fa {
        margin-right: 6px;
    }

.session-preview {
    font-size: 12px;
    color: #999;
    line-height: 1.5;
    max-height: 36px;
    overflow: hidden;
    text-overflow: ellipsis;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    margin-bottom: 8px;
}

.session-meta {
    display: flex;
    justify-content: space-between;
    align-items: center;
    font-size: 11px;
    color: #999;
}

.session-modules .fa {
    margin-right: 3px;
}

.session-actions {
    position: absolute;
    top: 10px;
    right: 10px;
    opacity: 0;
    transition: opacity 0.3s ease;
}

.session-item:hover .session-actions {
    opacity: 1;
}

.load-more {
    text-align: center;
    padding: 10px;
}

/* 右侧对话区域 */
.chat-main {
    display: flex;
    flex-direction: column;
    padding: 0;
    background: #f5f7fa;
}

.chat-header {
    padding: 20px;
    border-bottom: 1px solid #e4e7ed;
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: #fff;
}

.chat-title h3 {
    margin: 0 0 8px 0;
    font-size: 18px;
    font-weight: 600;
    color: #333;
}

.header-modules {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
}

/* 消息列表 */
.message-scrollbar {
    flex: 1;
    height: 0;
    padding: 20px;
}

.message-list {
    min-height: 100%;
}

.message-item {
    display: flex;
    margin-bottom: 24px;
    animation: fadeIn 0.3s ease;
}

@@keyframes fadeIn {
    from {
        opacity: 0;
        transform: translateY(10px);
    }

    to {
        opacity: 1;
        transform: translateY(0);
    }
}

.message-user {
    justify-content: flex-end;
}

.message-assistant {
    justify-content: flex-start;
}

.message-avatar {
    width: 40px;
    height: 40px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 20px;
    flex-shrink: 0;
}

.message-user .message-avatar {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    order: 2;
    margin-left: 12px;
}

.message-assistant .message-avatar {
    background: #f0f2f5;
    color: #8c52ff;
    margin-right: 12px;
}

.message-content-wrapper {
    max-width: 70%;
}

.message-user .message-content-wrapper {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
}

.message-header {
    display: flex;
    align-items: center;
    margin-bottom: 6px;
    gap: 8px;
}

.message-role {
    font-size: 13px;
    font-weight: 600;
    color: #666;
}

.message-time {
    font-size: 11px;
    color: #999;
}

.message-content {
    background: #fff;
    padding: 12px 16px;
    border-radius: 12px;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.message-user .message-content {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    color: white;
    border-radius: 12px 12px 0 12px;
}

.message-assistant .message-content {
    border-radius: 12px 12px 12px 0;
}

.message-text {
    font-size: 14px;
    line-height: 1.6;
    word-break: break-word;
}

    .message-text code {
        background: rgba(0, 0, 0, 0.05);
        padding: 2px 6px;
        border-radius: 3px;
        font-family: 'Courier New', monospace;
    }

.message-user .message-text code {
    background: rgba(255, 255, 255, 0.2);
}

.message-feedback {
    margin-top: 6px;
    display: flex;
    gap: 8px;
}

    .message-feedback .el-button {
        padding: 4px 8px;
        color: #999;
    }

        .message-feedback .el-button:hover {
            color: #8c52ff;
        }

        .message-feedback .el-button.active {
            color: #8c52ff;
        }

/* AI 正在输入 */
.typing-indicator {
    display: flex;
    gap: 4px;
    padding: 8px 0;
}

    .typing-indicator span {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background: #8c52ff;
        animation: typing 1.4s infinite;
    }

        .typing-indicator span:nth-child(2) {
            animation-delay: 0.2s;
        }

        .typing-indicator span:nth-child(3) {
            animation-delay: 0.4s;
        }

@@keyframes typing {
    0%, 60%, 100% {
        transform: translateY(0);
        opacity: 0.7;
    }

    30% {
        transform: translateY(-10px);
        opacity: 1;
    }
}

/* 输入区域 */
.chat-input-area {
    padding: 15px 20px;
    background: #fff;
    border-top: 1px solid #e4e7ed;
}

.input-toolbar {
    display: flex;
    justify-content: flex-end;
    margin-bottom: 8px;
}

.char-count {
    font-size: 12px;
    color: #999;
}

.input-wrapper {
    display: flex;
    gap: 12px;
    align-items: flex-end;
}

    .input-wrapper .el-textarea {
        flex: 1;
    }

        .input-wrapper .el-textarea .el-textarea__inner {
            border-radius: 8px;
            font-size: 14px;
            resize: none;
        }

.send-button {
    height: 40px;
    padding: 0 24px;
    border-radius: 8px;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    border: none;
}

    .send-button:hover {
        opacity: 0.9;
    }

/* 响应式布局 */
@@media screen and (max-width: 1200px) {
    .chat-sidebar {
        width: 250px !important;
    }

    .message-content-wrapper {
        max-width: 80%;
    }
}

@@media screen and (max-width: 768px) {
    .chat-container {
        height: calc(100vh - 120px);
    }

    .chat-sidebar {
        width: 200px !important;
    }

    .message-content-wrapper {
        max-width: 90%;
    }

    .chat-input-area {
        padding: 10px;
    }

    .input-wrapper {
        flex-direction: column;
        align-items: stretch;
    }

    .send-button {
        width: 100%;
    }
}
```

**关键技术点**：
- 使用 Flexbox 实现左右分栏和消息布局
- 渐变色背景和阴影效果
- 流畅的动画过渡
- 响应式设计，适配不同屏幕尺寸

---

## ✅ 验收标准

### 功能验收
- [ ] 对话页面正确加载
- [ ] 左侧显示会话历史列表
- [ ] 右侧显示当前会话的消息
- [ ] 可以发送消息并收到 AI 回复
- [ ] 消息显示正确（用户/AI区分）
- [ ] 可以创建新会话
- [ ] 可以切换会话
- [ ] 可以删除会话
- [ ] 可以对 AI 回复点赞/点踩
- [ ] 自动滚动到最新消息

### 技术验收
- [ ] API 调用正确
- [ ] 数据绑定正确
- [ ] 事件处理正确
- [ ] 错误处理完善
- [ ] 用户体验流畅

### 质量验收
- [ ] 样式美观，符合系统风格
- [ ] 响应式布局正常
- [ ] 无 JavaScript 错误
- [ ] 无样式冲突
- [ ] 代码注释清晰

---

## 📝 注意事项

⚠️ **重要**：
- 确保左侧边栏宽度合适（300px），不要太宽或太窄
- 消息列表要自动滚动到底部，但不影响用户查看历史消息
- AI 正在输入时要显示动画，提升用户体验
- 消息内容要正确换行和格式化，并进行 XSS 防护（HTML 转义）
- 时间显示要人性化（刚刚、X分钟前等）
- **只使用系统现有组件**：Vue.js、Element UI、Font Awesome、axios，不引入新的第三方库
- 所有 JS/CSS 资源使用本地文件，不使用 CDN 远程连接

⚠️ **性能考虑**：
- 消息列表使用虚拟滚动（如果消息量很大）
- 会话列表分页加载，避免一次性加载过多
- API 调用添加防抖，避免重复请求

⚠️ **用户体验**：
- 发送消息后立即清空输入框
- 显示 AI 正在输入的动画
- 提供明确的错误提示
- 支持键盘快捷键（Ctrl+Enter 发送）

---

## 🔗 相关任务
- 上一步：[Step 03: 首页UI改版](./step-03-homepage-ui.md)
- 下一步：[Step 05: 模块拖拽功能](./step-05-drag-drop.md)
- 关联文档：[scratchpad.md](../scratchpad.md)

---

## 📊 进度追踪

**任务拆解**：
- [ ] **[TASK-13]** 创建 Chat.cshtml 页面结构 (1h)
- [ ] **[TASK-14]** 创建 Chat.cshtml.cs 后端逻辑 (0.5h)
- [ ] **[TASK-15]** 创建 Chat.js 前端交互 (1h)
- [ ] **[TASK-16]** 创建 Chat.css 样式文件 (0.5h)

**预计总耗时**: 3 小时

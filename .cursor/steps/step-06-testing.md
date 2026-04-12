[中文版](step-06-testing.cn.md)

# Step 06: Integration testing and optimization

## 📋 Mission Overview
Conduct complete functional testing to ensure all features are working properly, optimize performance and user experience, and fix discovered issues.

## 🎯 Goal
- ✅ Complete end-to-end functional testing
- ✅ Verify the correctness of database operations
- ✅ Optimize front-end performance and user experience
- ✅ Fix found bugs
- ✅ Code review and specification checking
- ✅Write user documentation

## 📂Involved documents

### Test scope
- All new and modified files
- Database table structure verification
- API interface testing
- Front-end interaction testing

## 🔧 Test steps

### 1. Database table structure test

**Test Checklist**:

#### 1.1 Execute Migration
```bash
# 在 Package Manager Console 或 Terminal 中执行
# 根据使用的数据库类型选择相应的上下文

# MySQL
Add-Migration Add_AdminChat_Tables -Context AdminSenparcEntities_MySql -OutputDir Domain/Migrations/MySql
Update-Database -Context AdminSenparcEntities_MySql

# SQL Server
Add-Migration Add_AdminChat_Tables -Context AdminSenparcEntities_SqlServer -OutputDir Domain/Migrations/SqlServer
Update-Database -Context AdminSenparcEntities_SqlServer

# PostgreSQL
Add-Migration Add_AdminChat_Tables -Context AdminSenparcEntities_PostgreSQL -OutputDir Domain/Migrations/PostgreSQL
Update-Database -Context AdminSenparcEntities_PostgreSQL

# SQLite
Add-Migration Add_AdminChat_Tables -Context AdminSenparcEntities_Sqlite -OutputDir Domain/Migrations/Sqlite
Update-Database -Context AdminSerparcEntities_Sqlite
```#### 1.2 Verify table structure
- [ ] Check whether the `ADMIN_AdminChatSession` table is created successfully
- [ ] Check whether the `ADMIN_AdminChatMessage` table is created successfully
- [ ] Check whether the `ADMIN_AdminChatSessionModule` table is created successfully
- [ ] Verify whether the foreign key relationship is correct
- [ ] Check whether the index was created (if necessary)

#### 1.3 Database operation test
```sql
-- 插入测试数据
INSERT INTO ADMIN_AdminChatSession (Title, UserId, Status, LastMessageTime, AddTime, LastUpdateTime, Flag)
VALUES ('测试会话', 1, 0, GETDATE(), GETDATE(), GETDATE(), 0);

-- 查询测试
SELECT * FROM ADMIN_AdminChatSession WHERE UserId = 1;

-- 验证外键
INSERT INTO ADMIN_AdminChatMessage (SessionId, RoleType, Content, Sequence, CreatedTime, AddTime, Flag)
VALUES (1, 0, '测试消息', 1, GETDATE(), GETDATE(), 0);

-- 清理测试数据
DELETE FROM ADMIN_AdminChatMessage;
DELETE FROM ADMIN_AdminChatSession;
```---

### 2. API interface testing

**Testing Tools**: Postman / Swagger / Browser Developer Tools

#### 2.1 Create session interface
```
POST /api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.CreateSession

Body:
{
  "userId": 1,
  "initialMessage": "如何配置系统参数？"
}

预期结果:
{
  "success": true,
  "data": {
    "id": 1,
    "title": "如何配置系统参数？",
    "userId": 1,
    "status": 0,
    ...
  }
}
```#### 2.2 Get session list interface
```
GET /api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetUserSessions?userId=1&pageIndex=1&pageSize=20

预期结果:
{
  "success": true,
  "data": {
    "sessions": [...],
    "totalCount": 5,
    "pageIndex": 1,
    "pageSize": 20
  }
}
```#### 2.3 Send message interface
```
POST /api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.SendMessage

Body:
{
  "sessionId": 1,
  "content": "请告诉我如何配置邮件服务？",
  "moduleUids": []
}

预期结果:
{
  "success": true,
  "data": {
    "userMessage": {...},
    "assistantMessage": {...},
    "sessionId": 1
  }
}
```#### 2.4 Other interfaces
- [ ] Test the interface for obtaining session messages
- [ ] Test adding module to session interface
- [ ] Test delete session interface
- [ ] Test setting message feedback interface

---

### 3. Front-end functional testing

#### 3.1 Home page dialogue entrance test
- [ ] The dialogue entry is displayed correctly after the page is loaded.
- [ ] input box can be input normally
- [ ] Button disabled when empty input
- [ ] Click the button to jump to the conversation page
- [ ] Press Enter key to trigger conversation
- [ ] Drag and drop area displays normally
- [ ] Drag the module to the drag and drop area successfully

#### 3.2 Dialogue page test
- [ ] The page layout is correct (left and right columns)
- [ ] Session list loads correctly
- [ ] Message list is displayed correctly
- [ ] can send messages
- [ ] AI replies are displayed correctly
- [ ] Message automatically scrolls to the bottom
- [ ] can create new sessions
- [ ] can switch sessions
- [ ] can delete the session
- [ ] You can like/dislike the message
- [ ] input animation display

#### 3.3 Drag and drop function test
- [ ] Module cards can be dragged and dropped
- [ ] Visual feedback during dragging process
- [ ] is displayed correctly after dragging and dropping into the area
- [ ] Prompts for repeated additions
- [ ] Limitations for more than 5 modules
- [ ] can remove the selected module
- [ ] Pass the selected module to the dialog page

#### 3.4 Responsive testing
- [ ] displays normally at 1920x1080 resolution
- [ ] displays normally at 1366x768 resolution
- [ ] displays normally on tablets (768px)
- [ ] displays normally on mobile phones (< 480px)

---

### 4. Performance optimization

#### 4.1 Front-end performance optimization

**Optimization Project**:

1. **Virtual scrolling of message list** (such as message volume > 100)
```javascript
// 如果消息量非常大，可以考虑使用 Element UI 自带的虚拟滚动
// 或者实现简单的分页加载机制
// 当前实现已满足一般场景（< 100 条消息），可选优化
// 注意：不要引入新的第三方库，使用系统现有组件
```2. **Anti-shake input** (simple implementation, without introducing lodash)
```javascript
// 在 Index.js 中添加简单的防抖函数
methods: {
  // 简单的防抖实现
  debounce(func, wait) {
    let timeout;
    return function(...args) {
      clearTimeout(timeout);
      timeout = setTimeout(() => func.apply(this, args), wait);
    };
  }
}

// 如果需要实时搜索建议功能，可以使用防抖
// 当前不需要，可选优化
```3. **Lazy loading of session list**
- Paging loading has been implemented
- Can add scroll to bottom auto-loading

4. **Caching Optimization**
```javascript
// 缓存已加载的会话消息，避免重复请求
const messageCache = {};

async loadMessages(sessionId) {
  if (messageCache[sessionId]) {
    this.messageList = messageCache[sessionId];
    return;
  }
  
  // ... 原有加载逻辑 ...
  
  messageCache[sessionId] = this.messageList;
}
```#### 4.2 Backend performance optimization

**Optimization Project**:

1. **Add database index**
```sql
-- AdminChatSession 表
CREATE INDEX IX_AdminChatSession_UserId_Status ON ADMIN_AdminChatSession(UserId, Status);
CREATE INDEX IX_AdminChatSession_LastMessageTime ON ADMIN_AdminChatSession(LastMessageTime);

-- AdminChatMessage 表
CREATE INDEX IX_AdminChatMessage_SessionId_Sequence ON ADMIN_AdminChatMessage(SessionId, Sequence);
CREATE INDEX IX_AdminChatMessage_CreatedTime ON ADMIN_AdminChatMessage(CreatedTime);

-- AdminChatSessionModule 表
CREATE INDEX IX_AdminChatSessionModule_SessionId ON ADMIN_AdminChatSessionModule(SessionId);
```2. **EF Core Query Optimization**
```csharp
// 在 Service 中使用 AsNoTracking 提升查询性能
public async Task<List<AdminChatMessageDto>> GetSessionMessagesAsync(int sessionId)
{
    var messages = await base.GetFullList(z => z.SessionId == sessionId, null)
        .AsNoTracking() // 只读查询，不需要跟踪
        .OrderBy(z => z.Sequence)
        .ToListAsync();

    return messages.Select(m => new AdminChatMessageDto(m)).ToList();
}
```3. **Batch operation optimization**
```csharp
// 批量添加模块
public async Task<List<AdminChatSessionModuleDto>> AddModulesToSessionBatchAsync(
    int sessionId, 
    List<(string Uid, string Name, string Version)> modules)
{
    var sessionModules = new List<AdminChatSessionModule>();
    
    foreach (var (uid, name, version) in modules)
    {
        // 检查是否已存在
        var existing = await base.GetObjectAsync(z => z.SessionId == sessionId && z.XncfModuleUid == uid);
        if (existing == null)
        {
            sessionModules.Add(new AdminChatSessionModule(sessionId, uid, name, version));
        }
    }
    
    // 批量保存
    await base.SaveObjectListAsync(sessionModules);
    
    return sessionModules.Select(m => new AdminChatSessionModuleDto(m)).ToList();
}
```---

### 5. User experience optimization

#### 5.1 Loading state optimization
```javascript
// 在关键操作时显示加载动画
const loading = this.$loading({
  lock: true,
  text: '处理中...',
  background: 'rgba(0, 0, 0, 0.7)'
});

// 操作完成后关闭
loading.close();
```#### 5.2 Error prompt optimization
```javascript
// 使用更友好的错误提示
catch (error) {
  let errorMessage = '操作失败';
  
  if (error.response) {
    if (error.response.status === 401) {
      errorMessage = '登录已过期，请重新登录';
      setTimeout(() => {
        window.location.href = '/Admin/Login';
      }, 2000);
    } else if (error.response.data && error.response.data.message) {
      errorMessage = error.response.data.message;
    }
  }
  
  this.$message.error(errorMessage);
}
```#### 5.3 Shortcut key support
```javascript
// 在对话页面添加快捷键支持
mounted() {
  // ... 现有代码 ...
  
  // 添加键盘事件监听
  document.addEventListener('keydown', this.handleKeydown);
},

beforeDestroy() {
  document.removeEventListener('keydown', this.handleKeydown);
},

methods: {
  handleKeydown(event) {
    // Ctrl+Enter 或 Cmd+Enter 发送消息
    if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') {
      this.sendMessage();
    }
    
    // ESC 清空输入框
    if (event.key === 'Escape' && document.activeElement.tagName === 'TEXTAREA') {
      this.inputMessage = '';
    }
  }
}
```---

### 6. Code review checklist

#### 6.1 Code Quality
- [ ] All methods have XML annotations
- [ ] Clear naming of variables and methods
- [ ] No hardcoded magic numbers and strings
- [ ] Improved exception handling
- [ ] Logging key operations
- [ ] Code conforms to C# and JavaScript specifications

#### 6.2 Security Check
- [ ] User input for validation and sanitization
- [ ] SQL injection protection (using parameterized queries)
- [ ] XSS attack protection (front-end output escaping)
- [ ] Permission verification (make sure you can only access your own session)
- [ ] Sensitive information is not output in the log

#### 6.3 Maintainability Check
- [ ] The code structure is clear and the responsibilities are clearly defined
- [ ] No duplicate code
- [ ] Configuration items are configurable (such as the maximum number of modules, message length, etc.)
- [ ] Easy to extend (such as adding new message types)

---

### 7. Complete functional test scenario

#### Scenario 1: First time use process
1. User login management background
2. See the AI dialogue entrance on the homepage
3. Enter the question "How to configure the system?"
4. Click the "Start Conversation" button
5. Automatically create a conversation and jump to the conversation page
6. AI automatically responds to questions
7. Users can continue the conversation

**Expected results**:
- ✅ Smooth process, no lags
- ✅ Session created successfully
- ✅ AI replies correctly
- ✅ Save messages to database

#### Scenario 2: Module drag-and-drop dialogue process
1. The user drags the "System Configuration" module on the home page to the drag and drop area.
2. Drag the "User Management" module to the drag and drop area
3. Enter the question "How do I manage user permissions?"
4. Click "Start Conversation"
5. Jump to the dialogue page, showing 2 selected modules
6. AI considers module context when replying

**Expected results**:
- ✅ Smooth dragging and clear visual feedback
- ✅ Module added correctly
- ✅Module information is transferred to the conversation page
- ✅ Correct records in the association table

#### Scenario 3: Multiple rounds of dialogue process
1. Enter an existing session
2. Send the message "What is the XNCF module?"
3. AI reply
4. Continue to send "How to create a new module?"
5. AI replies based on context
6. Like AI replies

**Expected results**:
- ✅ The dialogue is coherent and AI understands the context
- ✅ Messages are in the correct order
- ✅ Feedback function is normal

#### Scenario 4: Session management process
1. View the conversation list on the left side of the conversation page
2. Click to switch to other sessions
3. Create a new session
4. Delete an old session
5. Verify session state updates

**Expected results**:
- ✅ Session list updated in real time
- ✅ No delay in switching sessions
- ✅ Session deleted successfully
- ✅ Database status is correct

#### Scenario 5: Exception handling
1. Send messages when the network is disconnected
2. Access when the session does not exist
3. Enter very long messages (> 2000 characters)
4. Click the send button in quick succession
5. Access when the user is not logged in

**Expected results**:
- ✅ Friendly error prompts
- ✅ No crashing or freezing
- ✅ No data loss
- ✅ Automatically redirect to login page

---

### 8. Performance testing

#### 8.1 Stress Test
- [ ] Create 100+ sessions to test list loading speed
- [ ] A single session contains 200+ messages to test the message loading speed
- [ ] Send 10 messages in quick succession to test system stability
- [ ] Open 5 conversation pages at the same time to test concurrent processing

#### 8.2 Performance indicators
| Action | Target Time | Actual Time | Status |
|------|---------|---------|------|
| Home page loading | < 2s | _To be tested_ | ⏳ |
| Dialogue page loading | < 2s | _To be tested_ | ⏳ |
| Send message (including AI reply) | < 3s | _To be tested_ | ⏳ |
| Switch session | < 1s | _To be tested_ | ⏳ |
| Loading 50 messages | < 1s | _To be tested_ | ⏳ |

---

### 9. User documentation

**File path**: `docs/AdminChat-Usage-Guide.md` (optional, create as needed)

**Content Outline**:
```markdown
# 管理后台 AI 对话功能使用指南

## 功能介绍
在 NeuCharFramework 管理后台首页增加了 AI 智能对话功能...

## 快速开始
1. 在首页找到"AI 智能助手"区域
2. 输入您的问题
3. 点击"开始对话"或按 Enter 键

## 高级功能
### 模块拖拽
1. 将功能模块卡片拖拽到对话框
2. 模块会作为上下文传递给 AI
3. AI 会基于模块功能提供更精准的回答

### 会话管理
- 创建新会话
- 切换历史会话
- 删除不需要的会话

### 消息反馈
对 AI 的回复点赞或点踩，帮助改进 AI 服务

## 常见问题
Q: AI 回复速度慢怎么办？
A: 可能是网络问题或 AI 模型配置问题...

Q: 如何切换不同的 AI 模型？
A: 在系统设置中配置默认 AI 模型...
```---

### 10. Bug fix record

**Format**:
```markdown
**Bug #1**: 消息发送后不自动滚动到底部
- **发现时间**: 2026-03-25
- **影响**: 用户体验
- **原因**: $nextTick 时机不对
- **解决方案**: 使用 setTimeout 延迟滚动
- **修复文件**: Chat.js
```---

## ✅ Acceptance Criteria

### Function acceptance
- [ ] All test scenarios passed
- [ ] No blocking bugs
- [ ] Performance indicators meet the standards
- [ ] Smooth user experience

### Technical acceptance
- [ ] Code review passed
- [ ] Security check passed
- [ ] Performance optimization completed
- [ ] Complete documentation

### Quality acceptance
- [ ] No compilation errors
- [ ] No linting warning
- [ ] No console errors
- [ ] Responsive layout is normal

---

## 📝 Notes

⚠️ **Database Migration**:
- Users indicated that they would manually perform Migration
- We just need to make sure the Model is defined correctly
- Provide Migration command reference
- It is recommended to execute it in the test environment first

⚠️ **AI Service Integration**:
- Current AI calls are simplified versions
- Needs to be adjusted according to the actual AIKernel interface
- Make sure the AI model is configured correctly
- Handle situations where AI services are unavailable

⚠️ **Production environment deployment**:
- Back up the database before executing Migration
- Check whether dependent services are ready (AI service, cache, etc.)
- Test the logged in user's permissions
- Monitor system performance and error logs
- **Confirm that all resource files are local**, no CDN remote connection is used
- Check browser compatibility (mainly supports the latest versions of Chrome, Firefox, Edge, and Safari)

---

## 🔗 Related tasks
- Previous step: [Step 05: Module drag and drop function](./step-05-drag-drop.md)
- Associated documents: [scratchpad.md](../scratchpad.md)

---

## 📊 Progress Tracking

**Task breakdown**:
- [ ] **[TASK-20]** Full functional test (0.5h)
  - Database table structure verification
  - API interface testing
  - Front-end functional testing
  - Responsive testing
  
- [ ] **[TASK-21]** Performance optimization and code review (0.5h)
  - Front-end performance optimization
  - Backend performance optimization
  - Code review
  - Security check

**Estimated total time**: 1 hour

---

## 📦 Final delivery list

### ✅ Completed features
1. **Data Model Layer** - 3 Entities + 3 DTOs
2. **Service layer** - 3 Services + 1 AppService
3. **Home Page Revision** - AI dialogue entrance + drag and drop area
4. **Dialogue Page** - Complete dialogue interface
5. **Drag and drop function** - module drag and context management

### 📄 New files (14)
- 6 data models and DTO files
- 3 Service files
- 1 AppService file
- 2 page files (cshtml + cs)
- 1 JS file
- 1 CSS file

### 🔧 Modify files (4)
- Index.cshtml
- Index.cshtml.cs
- Index.js
- AdminSenparcEntities.cs

### 📚 Documents (6)
- scratchpad.md (project planning)
- step-01-data-models.md (data model)
- step-02-service-layer.md (service layer)
- step-03-homepage-ui.md (Homepage UI)
- step-04-chat-page.md (conversation page)
- step-05-drag-drop.md (drag and drop function)
- step-06-testing.md (test optimization)

---

## 🎉 Project completion sign

The project is ready for delivery when all of the following items are completed:

- ✅ All code files have been created and modified
- ✅ Database Migration was executed successfully
- ✅ All test scenarios passed
- ✅ Performance indicators meet standards
- ✅ User acceptance passed

**Congratulations! The management backend AI dialogue function revision project is completed! ** 🎊

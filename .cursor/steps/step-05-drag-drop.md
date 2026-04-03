# Step 05: 模块拖拽功能实现

## 📋 任务概述
为首页的功能模块卡片添加拖拽功能，支持将模块拖拽到对话入口的拖放区域，实现基于模块上下文的 AI 对话。

## 🎯 目标
- ✅ 为模块卡片添加拖拽支持（draggable）
- ✅ 实现对话框拖放区域的接收逻辑
- ✅ 显示已选中的模块列表
- ✅ 支持移除已选模块
- ✅ 拖拽过程提供视觉反馈
- ✅ 将选中模块传递给对话页面

## 📂 涉及文件

### 修改文件
1. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/Index.cshtml` - 已在 Step 03 中添加拖放区域
2. `tools/NcfSimulatedSite/Senparc.Areas.Admin/wwwroot/js/Admin/Pages/Index/Index.js` - 需要增强拖拽逻辑

## 🔧 实现步骤

### 1. 增强模块卡片的拖拽功能

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/wwwroot/js/Admin/Pages/Index/Index.js`

**修改位置**: 在已有的 `initializeModuleDrag()` 方法中增强（Step 03 已添加基础版本）

**完整增强版本**:

```javascript
/**
 * 初始化模块拖拽功能（增强版）
 */
initializeModuleDrag() {
  this.$nextTick(() => {
    // 为所有模块卡片添加拖拽支持
    const moduleCards = document.querySelectorAll('#xncf-modules-area .xncf-item');
    
    moduleCards.forEach((card, index) => {
      card.setAttribute('draggable', 'true');
      card.style.cursor = 'move';
      
      // dragstart - 开始拖拽
      card.addEventListener('dragstart', (event) => {
        // 获取模块数据
        const cardElement = event.currentTarget;
        const linkElement = cardElement.querySelector('a[href*="uid="]');
        const headerElement = cardElement.querySelector('.el-card__header span:first-child');
        const iconElement = cardElement.querySelector('.icon');
        const versionElement = cardElement.querySelector('.version');
        const descElement = cardElement.querySelector('.description');

        const moduleData = {
          uid: linkElement?.href?.match(/uid=([^&]+)/)?.[1] || '',
          name: headerElement?.textContent?.trim() || '未知模块',
          icon: iconElement?.className || 'fa fa-cube',
          version: versionElement?.textContent?.trim() || '',
          description: descElement?.textContent?.trim() || ''
        };
        
        // 设置拖拽数据
        event.dataTransfer.setData('application/json', JSON.stringify(moduleData));
        event.dataTransfer.effectAllowed = 'copy';
        
        // 添加拖拽视觉反馈
        cardElement.style.opacity = '0.5';
        cardElement.classList.add('dragging');
        
        // 高亮拖放区域
        const dropZone = document.querySelector('.chat-module-drop-zone');
        if (dropZone) {
          dropZone.classList.add('highlight');
        }
      });

      // dragend - 拖拽结束
      card.addEventListener('dragend', (event) => {
        event.currentTarget.style.opacity = '1';
        event.currentTarget.classList.remove('dragging');
        
        // 移除拖放区域高亮
        const dropZone = document.querySelector('.chat-module-drop-zone');
        if (dropZone) {
          dropZone.classList.remove('highlight');
        }
      });
    });
  });
},

/**
 * 处理模块拖放（已在 Step 03 中添加，这里是完整版）
 */
handleModuleDrop(event) {
  this.isDragOver = false;
  
  try {
    const moduleData = JSON.parse(event.dataTransfer.getData('application/json'));
    
    // 验证数据完整性
    if (!moduleData.uid || !moduleData.name) {
      this.$message.warning('模块数据不完整');
      return;
    }
    
    // 检查是否已添加
    if (this.selectedModules.some(m => m.uid === moduleData.uid)) {
      this.$message.info(`模块"${moduleData.name}"已在上下文中`);
      return;
    }

    // 检查数量限制（最多5个模块）
    if (this.selectedModules.length >= 5) {
      this.$message.warning('最多只能添加 5 个模块到对话上下文');
      return;
    }

    // 添加到选中列表
    this.selectedModules.push({
      uid: moduleData.uid,
      name: moduleData.name,
      icon: moduleData.icon,
      version: moduleData.version,
      description: moduleData.description
    });

    // 添加动画效果
    this.$nextTick(() => {
      const tags = document.querySelectorAll('.module-tag');
      if (tags.length > 0) {
        const lastTag = tags[tags.length - 1];
        lastTag.classList.add('tag-appear');
        setTimeout(() => {
          lastTag.classList.remove('tag-appear');
        }, 500);
      }
    });

    this.$message.success({
      message: `已添加模块：${moduleData.name}`,
      duration: 2000
    });
  } catch (error) {
    console.error('处理拖放失败:', error);
    this.$message.error('添加模块失败，请重试');
  }
},

/**
 * 拖拽进入拖放区域
 */
handleDragOver(event) {
  event.preventDefault();
  event.dataTransfer.dropEffect = 'copy';
  this.isDragOver = true;
},

/**
 * 拖拽离开拖放区域
 */
handleDragLeave(event) {
  // 检查是否真的离开了拖放区域（避免子元素触发）
  const dropZone = event.currentTarget;
  const relatedTarget = event.relatedTarget;
  
  if (!dropZone.contains(relatedTarget)) {
    this.isDragOver = false;
  }
},

/**
 * 移除已选模块
 */
removeModule(uid) {
  const index = this.selectedModules.findIndex(m => m.uid === uid);
  if (index !== -1) {
    const moduleName = this.selectedModules[index].name;
    this.selectedModules.splice(index, 1);
    
    this.$message({
      message: `已移除模块：${moduleName}`,
      type: 'info',
      duration: 2000
    });
  }
},

/**
 * 清空所有选中的模块
 */
clearSelectedModules() {
  if (this.selectedModules.length === 0) {
    return;
  }

  this.$confirm('确定要清空所有选中的模块吗？', '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '取消',
    type: 'warning'
  }).then(() => {
    this.selectedModules = [];
    this.$message.success('已清空选中模块');
  }).catch(() => {
    // 用户取消
  });
},

/**
 * 增强的开始对话方法（包含模块信息）
 */
async startChatSession() {
  if (!this.chatInputText || this.chatInputText.trim().length === 0) {
    this.$message.warning('请输入您的问题');
    return;
  }

  const message = this.chatInputText.trim();
  
  // 显示加载状态
  const loading = this.$loading({
    lock: true,
    text: '正在创建对话...',
    background: 'rgba(0, 0, 0, 0.7)'
  });

  try {
    // 创建新的对话会话
    const response = await service.post(
      '/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.CreateSession',
      {
        userId: this.getCurrentUserId(),
        initialMessage: message
      }
    );

    if (response.data && response.data.success && response.data.data) {
      const sessionId = response.data.data.id;
      
      // 如果有选中的模块，添加到会话
      if (this.selectedModules.length > 0) {
        await this.addModulesToSession(sessionId);
      }

      // 跳转到对话页面，并传递初始消息和模块信息
      const moduleUids = this.selectedModules.map(m => m.uid).join(',');
      let targetUrl = `/Admin/AdminChat/Chat?sessionId=${sessionId}&initialMessage=${encodeURIComponent(message)}`;
      
      if (moduleUids) {
        targetUrl += `&moduleUids=${moduleUids}`;
      }

      window.location.href = targetUrl;
    } else {
      this.$message.error('创建对话失败，请稍后再试');
    }
  } catch (error) {
    console.error('创建对话失败:', error);
    this.$message.error('创建对话失败：' + (error.response?.data?.message || error.message || '未知错误'));
  } finally {
    loading.close();
  }
},

/**
 * 将选中的模块批量添加到会话
 */
async addModulesToSession(sessionId) {
  try {
    for (const module of this.selectedModules) {
      await service.post(
        '/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.AddModuleToSession',
        {
          sessionId: sessionId,
          moduleUid: module.uid,
          moduleName: module.name,
          moduleVersion: module.version
        }
      );
    }
  } catch (error) {
    console.error('添加模块到会话失败:', error);
    // 不阻断流程，只记录错误
  }
},
```

**关键技术点**：
- 使用 `dataTransfer` 传递拖拽数据
- 拖拽过程中的视觉反馈（opacity、class）
- 限制最多5个模块，避免上下文过长
- 批量添加模块到会话

---

### 2. 增强拖放区域样式

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/Index.cshtml`

**在 @section style 中添加额外样式**:

```css
/* 拖拽增强样式 */
#xncf-modules-area .xncf-item.dragging {
    opacity: 0.5;
    transform: scale(0.95);
}

#xncf-modules-area .xncf-item[draggable="true"] {
    cursor: move;
}

    #xncf-modules-area .xncf-item[draggable="true"]:hover::after {
        content: '拖拽到下方对话框';
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        background: rgba(140, 82, 255, 0.9);
        color: white;
        padding: 8px 16px;
        border-radius: 6px;
        font-size: 12px;
        white-space: nowrap;
        pointer-events: none;
        opacity: 0;
        transition: opacity 0.3s ease;
    }

    #xncf-modules-area .xncf-item[draggable="true"]:hover:hover::after {
        opacity: 1;
    }

.chat-module-drop-zone.highlight {
    border-color: #8c52ff;
    background: linear-gradient(135deg, #f5f0ff 0%, #faf5ff 100%);
    animation: pulse-border 1s infinite;
}

@@keyframes pulse-border {
    0%, 100% {
        box-shadow: 0 0 0 rgba(140, 82, 255, 0.4);
    }

    50% {
        box-shadow: 0 0 20px rgba(140, 82, 255, 0.6);
    }
}

.module-tag.tag-appear {
    animation: tagAppear 0.5s ease;
}

@@keyframes tagAppear {
    0% {
        opacity: 0;
        transform: scale(0.5) translateY(-20px);
    }

    60% {
        transform: scale(1.1);
    }

    100% {
        opacity: 1;
        transform: scale(1) translateY(0);
    }
}

/* 选中模块标签悬停效果 */
.module-tag {
    transition: all 0.3s ease;
    cursor: default;
}

    .module-tag:hover {
        transform: translateY(-2px);
        box-shadow: 0 2px 8px rgba(140, 82, 255, 0.2);
    }

/* 拖放区域空状态样式 */
.drop-zone-placeholder {
    transition: all 0.3s ease;
}

.chat-module-drop-zone.drag-over .drop-zone-placeholder .fa {
    color: #8c52ff;
    animation: bounce 0.6s ease infinite;
}

@@keyframes bounce {
    0%, 100% {
        transform: translateY(0);
    }

    50% {
        transform: translateY(-10px);
    }
}
```

**关键技术点**：
- 拖拽状态的 CSS 动画反馈
- 悬停提示，引导用户操作
- 边框脉冲动画，突出拖放区域
- 模块标签添加动画

---

### 3. 在对话页面中处理模块上下文

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/AdminChat/Chat.cshtml.cs`

**修改**: 在 `OnGet` 方法中添加模块UID的处理

```csharp
public IActionResult OnGet(int? sessionId, string initialMessage, string moduleUids)
{
    // 获取当前登录用户
    CurrentUserId = GetCurrentUserId();

    if (CurrentUserId <= 0)
    {
        return RedirectToPage("/Login");
    }

    SessionId = sessionId ?? 0;
    InitialMessage = initialMessage;
    
    // 解析模块 UIDs
    if (!string.IsNullOrEmpty(moduleUids))
    {
        ModuleUids = moduleUids.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(uid => uid.Trim())
            .ToList();
    }

    return Page();
}

public List<string> ModuleUids { get; set; } = new List<string>();
```

**关键技术点**：
- 从 QueryString 获取多个模块 UID
- 传递给前端用于加载模块信息

---

### 4. 在对话页面中显示模块上下文

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/wwwroot/js/Admin/Pages/AdminChat/Chat.js`

**添加方法**: 在 `mounted()` 中添加模块加载逻辑

```javascript
mounted() {
  // 从页面获取初始数据
  const urlParams = new URLSearchParams(window.location.search);
  this.currentSessionId = parseInt(urlParams.get('sessionId')) || 0;
  this.initialMessage = urlParams.get('initialMessage') || '';
  this.currentUserId = this.getCurrentUserId();

  // 获取模块 UIDs
  const moduleUids = urlParams.get('moduleUids');
  if (moduleUids) {
    this.initialModuleUids = moduleUids.split(',');
  }

  // 加载会话列表
  this.loadSessions();

  // 如果有会话ID，加载消息和模块
  if (this.currentSessionId > 0) {
    this.loadMessages(this.currentSessionId);
    this.loadSessionModules(this.currentSessionId);
    
    // 如果有初始消息，自动发送
    if (this.initialMessage) {
      this.inputMessage = this.initialMessage;
      this.$nextTick(() => {
        this.sendMessage();
      });
    }
  }
},
```

**添加加载模块方法**:

```javascript
/**
 * 加载会话关联的模块
 */
async loadSessionModules(sessionId) {
  try {
    const response = await service.get(
      `/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetSessionModules`,
      {
        params: { sessionId: sessionId }
      }
    );

    if (response.data && response.data.success && response.data.data) {
      this.currentSessionModules = response.data.data;
    }
  } catch (error) {
    console.error('加载会话模块失败:', error);
  }
},
```

**在 data() 中添加字段**:

```javascript
data() {
  return {
    // ... 现有字段 ...
    initialModuleUids: [], // 从首页传递过来的模块 UIDs
  };
},
```

---

### 5. 在 AppService 中添加获取会话模块的接口

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/OHS/Local/AppService/AdminChatAppService.cs`

**添加方法**:

```csharp
/// <summary>
/// 获取会话关联的模块列表
/// </summary>
[ApiBind(ApiRequestMethod = ApiRequestMethod.Get)]
public async Task<AppResponseBase<List<AdminChatSessionModuleDto>>> GetSessionModules([FromQuery] int sessionId)
{
    return await this.GetResponseAsync<List<AdminChatSessionModuleDto>>(
        async (response, logger) =>
        {
            return await _sessionModuleService.GetSessionModulesAsync(sessionId);
        });
}

/// <summary>
/// 设置消息反馈
/// </summary>
[ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
public async Task<AppResponseBase<bool>> SetMessageFeedback([FromBody] MessageFeedbackRequest request)
{
    return await this.GetResponseAsync<bool>(
        async (response, logger) =>
        {
            return await _messageService.SetMessageFeedbackAsync(request.MessageId, request.IsLike);
        });
}

/// <summary>
/// 消息反馈请求
/// </summary>
public class MessageFeedbackRequest
{
    public int MessageId { get; set; }
    public bool IsLike { get; set; }
}
```

---

## ✅ 验收标准

### 功能验收
- [ ] 模块卡片可以拖拽
- [ ] 拖拽到拖放区域时有视觉反馈
- [ ] 拖放成功后模块显示在拖放区域
- [ ] 可以点击标签关闭按钮移除模块
- [ ] 最多只能添加 5 个模块
- [ ] 重复添加会有提示
- [ ] 选中的模块会传递到对话页面
- [ ] 对话页面显示模块上下文

### 技术验收
- [ ] 使用标准 HTML5 Drag & Drop API
- [ ] 拖拽数据使用 JSON 格式
- [ ] 防止重复添加
- [ ] 数量限制生效
- [ ] 模块信息正确传递

### 质量验收
- [ ] 拖拽交互流畅
- [ ] 视觉反馈明确
- [ ] 错误处理完善
- [ ] 用户提示友好
- [ ] 无 JavaScript 错误

---

## 🔍 测试建议

### 拖拽功能测试
1. 拖拽一个模块到拖放区域，验证是否成功添加
2. 尝试重复添加同一个模块，验证是否有提示
3. 添加 5 个模块后，尝试添加第 6 个，验证是否有限制
4. 点击标签的关闭按钮，验证是否正确移除
5. 拖拽过程中移动鼠标，验证视觉反馈

### 上下文传递测试
1. 在首页选中 2-3 个模块后开始对话
2. 在对话页面检查是否显示这些模块
3. 发送消息，验证 AI 是否理解模块上下文
4. 查看数据库，验证关联表是否正确记录

### 交互体验测试
1. 测试拖拽的流畅度
2. 检查动画效果是否自然
3. 验证提示消息是否及时
4. 测试不同浏览器的兼容性

---

## 📝 注意事项

⚠️ **重要**：
- 拖拽数据必须使用 JSON 格式，便于解析
- 要处理拖拽失败的情况（数据不完整等）
- 拖放区域的 `dragover` 事件必须 `preventDefault()`，否则 `drop` 不会触发
- 拖拽离开要判断是否真的离开（避免子元素触发）

⚠️ **浏览器兼容性**：
- HTML5 Drag & Drop API 在主流浏览器均支持（Chrome、Firefox、Edge、Safari）
- IE11 及以下可能有兼容性问题（系统已不支持旧版 IE）
- 移动端的拖拽体验较差，建议提供备选方案（如点击选择）
- 不引入任何拖拽库（如 SortableJS），使用原生 API

⚠️ **性能考虑**：
- 拖拽数据不要包含过大的对象
- 动画效果要流畅，使用 CSS3 transition 和 animation
- 批量添加模块时使用 for 循环即可（不需要 Promise.all 优化）
- 使用系统现有的动画和过渡效果

---

## 🔗 相关任务
- 上一步：[Step 04: 对话任务页面](./step-04-chat-page.md)
- 下一步：[Step 06: 集成测试和优化](./step-06-testing.md)
- 关联文档：[scratchpad.md](../scratchpad.md)

---

## 📊 进度追踪

**任务拆解**：
- [ ] **[TASK-17]** 为模块卡片添加拖拽支持 (0.5h)
- [ ] **[TASK-18]** 实现对话框拖放区域 (0.5h)
- [ ] **[TASK-19]** 实现选中模块显示和管理 (0.5h)

**预计总耗时**: 1.5 小时

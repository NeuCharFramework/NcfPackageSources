# Step 05: Implementation of module drag and drop function

## 📋 Mission Overview
Add a drag-and-drop function to the function module card on the homepage to support dragging the module to the drag-and-drop area of ​​the dialogue entrance to implement AI dialogue based on module context.

## 🎯 Goal
- ✅ Add draggable support for module cards
- ✅ Implement the receiving logic of the drag and drop area of ​​the dialog box
- ✅ Display the selected module list
- ✅ Supports removing selected modules
- ✅ Provide visual feedback during the dragging process
- ✅ Pass the selected module to the conversation page

## 📂Involved documents

### Modify files
1. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/Index.cshtml`- Added drag and drop area in Step 03
2. `tools/NcfSimulatedSite/Senparc.Areas.Admin/wwwroot/js/Admin/Pages/Index/Index.js`- Need to enhance drag and drop logic

## 🔧 Implementation steps

### 1. Enhance the drag and drop function of module cards

**File path**:`tools/NcfSimulatedSite/Senparc.Areas.Admin/wwwroot/js/Admin/Pages/Index/Index.js`

**Modify location**: in existing`initializeModuleDrag()`Enhancement in method (Basic version has been added in Step 03)

**Full enhanced version**:

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

**Key technical points**:
- use`dataTransfer`Pass drag and drop data
- Visual feedback (opacity, class) during dragging
- Limit up to 5 modules to avoid too long contexts
- Add modules to sessions in batches

---

### 2. Enhance drag and drop area style

**File path**:`tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/Index.cshtml`

**Add additional styles in @section style**:

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

**Key technical points**:
- CSS animation feedback for dragging state
- Hover prompts to guide users to operate
- Border pulse animation to highlight drag and drop areas
- Add animation to module label

---

### 3. Handle module context in conversation page

**File path**:`tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/AdminChat/Chat.cshtml.cs`

**Modification**: in`OnGet`Processing of adding module UID in method

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

**Key technical points**:
- Get multiple module UIDs from QueryString
- Passed to the front end for loading module information

---

### 4. Display module context in conversation page

**File path**:`tools/NcfSimulatedSite/Senparc.Areas.Admin/wwwroot/js/Admin/Pages/AdminChat/Chat.js`

**Add method**: in`mounted()`Add module loading logic in

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

**Add loading module method**:

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

**Add fields in data()**:

```javascript
data() {
  return {
    // ... 现有字段 ...
    initialModuleUids: [], // 从首页传递过来的模块 UIDs
  };
},
```

---

### 5. Add the interface to obtain the session module in AppService

**File path**:`tools/NcfSimulatedSite/Senparc.Areas.Admin/OHS/Local/AppService/AdminChatAppService.cs`

**Add method**:

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

## ✅ Acceptance Criteria

### Function acceptance
- [ ] Module cards can be dragged and dropped
- [ ] Visual feedback when dragging to the drop area
- [ ] After the drag and drop is successful, the module is displayed in the drag and drop area.
- [ ] You can click the label close button to remove the module
- [ ] Only up to 5 modules can be added
- [ ] There will be a prompt if you add it repeatedly.
- [ ] The selected module will be passed to the conversation page
- [ ] Dialog page displays module context

### Technical acceptance
- [ ] Use standard HTML5 Drag & Drop API
- [ ] Drag and drop data using JSON format
- [ ] Prevent duplicate additions
- [ ] Quantity limit is in effect
- [ ] Module information is passed correctly

### Quality acceptance
- [ ] Smooth drag and drop interaction
- [ ] Clear visual feedback
- [ ] Error handling improved
- [ ] User friendly prompts
- [ ] No JavaScript errors

---

## 🔍 Testing suggestions

### Drag and drop function test
1. Drag a module to the drag and drop area and verify whether it is added successfully.
2. Try to add the same module repeatedly and verify whether there are prompts
3. After adding 5 modules, try to add the 6th one to verify whether there are any restrictions.
4. Click the close button of the label to verify that it was removed correctly.
5. Move the mouse during dragging to verify visual feedback

### Context transfer test
1. Select 2-3 modules on the homepage and start the conversation
2. Check whether these modules are displayed on the dialog page
3. Send a message to verify whether the AI ​​understands the module context
4. Check the database to verify whether the associated table is correctly recorded

### Interactive experience test
1. Test the smoothness of drag and drop
2. Check whether the animation effect is natural
3. Verify whether the prompt message is timely
4. Test the compatibility of different browsers

---

## 📝 Notes

⚠️ **Important**:
- Drag and drop data must use JSON format for easy parsing
- To handle the situation of drag failure (incomplete data, etc.)
- Drag and drop area`dragover`event must`preventDefault()`,otherwise`drop`will not trigger
- When dragging to leave, you need to determine whether it is really left (to avoid triggering by child elements)

⚠️ **Browser Compatibility**:
- HTML5 Drag & Drop API is supported in all major browsers (Chrome, Firefox, Edge, Safari)
- IE11 and below may have compatibility issues (the system no longer supports older versions of IE)
- The drag-and-drop experience on mobile is poor, so it is recommended to provide alternatives (such as click selection)
- Does not introduce any drag and drop libraries (such as SortableJS) and uses native API

⚠️ **Performance Considerations**:
- Do not drag and drop objects that contain too large objects
- The animation effect should be smooth, use CSS3 transition and animation
- Just use a for loop when adding modules in batches (Promise.all optimization is not required)
- Use the system's existing animations and transitions

---

## 🔗 Related tasks
- Previous step: [Step 04: Chat task page](./step-04-chat-page.md)
- Next step: [Step 06: Integration testing and optimization](./step-06-testing.md)
- Associated documents: [scratchpad.md](../scratchpad.md)

---

## 📊 Progress Tracking

**Task breakdown**:
- [ ] **[TASK-17]** Add drag and drop support for module cards (0.5h)
- [ ] **[TASK-18]** Implement dialog drag and drop area (0.5h)
- [ ] **[TASK-19]** Implement display and management of selected modules (0.5h)

**Estimated total time**: 1.5 hours

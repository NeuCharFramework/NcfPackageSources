[中文版](step-03-homepage-ui.cn.md)

# Step 03: Home page UI revision - Add AI dialogue entrance

## 📋 Mission Overview
Add an eye-catching AI dialogue entry prompt box below the statistics area on the homepage of the management backend to allow users to input and jump to a dedicated dialogue page.

## 🎯 Goal
- ✅ Added AI dialogue entrance prompt box below the statistics area
- ✅ Design modern and eye-catching dialog box styles
- ✅ Implement input box interaction and page jump logic
- ✅ Retain the original functional module area
- ✅ Adapt to responsive layout

## 📂Involved documents

### Modify files
1. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/Index.cshtml`
2. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/Index.cshtml.cs`
3. `tools/NcfSimulatedSite/Senparc.Areas.Admin/wwwroot/js/Admin/Pages/Index/Index.js`

## 🔧 Implementation steps

### 1. Modify Index.cshtml - add AI dialogue entrance

**File path**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/Index.cshtml`

**Change location**: Insert between `</el-container>` (line 356) and `<!-- Function module -->` (line 358)

**INSERT CONTENT**:
```cshtml
<!-- AI chat entry -->
<div id="ai-chat-entrance">
    <el-card class="chat-entrance-card">
        <div class="chat-entrance-container">
            <div class="chat-entrance-icon">
                <i class="fa fa-comments"></i>
            </div>
            <div class="chat-entrance-content">
                <h3 class="chat-entrance-title">
                    <i class="fa fa-magic"></i> AI assistant
                </h3>
                <p class="chat-entrance-subtitle">Ask a question to manage the system more efficiently</p>
                <div class="chat-entrance-input-wrapper">
                    <el-input
                        v-model="chatInputText"
                        placeholder="Type your question, e.g. how do I configure system parameters?"
                        @@keyup.enter.native="startChatSession"
                        class="chat-entrance-input"
                        clearable>
                        <el-button 
                            slot="append" 
                            icon="el-icon-s-promotion" 
                            @@click="startChatSession"
                            :disabled="!chatInputText || chatInputText.trim().length === 0">
                            Start chat
                        </el-button>
                    </el-input>
                </div>
                <div class="chat-entrance-tips">
                    <span class="tip-item"><i class="fa fa-lightbulb-o"></i> Multi-turn dialogue</span>
                    <span class="tip-item"><i class="fa fa-cube"></i> Drag modules into context</span>
                    <span class="tip-item"><i class="fa fa-history"></i> History is saved</span>
                </div>
            </div>
            <!-- Drop zone -->
            <div class="chat-module-drop-zone" 
                 @@drop.prevent="handleModuleDrop"
                 @@dragover.prevent="handleDragOver"
                 @@dragleave="handleDragLeave"
                 :class="{ 'drag-over': isDragOver }">
                <div v-if="selectedModules.length === 0" class="drop-zone-placeholder">
                    <i class="fa fa-arrow-down"></i>
                    <p>Drop modules here to add them to the chat context</p>
                </div>
                <div v-else class="selected-modules-list">
                    <el-tag
                        v-for="module in selectedModules"
                        :key="module.uid"
                        closable
                        @@close="removeModule(module.uid)"
                        type="info"
                        class="module-tag">
                        <i :class="module.icon"></i> {{module.name}}
                    </el-tag>
                </div>
            </div>
        </div>
    </el-card>
</div>
```
**Style addition**: Add in `@section style` (before line 273)
```css
/* AI chat entry */
#ai-chat-entrance {
    margin: 30px 20px;
}

.chat-entrance-card {
    border-radius: 12px;
    box-shadow: 0 2px 20px rgba(140, 82, 255, 0.1);
    transition: all 0.3s ease;
}

    .chat-entrance-card:hover {
        box-shadow: 0 4px 30px rgba(140, 82, 255, 0.2);
        transform: translateY(-2px);
    }

    .chat-entrance-card .el-card__body {
        padding: 30px;
    }

.chat-entrance-container {
    display: flex;
    flex-direction: column;
    gap: 20px;
}

.chat-entrance-icon {
    text-align: center;
}

    .chat-entrance-icon .fa {
        font-size: 48px;
        color: #8c52ff;
        animation: pulse 2s ease-in-out infinite;
    }

@@keyframes pulse {
    0%, 100% {
        opacity: 1;
        transform: scale(1);
    }

    50% {
        opacity: 0.8;
        transform: scale(1.05);
    }
}

.chat-entrance-content {
    text-align: center;
}

.chat-entrance-title {
    font-size: 24px;
    font-weight: 600;
    color: #333;
    margin-bottom: 8px;
}

    .chat-entrance-title .fa {
        color: #8c52ff;
        margin-right: 8px;
    }

.chat-entrance-subtitle {
    font-size: 14px;
    color: #666;
    margin-bottom: 20px;
}

.chat-entrance-input-wrapper {
    max-width: 800px;
    margin: 0 auto 15px;
}

.chat-entrance-input {
    font-size: 15px;
}

    .chat-entrance-input .el-input__inner {
        border-radius: 24px;
        padding-left: 20px;
        font-size: 15px;
    }

    .chat-entrance-input .el-input-group__append {
        border-radius: 0 24px 24px 0;
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        border: none;
        color: white;
    }

        .chat-entrance-input .el-input-group__append .el-button {
            color: white;
            font-weight: 600;
        }

            .chat-entrance-input .el-input-group__append .el-button:hover {
                background: rgba(255, 255, 255, 0.1);
            }

.chat-entrance-tips {
    display: flex;
    justify-content: center;
    gap: 25px;
    flex-wrap: wrap;
}

.tip-item {
    font-size: 13px;
    color: #999;
}

    .tip-item .fa {
        color: #8c52ff;
        margin-right: 5px;
    }

/* Drop zone */
.chat-module-drop-zone {
    min-height: 80px;
    border: 2px dashed #d9d9d9;
    border-radius: 8px;
    padding: 15px;
    background: #fafafa;
    transition: all 0.3s ease;
}

    .chat-module-drop-zone.drag-over {
        border-color: #8c52ff;
        background: #f5f0ff;
        box-shadow: 0 0 15px rgba(140, 82, 255, 0.2);
    }

.drop-zone-placeholder {
    text-align: center;
    color: #999;
}

    .drop-zone-placeholder .fa {
        font-size: 24px;
        color: #d9d9d9;
        margin-bottom: 8px;
    }

    .drop-zone-placeholder p {
        font-size: 13px;
        margin: 0;
    }

.selected-modules-list {
    display: flex;
    flex-wrap: wrap;
    gap: 10px;
}

.module-tag {
    font-size: 14px;
    padding: 8px 12px;
    border-radius: 6px;
}

    .module-tag .fa {
        margin-right: 5px;
    }

/* Responsive */
@@media screen and (max-width: 768px) {
    .chat-entrance-input-wrapper {
        max-width: 100%;
    }

    .chat-entrance-tips {
        flex-direction: column;
        gap: 10px;
    }

    .chat-entrance-card .el-card__body {
        padding: 20px;
    }
}
```
**Key technical points**:
- Use the Card, Input, and Button components of the system's existing Element UI
- Gradient color button + CSS3 animation effect (no need to introduce new libraries)
- Drag and drop area uses native HTML5 Drag & Drop API
- Responsive design, supports mobile terminals
- All icons use Font Awesome already in the system
- Does not introduce any new third-party libraries or CDN resources

---

### 2. Modify Index.js - add interactive logic

**File path**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/wwwroot/js/Admin/Pages/Index/Index.js`

**Modification position 1**: Add new fields in `data()` (between lines 4-15)
```javascript
data() {
  return {
    isExpandAll: true,
    loading: false,
    refreshTable: true,
    xncfStat: {},
    xncfOpeningList: {},
    chartData: [],
    todayLogData: [],
    shakeAllModules: false,
    glowUpgradeableModules: false,
    // AI chat state
    chatInputText: '',
    isDragOver: false,
    selectedModules: [], // selected modules [{uid, name, icon, version}]
  };
},
```
**Change position 2**: Add new method in `methods` (after line 245)
```javascript
// ========== AI chat helpers ==========

/**
 * Start a chat session
 */
async startChatSession() {
  if (!this.chatInputText || this.chatInputText.trim().length === 0) {
    this.$message.warning('Please enter your question');
    return;
  }

  const message = this.chatInputText.trim();

  try {
    const response = await service.post(
      '/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.CreateSession',
      {
        userId: this.getCurrentUserId(),
        initialMessage: message
      }
    );

    if (response.data && response.data.success && response.data.data) {
      const sessionId = response.data.data.id;

      if (this.selectedModules.length > 0) {
        await this.addModulesToSession(sessionId);
      }

      window.location.href = `/Admin/AdminChat/Chat?sessionId=${sessionId}&initialMessage=${encodeURIComponent(message)}`;
    } else {
      this.$message.error('Failed to create session. Try again later.');
    }
  } catch (error) {
    console.error('Create session failed:', error);
    this.$message.error('Create session failed: ' + (error.message || 'Unknown error'));
  }
},

/**
 * Current user id (from store, cookie, or claims)
 */
getCurrentUserId() {
  // TODO: wire to real auth state
  return 1;
},

/**
 * Attach selected modules to session
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
    console.error('Add modules to session failed:', error);
  }
},

/**
 * Handle module drop
 */
handleModuleDrop(event) {
  this.isDragOver = false;

  try {
    const moduleData = JSON.parse(event.dataTransfer.getData('application/json'));

    if (this.selectedModules.some(m => m.uid === moduleData.uid)) {
      this.$message.info('Module already added');
      return;
    }

    this.selectedModules.push({
      uid: moduleData.uid,
      name: moduleData.name,
      icon: moduleData.icon,
      version: moduleData.version
    });

    this.$message.success(`Added module: ${moduleData.name}`);
  } catch (error) {
    console.error('Drop handling failed:', error);
    this.$message.error('Failed to add module');
  }
},

/**
 * Drag enter drop zone
 */
handleDragOver(event) {
  this.isDragOver = true;
},

/**
 * Drag leave drop zone
 */
handleDragLeave(event) {
  this.isDragOver = false;
},

/**
 * Remove selected module
 */
removeModule(uid) {
  const index = this.selectedModules.findIndex(m => m.uid === uid);
  if (index !== -1) {
    const moduleName = this.selectedModules[index].name;
    this.selectedModules.splice(index, 1);
    this.$message.success(`Removed module: ${moduleName}`);
  }
},
```
**Modify location 3**: Add initialization in `mounted()` (before line 24)
```javascript
mounted() {
  this.getXncfStat();
  this.getXncfOpening();
  this.fetchChartData();
  this.fetchTodayLogData();
  this.initializeHoverEffects();
  this.initializeModuleDrag();
},
```
**Add method**: Add in `methods` (after line 245)
```javascript
/**
 * Initialize module drag sources
 */
initializeModuleDrag() {
  this.$nextTick(() => {
    const moduleCards = document.querySelectorAll('#xncf-modules-area .xncf-item');
    moduleCards.forEach(card => {
      card.setAttribute('draggable', 'true');

      card.addEventListener('dragstart', (event) => {
        const moduleData = {
          uid: event.currentTarget.querySelector('a[href*="uid="]')?.href?.match(/uid=([^&]+)/)?.[1],
          name: event.currentTarget.querySelector('.el-card__header span')?.textContent?.trim(),
          icon: event.currentTarget.querySelector('.icon')?.className,
          version: event.currentTarget.querySelector('.version')?.textContent?.trim()
        };

        event.dataTransfer.setData('application/json', JSON.stringify(moduleData));
        event.dataTransfer.effectAllowed = 'copy';

        event.currentTarget.style.opacity = '0.5';
      });

      card.addEventListener('dragend', (event) => {
        event.currentTarget.style.opacity = '1';
      });
    });
  });
},
```
**Key technical points**:
- Data binding and event handling using Vue
- HTML5 Drag & Drop API to implement drag and drop
- Element UI's Message component provides user feedback
- Responsive interaction, instant feedback

---

### 3. Modify Index.cshtml.cs - add backend support (optional)

**File path**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Areas/Admin/Pages/Index.cshtml.cs`

**Note**: Since the dialogue function is mainly implemented through the API interface, Index.cshtml.cs does not need to be modified for the time being. If you need to obtain user information when the page loads, you can add the corresponding attributes.

**OPTIONAL ADDITION**:
```csharp
// On IndexModel
public int CurrentUserId { get; set; }

public IActionResult OnGet()
{
    CurrentUserId = GetCurrentUserId();
    return null;
}

private int GetCurrentUserId()
{
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
    {
        return userId;
    }
    return 0;
}
```
**Used in Index.cshtml**:
```cshtml
@{
    var currentUserId = Model.CurrentUserId;
}

<script>
    // Pass into Vue bootstrap
    var __INITIAL_DATA__ = {
        currentUserId: @currentUserId
    };
</script>
```
---

## ✅ Acceptance Criteria

### Function acceptance
- [ ] Display the AI dialogue entry prompt box after the page is loaded.
- [ ] The tooltip is located below the statistics area with appropriate margin
- [ ] input box can input text normally
- [ ] Click the "Start Conversation" button to jump to the conversation page
- [ ] Pressing the Enter key can also trigger a conversation
- [ ] Button disabled when empty input
- [ ] Drag and drop area displays normally
- [ ] Visual feedback when dragging modules

### Technical acceptance
- [ ] Vue data binding is correct
- [ ] The event handler function is bound correctly
- [ ] API call path is correct
- [ ] Error handling improved
- [ ] The style is consistent with the system style
- [ ] Responsive layout is normal

### Quality acceptance
- [ ] Code comments are clear
- [ ] CSS style structure is reasonable
- [ ] No JavaScript errors
- [ ] No style conflicts
- [ ] Smooth interaction, no lag

---

## 🔍 Testing suggestions

### Visual test
1. Refresh the homepage and check whether the dialogue entrance is eye-catching.
2. Check whether the spacing and layout are reasonable
3. Test responsive layout (shrink browser window)
4. Check whether the animation effect is smooth

### Interactive testing
1. Enter text in the input box and check whether the button is enabled
2. Click the "Start Conversation" button and check whether it jumps
3. Press the Enter key and check if the dialog is triggered
4. Clear the input box and check whether the button is disabled
5. Test dragging the module to the drag and drop area

### Integration testing
1. Check whether the API call is successful
2. Verify that the created session ID is correct
3. Test error handling (network failure, etc.)

---

## 📝 Notes

⚠️ **Important**:
- Make sure the height and margin of the dialogue entry are eye-catching enough but do not affect the overall layout
- Gradient colors and animations must be coordinated with the overall style of the system
- The drag-and-drop area displays a prompt when there are no modules, and displays a list when there are modules.
- The disabled status of the button should be clear to avoid empty submissions
- **Only use existing components of the system**: Element UI, Font Awesome, Vue.js
- **No new dependencies**: All functions are implemented using native JavaScript and Vue

⚠️ **User ID acquisition**:
- Need to obtain the current user ID based on the actual authentication system
- Can be obtained from JWT Token, Cookie or Store
- Make sure the user IDs on the front and back ends are consistent

⚠️ **Drag and drop function note**:
- Module cards need to set the `draggable="true"` attribute
- Drag and drop data is transferred in JSON format
- The drag and drop area must correctly handle the `dragover` and `drop` events

---

## 🔗 Related tasks
- Previous step: [Step 02: Service layer implementation](./step-02-service-layer.md)
- Next step: [Step 04: Chat task page](./step-04-chat-page.md)
- Associated documents: [scratchpad.md](../scratchpad.md)

---

## 📊 Progress Tracking

**Task breakdown**:
- [ ] **[TASK-09]** Modify Index.cshtml to add dialogue entry (0.8h)
- [ ] **[TASK-10]** Modify Index.js to add dialogue entry interaction (0.5h)
- [ ] **[TASK-11]** Modify Index.cshtml.cs to add backend logic (0.4h)
- [ ] **[TASK-12]** Add responsive styles and animations (0.3h)

**Estimated total time**: 2 hours

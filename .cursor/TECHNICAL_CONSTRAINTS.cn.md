# 管理后台 AI 对话功能 - 技术规范说明

## 📋 技术约束总结

### 1. DTO 继承规范 ✅

**正确做法**：
```csharp
// ✅ 使用 DtoBase<int> 自动提供 Id 属性
public class AdminChatSessionDto : DtoBase<int>
{
    // 无需手动定义 Id 属性
    // Id、Flag、AddTime、LastUpdateTime、TenantId 等由基类自动提供
    
    public string Title { get; set; }
    public int UserId { get; set; }
    // ... 其他业务属性
}
```

**错误做法**：
```csharp
// ❌ 不要继承 DtoBase 然后手动添加 Id
public class AdminChatSessionDto : DtoBase
{
    public int Id { get; set; } // 不需要！
}
```

**实体转 DTO 时的注意事项**：
```csharp
public AdminChatSessionDto(AdminChatSession entity)
{
    if (entity == null) return;

    // 基类属性（由 DtoBase<int> 提供）
    Id = entity.Id;                      // ✅ 必须复制
    AddTime = entity.AddTime;            // ✅ 必须复制
    LastUpdateTime = entity.LastUpdateTime; // ✅ 必须复制
    TenantId = entity.TenantId;          // ✅ 必须复制
    Flag = entity.Flag;                  // ✅ 必须复制
    
    // 业务属性
    Title = entity.Title;
    UserId = entity.UserId;
    // ...
}
```

### 2. 前端技术栈约束 ✅

**系统已有（可以使用）**：
- ✅ Vue.js 2.x - `~/lib/vue/vue.js`
- ✅ Element UI 2.13.2 - `~/lib/element-ui_2.13.2/`
- ✅ axios - `~/lib/axios/axios.min.js`（通过 service 封装）
- ✅ Font Awesome - `~/lib/font-awesome/`
- ✅ Vuex - `~/lib/vuex.js`
- ✅ Echarts - `~/lib/echarts/` (如需图表)

**禁止引入（不要使用）**：
- ❌ lodash / underscore（工具函数自己实现）
- ❌ marked.js / markdown-it（Markdown 解析，使用简单正则）
- ❌ moment.js / dayjs（时间格式化自己实现）
- ❌ vue-virtual-scroller（虚拟滚动库）
- ❌ jQuery（系统使用 Vue.js）
- ❌ 任何 CDN 远程资源
- ❌ 任何需要 npm install 的新包

**如何实现常见功能**：

1. **防抖（Debounce）**：
```javascript
// ✅ 自己实现简单的防抖
methods: {
  debounce(func, wait) {
    let timeout;
    return function(...args) {
      clearTimeout(timeout);
      timeout = setTimeout(() => func.apply(this, args), wait);
    };
  }
}
```

2. **时间格式化**：
```javascript
// ✅ 使用原生 JavaScript
formatTime(timeString) {
  const date = new Date(timeString);
  const now = new Date();
  const diff = now - date;
  
  if (diff < 60000) return '刚刚';
  if (diff < 3600000) return Math.floor(diff / 60000) + ' 分钟前';
  return date.toLocaleString('zh-CN');
}
```

3. **Markdown 简单解析**：
```javascript
// ✅ 使用正则表达式
formatMessageContent(content) {
  return content
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/\n/g, '<br/>')
    .replace(/`([^`]+)`/g, '<code>$1</code>')
    .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
    .replace(/\*([^*]+)\*/g, '<em>$1</em>');
}
```

### 3. 后端技术规范 ✅

**继承关系**：
```csharp
// ✅ 实体
public class AdminChatSession : EntityBase<int> { }

// ✅ DTO
public class AdminChatSessionDto : DtoBase<int> { }

// ✅ Service
public class AdminChatSessionService : ServiceBase<AdminChatSession> { }

// ✅ AppService
public class AdminChatAppService : AppServiceBase { }
```

**API 接口规范**：
```csharp
// ✅ 使用 ApiBind 特性
[ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
public async Task<AppResponseBase<AdminChatSessionDto>> CreateSession([FromBody] CreateSessionRequest request)
{
    return await this.GetResponseAsync<AdminChatSessionDto>(
        async (response, logger) =>
        {
            // 业务逻辑
        });
}
```

### 4. 资源文件管理 ✅

**正确做法**：
```html
<!-- ✅ 使用本地文件 -->
<script src="~/lib/vue/vue.js"></script>
<script src="~/lib/element-ui_2.13.2/element.js"></script>
<link href="~/lib/element-ui_2.13.2/element.css" rel="stylesheet" />
```

**错误做法**：
```html
<!-- ❌ 不要使用 CDN -->
<script src="https://cdn.jsdelivr.net/npm/vue@2.x/dist/vue.js"></script>
<script src="https://unpkg.com/element-ui@2.x/lib/index.js"></script>

<!-- ❌ 不要引入新库 -->
<script src="~/lib/marked/marked.min.js"></script> <!-- 这个库不存在 -->
```

---

## 📦 已修正的文件清单

### ✅ 已更新的 steps 文件

1. **step-01-data-models.md**
   - ✅ 所有 DTO 改为继承 `DtoBase<int>`
   - ✅ 移除手动定义的 Id 属性
   - ✅ 实体转 DTO 时添加所有基类属性的复制
   - ✅ 更新关键技术点说明
   - ✅ 更新验收标准

2. **step-03-homepage-ui.md**
   - ✅ 强调只使用系统现有组件
   - ✅ 不引入新依赖的说明

3. **step-04-chat-page.md**
   - ✅ 移除 marked.js 库的建议
   - ✅ 改用正则表达式进行简单格式化
   - ✅ 添加 XSS 防护（HTML 转义）
   - ✅ 强调只使用系统现有组件

4. **step-05-drag-drop.md**
   - ✅ 强调使用原生 Drag & Drop API
   - ✅ 不引入 SortableJS 等拖拽库

5. **step-06-testing.md**
   - ✅ 移除 vue-virtual-scroller 的建议
   - ✅ 移除 lodash 防抖的建议
   - ✅ 改用自实现的简单工具函数
   - ✅ 添加资源文件检查项

6. **scratchpad.md**
   - ✅ 添加技术约束说明
   - ✅ 更新数据库设计说明（包含 DTO 规范）
   - ✅ 添加技术选型说明章节
   - ✅ 列出系统已有和禁止引入的库

---

## 🎯 关键修正点总结

### 修正 1: DTO 继承 `DtoBase<int>` ✅

**变更内容**：
- `AdminChatSessionDto : DtoBase<int>` （原：`DtoBase`）
- `AdminChatMessageDto : DtoBase<int>` （原：`DtoBase`）
- `AdminChatSessionModuleDto : DtoBase<int>` （原：`DtoBase`）

**影响范围**：
- step-01-data-models.md（3 个 DTO 定义）
- 所有实体转 DTO 的构造函数要复制基类属性

**优势**：
- 🎯 自动提供 Id 属性，代码更简洁
- 🎯 类型安全，编译时检查
- 🎯 符合框架规范

### 修正 2: 不引入新依赖 ✅

**变更内容**：
- 移除 marked.js 的建议 → 使用正则表达式
- 移除 lodash 的建议 → 自实现防抖函数
- 移除 vue-virtual-scroller 的建议 → 使用简单分页
- 强调使用系统现有组件

**技术替代方案**：
| 原计划 | 修正后 | 位置 |
|--------|--------|------|
| marked.js | 正则表达式简单格式化 | step-04 |
| lodash.debounce | 自实现防抖函数 | step-06 |
| vue-virtual-scroller | Element UI 原生滚动 | step-06 |
| moment.js | 原生 Date API | step-04 |

**优势**：
- 🎯 无需下载新依赖
- 🎯 减少项目体积
- 🎯 避免版本冲突
- 🎯 符合系统管理规范

---

## ✅ 验证清单

### DTO 定义检查
- [x] AdminChatSessionDto 继承 `DtoBase<int>` ✅
- [x] AdminChatMessageDto 继承 `DtoBase<int>` ✅
- [x] AdminChatSessionModuleDto 继承 `DtoBase<int>` ✅
- [x] 实体转 DTO 构造函数复制所有基类属性 ✅
- [x] 无手动定义的 Id 属性 ✅

### 依赖引入检查
- [x] 未引入 marked.js ✅
- [x] 未引入 lodash ✅
- [x] 未引入 moment.js ✅
- [x] 未引入 vue-virtual-scroller ✅
- [x] 未引入任何 CDN 远程资源 ✅
- [x] 只使用系统现有的 Vue.js、Element UI、axios、Font Awesome ✅

### 文档更新检查
- [x] step-01: DTO 定义已修正 ✅
- [x] step-03: 技术约束已添加 ✅
- [x] step-04: 移除 marked.js，使用正则 ✅
- [x] step-05: 强调使用原生 API ✅
- [x] step-06: 移除第三方库建议 ✅
- [x] scratchpad.md: 添加技术选型说明 ✅

---

## 📝 执行者使用指南

### 开发时的检查清单

**每创建一个 DTO 时**：
1. ✅ 继承 `DtoBase<int>` 吗？
2. ✅ 没有手动定义 Id 属性吗？
3. ✅ 实体转换构造函数复制了所有基类属性吗？

**每引入一个功能时**：
1. ✅ 检查系统是否已有类似组件？
2. ✅ 能用现有组件实现吗？
3. ✅ 必须引入新库吗？（99% 情况下答案是：不需要）
4. ✅ 如果必须引入，是否下载到本地？

**每写一段 JavaScript 时**：
1. ✅ 使用了 CDN 链接吗？（禁止）
2. ✅ 引入了新的 .js 文件吗？（需要确认是否已存在）
3. ✅ 工具函数能自己实现吗？（优先自己实现）

---

## 🎉 修正完成

所有 steps 文件已更新，确保：
1. ✅ 所有 DTO 继承 `DtoBase<int>`
2. ✅ 不引入任何新的第三方库
3. ✅ 只使用系统现有组件
4. ✅ 技术约束清晰明确

现在规划文档已完全符合项目技术规范，可以开始执行实现了！

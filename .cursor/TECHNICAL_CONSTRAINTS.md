#Manage background AI dialogue function - technical specifications

## 📋 Summary of technical constraints

### 1. DTO inheritance specification ✅

**Correct approach**:
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

**Wrong Practice**:
```csharp
// ❌ 不要继承 DtoBase 然后手动添加 Id
public class AdminChatSessionDto : DtoBase
{
    public int Id { get; set; } // 不需要！
}
```

**Things to note when converting entities to DTO**:
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

### 2. Front-end technology stack constraints ✅

**The system already exists (can be used)**:
- ✅ Vue.js 2.x - `~/lib/vue/vue.js`
- ✅ Element UI 2.13.2 - `~/lib/element-ui_2.13.2/`
- ✅ axios - `~/lib/axios/axios.min.js`(encapsulated by service)
- ✅ Font Awesome - `~/lib/font-awesome/`
- ✅ Vuex - `~/lib/vuex.js`
- ✅ Echarts - `~/lib/echarts/`(charts required)

**Not allowed to be introduced (do not use)**:
- ❌ lodash/underscore (the tool function is implemented by itself)
- ❌ marked.js/markdown-it (Markdown parsing, using simple regular expressions)
- ❌ moment.js / dayjs (time formatting implemented by yourself)
- ❌ vue-virtual-scroller (virtual scrolling library)
- ❌ jQuery (system uses Vue.js)
- ❌ Any CDN remote resource
- ❌ Any new package that requires npm install

**How ​​to implement common functions**:

1. **Debounce**:
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

2. **Time formatting**:
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

3. **Markdown simple analysis**:
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

### 3. Backend technical specifications ✅

**Inheritance relationship**:
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

**API interface specification**:
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

### 4. Resource file management ✅

**Correct approach**:
```html
<!-- ✅ 使用本地文件 -->
<script src="~/lib/vue/vue.js"></script>
<script src="~/lib/element-ui_2.13.2/element.js"></script>
<link href="~/lib/element-ui_2.13.2/element.css" rel="stylesheet" />
```

**Wrong Practice**:
```html
<!-- ❌ 不要使用 CDN -->
<script src="https://cdn.jsdelivr.net/npm/vue@2.x/dist/vue.js"></script>
<script src="https://unpkg.com/element-ui@2.x/lib/index.js"></script>

<!-- ❌ 不要引入新库 -->
<script src="~/lib/marked/marked.min.js"></script> <!-- 这个库不存在 -->
```

---

## 📦 Fixed file list

### ✅ Updated steps file

1. **step-01-data-models.md**
- ✅ All DTOs are changed to inheritance`DtoBase<int>`
- ✅ Remove manually defined Id attribute
- ✅ Add copy of all base class attributes when converting entities to DTO
- ✅ Updated description of key technical points
- ✅ Updated acceptance criteria

2. **step-03-homepage-ui.md**
- ✅ Emphasis on using only existing components of the system
- ✅ Instructions for not introducing new dependencies

3. **step-04-chat-page.md**
- ✅ Recommendation to remove marked.js library
- ✅ Use regular expressions for simple formatting instead
- ✅ Added XSS protection (HTML escaping)
- ✅ Emphasis on using only existing components of the system

4. **step-05-drag-drop.md**
- ✅ Emphasis on using native Drag & Drop API
- ✅ Does not introduce drag and drop libraries such as SortableJS

5. **step-06-testing.md**
- ✅ Removal of vue-virtual-scroller suggestions
- ✅ Recommendations for removing lodash anti-shake
- ✅ Switch to self-implemented simple utility functions
- ✅ Added resource file check items

6. **scratchpad.md**
- ✅Add technical constraints description
- ✅ Updated database design instructions (including DTO specifications)
- ✅ Added technical selection instructions section
- ✅ List existing and prohibited libraries in the system

---

## 🎯 Summary of key correction points

### Fix 1: DTO inheritance`DtoBase<int>` ✅

**Changes**:
- `AdminChatSessionDto : DtoBase<int>`(Original:`DtoBase`）
- `AdminChatMessageDto : DtoBase<int>`(Original:`DtoBase`）
- `AdminChatSessionModuleDto : DtoBase<int>`(Original:`DtoBase`）

**Scope of Impact**:
- step-01-data-models.md (3 DTO definitions)
- The constructor of all entities converted to DTO must copy the base class attributes

**Advantages**:
- 🎯 Automatically provide the Id attribute, making the code more concise
- 🎯 Type safety, compile-time checking
- 🎯 Comply with framework specifications

### Correction 2: No new dependencies are introduced ✅

**Changes**:
- Removal of marked.js suggestion → use regular expressions
- Recommendation to remove lodash → self-implemented anti-shake function
- Recommendation to remove vue-virtual-scroller → Use simple paging
- Emphasis on using existing components of the system

**Technical Alternatives**:
| original plan | After correction | Location |
|--------|--------|------|
| marked.js | Regular expression simple formatting | step-04 |
| lodash.debounce | Self-implemented anti-shake function | step-06 |
| vue-virtual-scroller | Element UI native scrolling | step-06 |
| moment.js | Native Date API | step-04 |

**Advantages**:
- 🎯 No need to download new dependencies
- 🎯 Reduce project size
- 🎯 Avoid version conflicts
- 🎯 Comply with system management specifications

---

## ✅ Verification Checklist

### DTO definition check
- [x] AdminChatSessionDto inheritance`DtoBase<int>` ✅
- [x] AdminChatMessageDto inheritance`DtoBase<int>` ✅
- [x] AdminChatSessionModuleDto inheritance`DtoBase<int>` ✅
- [x] Entity to DTO constructor copies all base class attributes ✅
- [x] No manually defined Id attribute ✅

### Dependency introduction check
- [x] marked.js is not introduced ✅
- [x] lodash not introduced ✅
- [x] moment.js is not introduced ✅
- [x] vue-virtual-scroller is not introduced ✅
- [x] No CDN remote resources are introduced ✅
- [x] Only use the system’s existing Vue.js, Element UI, axios, and Font Awesome ✅

### Document update check
- [x] step-01: DTO definition has been corrected ✅
- [x] step-03: Technical constraints have been added ✅
- [x] step-04: Remove marked.js and use regular expressions ✅
- [x] step-05: Emphasis on using native API ✅
- [x] step-06: Remove third-party library suggestions ✅
- [x] scratchpad.md: Add technical selection instructions ✅

---

## 📝 Executor User Guide

### Checklist during development

**Every time a DTO is created**:
1. ✅ Inheritance`DtoBase<int>`?
2. ✅ Not manually defining the Id attribute?
3. ✅ Does the entity conversion constructor copy all base class properties?

**Whenever a feature is introduced**:
1. ✅ Check whether the system already has similar components?
2. ✅ Can it be implemented using existing components?
3. ✅ Do I need to introduce new libraries? (99% of the time the answer is: no)
4. ✅ If it must be imported, should it be downloaded locally?

**Every time you write a piece of JavaScript**:
1. ✅ Are you using a CDN link? (prohibit)
2. ✅ Are new .js files introduced? (need to confirm whether it already exists)
3. ✅ Can the tool function be implemented by myself? (Priority to implement it yourself)

---

## 🎉 Correction completed

All steps files have been updated to ensure:
1. ✅ All DTO inheritance`DtoBase<int>`
2. ✅ Does not introduce any new third-party libraries
3. ✅ Only use existing components of the system
4. ✅ Technical constraints are clear and clear

Now that the planning document fully complies with the project technical specifications, implementation can begin!

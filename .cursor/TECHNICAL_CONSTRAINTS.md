# Admin AI Chat Feature - Technical Specification

## Technical Constraints Summary

### 1. DTO Inheritance Rules ✅

**Correct:**
```csharp
// ✅ DtoBase<int> provides the Id property automatically.
public class AdminChatSessionDto : DtoBase<int>
{
    // No manual Id property is required.
    // Id, Flag, AddTime, LastUpdateTime, TenantId, and related fields come from the base class.
    
    public string Title { get; set; }
    public int UserId { get; set; }
    // ... other business properties
}
```

**Incorrect:**
```csharp
// ❌ Do not inherit DtoBase and then add Id manually.
public class AdminChatSessionDto : DtoBase
{
    public int Id { get; set; } // Not needed.
}
```

**When converting an entity to a DTO:**
```csharp
public AdminChatSessionDto(AdminChatSession entity)
{
    if (entity == null) return;

    // Base-class properties supplied by DtoBase<int>.
    Id = entity.Id;                      // ✅ Must be copied.
    AddTime = entity.AddTime;            // ✅ Must be copied.
    LastUpdateTime = entity.LastUpdateTime; // ✅ Must be copied.
    TenantId = entity.TenantId;          // ✅ Must be copied.
    Flag = entity.Flag;                  // ✅ Must be copied.
    
    // Business properties.
    Title = entity.Title;
    UserId = entity.UserId;
    // ...
}
```

### 2. Frontend Stack Constraints ✅

**Existing system libraries (allowed):**
- ✅ Vue.js 2.x - `~/lib/vue/vue.js`
- ✅ Element UI 2.13.2 - `~/lib/element-ui_2.13.2/`
- ✅ axios - `~/lib/axios/axios.min.js` (wrapped by the service helper)
- ✅ Font Awesome - `~/lib/font-awesome/`
- ✅ Vuex - `~/lib/vuex.js`
- ✅ Echarts - `~/lib/echarts/` (when charts are needed)

**Prohibited dependencies:**
- ❌ lodash / underscore (implement utility functions locally)
- ❌ marked.js / markdown-it (use simple regular expressions for Markdown parsing)
- ❌ moment.js / dayjs (use native date formatting)
- ❌ vue-virtual-scroller (virtual scrolling library)
- ❌ jQuery (the system uses Vue.js)
- ❌ Any remote CDN resource
- ❌ Any new package that requires `npm install`

**Common implementation patterns:**

1. **Debounce:**
```javascript
// ✅ Implement a small debounce helper locally.
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

2. **Time formatting:**
```javascript
// ✅ Use native JavaScript.
formatTime(timeString) {
  const date = new Date(timeString);
  const now = new Date();
  const diff = now - date;
  
  if (diff < 60000) return 'just now';
  if (diff < 3600000) return Math.floor(diff / 60000) + ' minutes ago';
  return date.toLocaleString('en-US');
}
```

3. **Simple Markdown parsing:**
```javascript
// ✅ Use regular expressions.
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

### 3. Backend Technical Rules ✅

**Inheritance relationships:**
```csharp
// ✅ Entity
public class AdminChatSession : EntityBase<int> { }

// ✅ DTO
public class AdminChatSessionDto : DtoBase<int> { }

// ✅ Service
public class AdminChatSessionService : ServiceBase<AdminChatSession> { }

// ✅ AppService
public class AdminChatAppService : AppServiceBase { }
```

**API contract rules:**
```csharp
// ✅ Use the ApiBind attribute.
[ApiBind(ApiRequestMethod = ApiRequestMethod.Post)]
public async Task<AppResponseBase<AdminChatSessionDto>> CreateSession([FromBody] CreateSessionRequest request)
{
    return await this.GetResponseAsync<AdminChatSessionDto>(
        async (response, logger) =>
        {
            // Business logic.
        });
}
```

### 4. Resource File Management ✅

**Correct:**
```html
<!-- ✅ Use local files -->
<script src="~/lib/vue/vue.js"></script>
<script src="~/lib/element-ui_2.13.2/element.js"></script>
<link href="~/lib/element-ui_2.13.2/element.css" rel="stylesheet" />
```

**Incorrect:**
```html
<!-- ❌ Do not use a CDN. -->
<script src="https://cdn.jsdelivr.net/npm/vue@2.x/dist/vue.js"></script>
<script src="https://unpkg.com/element-ui@2.x/lib/index.js"></script>

<!-- ❌ Do not introduce a new library. -->
<script src="~/lib/marked/marked.min.js"></script> <!-- This library does not exist. -->
```

---

## Corrected File Checklist

### ✅ Updated step files

1. **step-01-data-models.md**
   - ✅ All DTOs now inherit `DtoBase<int>`.
   - ✅ Manual Id properties removed.
   - ✅ Entity-to-DTO constructors now copy all base-class properties.
   - ✅ Key technical notes updated.
   - ✅ Acceptance criteria updated.

2. **step-03-homepage-ui.md**
   - ✅ Emphasizes using only existing system components.
   - ✅ Documents that no new dependencies should be introduced.

3. **step-04-chat-page.md**
   - ✅ Removes the marked.js recommendation.
   - ✅ Uses regular expressions for simple formatting.
   - ✅ Adds XSS protection through HTML escaping.
   - ✅ Emphasizes using only existing system components.

4. **step-05-drag-drop.md**
   - ✅ Emphasizes the native Drag & Drop API.
   - ✅ Does not introduce SortableJS or another drag-and-drop library.

5. **step-06-testing.md**
   - ✅ Removes the vue-virtual-scroller recommendation.
   - ✅ Removes the lodash debounce recommendation.
   - ✅ Uses simple locally implemented utility functions.
   - ✅ Adds resource-file checks.

6. **scratchpad.md**
   - ✅ Adds the technical constraints.
   - ✅ Updates the database design notes, including DTO rules.
   - ✅ Adds a technology-selection section.
   - ✅ Lists existing and prohibited libraries.

---

## Key Corrections Summary

### Correction 1: DTO Inheritance with `DtoBase<int>` ✅

**Changes:**
- `AdminChatSessionDto : DtoBase<int>` (previously `DtoBase`)
- `AdminChatMessageDto : DtoBase<int>` (previously `DtoBase`)
- `AdminChatSessionModuleDto : DtoBase<int>` (previously `DtoBase`)

**Scope:**
- step-01-data-models.md (three DTO definitions)
- Every entity-to-DTO constructor must copy base-class properties.

**Benefits:**
- 🎯 Id is provided automatically, keeping the code concise.
- 🎯 Type safety is checked at compile time.
- 🎯 The implementation follows framework conventions.

### Correction 2: Do Not Introduce New Dependencies ✅

**Changes:**
- Remove the marked.js recommendation → use regular expressions.
- Remove the lodash recommendation → implement debounce locally.
- Remove the vue-virtual-scroller recommendation → use simple pagination.
- Emphasize existing system components.

**Technical Alternatives:**
| Original plan | Replacement | Location |
|--------|--------|------|
| marked.js | Simple formatting with regular expressions | step-04 |
| lodash.debounce | Locally implemented debounce | step-06 |
| vue-virtual-scroller | Native Element UI scrolling | step-06 |
| moment.js | Native Date API | step-04 |

**Benefits:**
- 🎯 No new dependencies need to be downloaded.
- 🎯 Smaller application footprint.
- 🎯 Fewer version conflicts.
- 🎯 Consistent with system management rules.

---

## Verification Checklist

### DTO Definition Checks
- [x] AdminChatSessionDto inherits `DtoBase<int>` ✅
- [x] AdminChatMessageDto inherits `DtoBase<int>` ✅
- [x] AdminChatSessionModuleDto inherits `DtoBase<int>` ✅
- [x] Entity-to-DTO constructors copy all base-class properties ✅
- [x] No manually defined Id properties ✅

### Dependency Checks
- [x] marked.js is not introduced ✅
- [x] lodash is not introduced ✅
- [x] moment.js is not introduced ✅
- [x] vue-virtual-scroller is not introduced ✅
- [x] No remote CDN resources are introduced ✅
- [x] Only the existing Vue.js, Element UI, axios, and Font Awesome components are used ✅

### Documentation Checks
- [x] step-01: DTO definitions corrected ✅
- [x] step-03: Technical constraints added ✅
- [x] step-04: marked.js removed; regular expressions used ✅
- [x] step-05: Native APIs emphasized ✅
- [x] step-06: Third-party library recommendations removed ✅
- [x] scratchpad.md: Technology-selection notes added ✅

---

## Executor Guide

### Development Checklist

**For every new DTO:**
1. ✅ Does it inherit `DtoBase<int>`?
2. ✅ Does it avoid a manual Id property?
3. ✅ Does the entity conversion constructor copy all base-class properties?

**For every new feature:**
1. ✅ Does the system already have a similar component?
2. ✅ Can the feature be implemented with existing components?
3. ✅ Is a new library truly required? (In 99% of cases, the answer is no.)
4. ✅ If it is required, is it downloaded locally?

**For every JavaScript change:**
1. ✅ Does it use a CDN link? (Prohibited.)
2. ✅ Does it introduce a new `.js` file? (Confirm whether it already exists.)
3. ✅ Can the utility function be implemented locally? (Prefer a local implementation.)

---

## Corrections Complete

All step files have been updated to ensure:
1. ✅ All DTOs inherit `DtoBase<int>`.
2. ✅ No new third-party libraries are introduced.
3. ✅ Only existing system components are used.
4. ✅ Technical constraints are clear.

The planning documents now comply with the project technical rules and are ready for implementation.

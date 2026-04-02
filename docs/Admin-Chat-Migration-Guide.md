# Admin Console AI Chat Feature - Migration Guide

## 📋 Execution Checklist

### ✅ Completed Work

1. **Data model creation**
   - ✅ 3 entity classes (AdminChatSession, AdminChatMessage, AdminChatSessionModule)
   - ✅ 3 DTO classes (inherit from DtoBase<int>)
   - ✅ 3 Repository interfaces and implementations

2. **Service layer implementation**
   - ✅ 3 Domain Service classes
   - ✅ 1 AppService (9 API endpoints)
   - ✅ All services registered in Register.cs

3. **Frontend page development**
   - ✅ Home page AI chat entry (Index.cshtml + Index.js)
   - ✅ Chat task page (Chat.cshtml + Chat.js + Chat.css)
   - ✅ Module drag-and-drop feature

4. **Code quality**
   - ✅ Build succeeded (0 errors)
   - ✅ Application starts normally
   - ✅ All dependencies registered correctly

---

## 🚀 Pending Actions (Run after restart)

### Step 1: Create database migration

```bash
cd /Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources/tools/NcfSimulatedSite/Senparc.Web

# Create migration
dotnet ef migrations add AddAdminChatTables \
  --project ../Senparc.Areas.Admin/Senparc.Areas.Admin.csproj \
  --context AdminSenparcEntities
```

**Expected result**: A new migration file is generated in the Senparc.Areas.Admin/Migrations/ folder.

---

### Step 2: Verify migration file

Open the generated migration file (for example, 20260325_AddAdminChatTables.cs) and confirm it contains the following 3 tables:

1. **ADMIN_AdminChatSession**
   - Fields: Id, Title, UserId, Status, LastMessageTime, AddTime, LastUpdateTime, TenantId, Flag

2. **ADMIN_AdminChatMessage**
   - Fields: Id, SessionId, RoleType, Content, Sequence, UserFeedback, ModelIdentifier, AddTime, LastUpdateTime, TenantId, Flag

3. **ADMIN_AdminChatSessionModule**
   - Fields: Id, SessionId, XncfModuleUid, ModuleName, ModuleVersion, AddedTime, AddTime, LastUpdateTime, TenantId, Flag

---

### Step 3: Apply migration

```bash
cd /Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources/tools/NcfSimulatedSite/Senparc.Web

# Apply migration to database
dotnet ef database update \
  --project ../Senparc.Areas.Admin/Senparc.Areas.Admin.csproj \
  --context AdminSenparcEntities
```

**Expected result**: Three new tables are created in the database and the command shows a Done message.

---

### Step 4: Validate database tables

Use a database tool (such as Navicat or DBeaver) and check:

```sql
-- Verify table existence
SELECT * FROM ADMIN_AdminChatSession LIMIT 0;
SELECT * FROM ADMIN_AdminChatMessage LIMIT 0;
SELECT * FROM ADMIN_AdminChatSessionModule LIMIT 0;

-- Inspect table schema
DESCRIBE ADMIN_AdminChatSession;
DESCRIBE ADMIN_AdminChatMessage;
DESCRIBE ADMIN_AdminChatSessionModule;
```

---

### Step 5: Start application

```bash
cd /Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources/tools/NcfSimulatedSite/Senparc.Web

# Start app
dotnet run
```

**Expected result**:
- Application starts successfully
- Now listening on http://localhost:5000 is displayed
- No database-related errors

---

### Step 6: Functional testing

#### 6.1 Home page test

1. Open http://localhost:5000/Admin
2. Sign in to admin console
3. Verify home page update:
   - ✅ Top statistics area is displayed correctly
   - ✅ AI chat entry panel is visible (prominent with margin)
   - ✅ Start Chat button is clickable
   - ✅ Module cards are retained

#### 6.2 Module drag-and-drop test

1. Drag any module card on the home page
2. Drop it above the AI chat entry panel
3. Verify behavior:
   - ✅ Entry panel highlights while dragging
   - ✅ After drop, module name appears below the panel
   - ✅ Selected module can be removed by clicking the x button

#### 6.3 Chat page test

1. Enter text in the entry area and click Start Chat
2. Navigate to /Admin/AdminChat/Chat
3. Verify page layout:
   - ✅ Left session list is visible
   - ✅ Right chat window is visible
   - ✅ Initial message is sent
   - ✅ AI response is shown (placeholder)

#### 6.4 Chat interaction test

1. Enter a message in the right chat window and press Enter
2. Verify behavior:
   - ✅ User message appears on the right (blue avatar)
   - ✅ AI message appears on the left (bot avatar)
   - ✅ Message timestamp is correct
   - ✅ Scroll moves to latest message

3. Test thumbs up/down:
   - ✅ Click 👍 or 👎 on an AI message
   - ✅ State toggle works correctly (highlight/unhighlight)

4. Test session switching:
   - ✅ Click different sessions on the left
   - ✅ Right-side content switches correctly
   - ✅ Message history loads correctly

5. Test creating new chat:
   - ✅ Click New Chat button
   - ✅ New session is created and switched to

---

## ⚠️ Known limitations and planned extensions

### Current limitations

1. **AI reply is placeholder text**
   - AdminChatAppService.GenerateAIResponseAsync() currently returns fixed text
   - **Reason**: Real AI model integration is not configured yet
   - **Extension direction**: Integrate AIKernel and call configured model

2. **Selected module context not passed to AI yet**
   - Selected module data is persisted, but not used during AI generation yet
   - **Extension direction**: Read linked modules in GenerateAIResponseAsync and inject into prompt

3. **Session management is simplified**
   - Archiving, deleting, and renaming sessions are not supported yet
   - **Extension direction**: Add more actions in session list (archive, delete, export)

### Future roadmap

1. **File upload support**
   - Upload documents and images into chat
   - Store files using FileManager module

2. **Voice chat support**
   - Integrate speech recognition and TTS
   - Support voice messages

3. **Conversation export**
   - Export chat as Markdown or PDF
   - Share conversation links

4. **Collaborative chat**
   - Multiple admins in one session
   - Real-time message push (WebSocket)

5. **Smart suggestions and slash commands**
   - Suggest actions based on conversation history
   - Support slash commands such as /help and /clear

---

## 📊 Feature checklist summary

| Feature Module | Status | Description |
|---------|------|------|
| Home page statistics retained | ✅ Completed | Original features are fully preserved |
| AI chat entry panel | ✅ Completed | Prominent design with input and drag support |
| Module drag-and-drop | ✅ Completed | HTML5 Drag API, no external library |
| Chat task page | ✅ Completed | Two-pane modern layout |
| Session history management | ✅ Completed | List, switch, and create session |
| Message send/receive | ✅ Completed | User and AI messages separated |
| Message feedback | ✅ Completed | Thumbs up/down |
| Module context association | ✅ Completed | Saved to database |
| API completeness | ✅ Completed | 9 endpoints, full CRUD |
| Build passed | ✅ Completed | 0 errors |
| Real AI response | ⏳ Planned | AI model integration required |

---

## 🎯 Technical highlights

1. **Zero new dependencies**: Fully based on existing Vue.js, Element UI, and axios
2. **DDD architecture**: Strict Domain Entity, Service, AppService layering
3. **Repository pattern**: Repository interface and implementation for each entity
4. **DTO standardization**: All DTOs inherit from DtoBase<int>
5. **Drag interaction**: Native HTML5 Drag API, no third-party library
6. **Modern UI**: Consistent system style, responsive design, smooth transitions
7. **API security**: All endpoints protected by [BackendJwtAuthorize]

---

**Document Created**: 2026-03-25  
**Estimated Execution Time**: 10-15 minutes  
**Difficulty**: ⭐⭐ (Medium)

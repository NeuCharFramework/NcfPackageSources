[中文版](Admin-Chat-Migration-Guide.cn.md)

#Manage background AI dialogue function - Migration Guide

## 📋 Execution Checklist

### ✅ Completed work

1. **Data model creation**
   - ✅ 3 entity classes (AdminChatSession, AdminChatMessage, AdminChatSessionModule)
   - ✅ 3 DTO classes (inherit DtoBase<int>)
   - ✅ 3 Repository interfaces and implementation classes

2. **Service layer implementation**
   - ✅ 3 Domain Service classes
   - ✅ 1 AppService (9 API interfaces)
   - ✅ All services have been registered in Register.cs

3. **Front-end page development**
   - ✅ Home page AI dialogue entrance (Index.cshtml + Index.js)
   - ✅ Conversation task page (Chat.cshtml + Chat.js + Chat.css)
   - ✅ Module drag and drop function

4. **Code Quality**
   - ✅ Compiled successfully (0 errors)
   - ✅ The application can be launched normally
   - ✅ All dependencies have been registered correctly

---

## 🚀 Operation to be performed (operation after waking up)

### Step 1: Create database migration```bash
cd /Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources/tools/NcfSimulatedSite/Senparc.Web

# 创建 Migration
dotnet ef migrations add AddAdminChatTables \
  --project ../Senparc.Areas.Admin/Senparc.Areas.Admin.csproj \
  --context AdminSenparcEntities
```**Expected results**: Generate new migration files in the `Senparc.Areas.Admin/Migrations/` folder.

---

### Step 2: Check the Migration file

Open the generated Migration file (such as `20260325_AddAdminChatTables.cs`) and confirm that it contains the following 3 tables:

1. **ADMIN_AdminChatSession**
   - Fields: Id, Title, UserId, Status, LastMessageTime, AddTime, LastUpdateTime, TenantId, Flag

2. **ADMIN_AdminChatMessage**
   - Fields: Id, SessionId, RoleType, Content, Sequence, UserFeedback, ModelIdentifier, AddTime, LastUpdateTime, TenantId, Flag

3. **ADMIN_AdminChatSessionModule**
   - Fields: Id, SessionId, XncfModuleUid, ModuleName, ModuleVersion, AddedTime, AddTime, LastUpdateTime, TenantId, Flag

---

### Step 3: Execute Migration```bash
cd /Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources/tools/NcfSimulatedSite/Senparc.Web

# 应用 Migration 到数据库
dotnet ef database update \
  --project ../Senparc.Areas.Admin/Senparc.Areas.Admin.csproj \
  --context AdminSenparcEntities
```**Expected results**: 3 new tables are created in the database and the "Done." message is displayed.

---

### Step 4: Validate database tables

Use database management tools (such as Navicat, DBeaver) to connect to the database and check:```sql
-- 检查表是否存在
SELECT * FROM ADMIN_AdminChatSession LIMIT 0;
SELECT * FROM ADMIN_AdminChatMessage LIMIT 0;
SELECT * FROM ADMIN_AdminChatSessionModule LIMIT 0;

-- 查看表结构
DESCRIBE ADMIN_AdminChatSession;
DESCRIBE ADMIN_AdminChatMessage;
DESCRIBE ADMIN_AdminChatSessionModule;
```---

### Step 5: Launch the application```bash
cd /Volumes/DevelopAndData/SenparcProjects/NeuCharFramework/NcfPackageSources/tools/NcfSimulatedSite/Senparc.Web

# 启动应用
dotnet run
```**Expected results**:
- Application started successfully
- Display "Now listening on: http://localhost:5000"
- No database related errors

---

### Step 6: Functional Testing

#### 6.1 Home Page Test

1. Visit `http://localhost:5000/Admin`
2. Log in to the management background
3. Check the effect of homepage revision:
   - ✅ The top statistics area is displayed normally
   - ✅ AI dialogue entrance prompt box display (eye-catching, with margin)
   - ✅ "Start Conversation" button is clickable
   - ✅ Module cards are retained

#### 6.2 Module drag and drop test

1. On the homepage, drag any module card
2. Drag it to the top of the AI dialogue entrance prompt box
3. Check the effect:
   - ✅ The prompt box is highlighted/discolored when dragging
   - ✅ After release, the module name is displayed below the prompt box
   - ✅ Selected modules can be deleted (click the x button)

#### 6.3 Dialogue page test

1. Enter text in the conversation entry and click "Start Conversation"
2. Jump to the chat page `/Admin/AdminChat/Chat`
3. Check the page layout:
   - ✅ Session list display on the left
   - ✅ The dialogue window on the right is displayed
   - ✅ Initial message sent
   - ✅ AI reply display (placeholder text)

#### 6.4 Dialogue interaction test

1. Enter a message in the dialog window on the right and press Enter to send.
2. Check the effect:
   - ✅ User messages are displayed on the right (blue avatar)
   - ✅ AI replies are shown on the left (bot avatar)
   - ✅ Message timestamps are correct
   - ✅Scroll to the latest news

3. Test the like/dislike function:
   - ✅ Click on the 👍 or 👎 icon of AI Message
   - ✅ Status switching is normal (highlight/cancel highlight)

4. Test session switching:
   - ✅ Click on different conversations on the left
   - ✅ The content on the right is switched correctly
   - ✅ Message history is loaded correctly

5. Test the new conversation:
   - ✅ Click the "New Conversation" button
   - ✅ Create a new conversation and switch to a new conversation

---

## ⚠️ Known limitations and functions to be expanded

### Current Limitations

1. **AI replies as placeholder text**
   - `AdminChatAppService.GenerateAIResponseAsync()` method returns fixed text
   - **Reason**: Need to configure a real AI model interface
   - **Extension direction**: Integrate the AIKernel module and call the configured AI model

2. **Module context has not been passed to AI yet**
   - The selected module information will be saved to the database, but it will not be used in AI generation yet.
   - **Extension direction**: Read related modules in `GenerateAIResponseAsync` and add prompt

3. **Simplified session management function**
   - Session archiving, deletion, renaming and other operations are not currently supported.
   - **Extension direction**: Add more operation buttons (archive, delete, export, etc.) in the conversation list

### Future expansion directions

1. **File upload support**
   - Support uploading documents and pictures to conversations
   - Use FileManager module to store files

2. **Voice dialogue support**
   - Integrated speech recognition and TTS
   - Support voice messages

3. **Conversation export function**
   - Export conversations to Markdown/PDF
   - Share conversation link

4. **Multi-person collaborative dialogue**
   - Support multiple administrators talking in the same session
   - Real-time message push (WebSocket)

5. **Smart suggestions and shortcut commands**
   - Intelligent suggestions based on conversation history
   - Support `/` shortcut commands (such as `/help`, `/clear`, etc.)

---

## 📊 Function list summary

| Function module | Status | Description |
|---------|------|------|
| Home page statistics area retained | ✅ Completed | Original functions fully retained |
| AI dialogue entrance prompt box | ✅ Completed | Eye-catching design, supports input and drag and drop |
| Module drag and drop function | ✅ Completed | HTML5 Drag API, no library required |
| Dialogue task page | ✅ Completed | Left and right layout, modern design |
| Session history management | ✅ Completed | List display, switch, create new |
| Message sending and receiving | ✅ Completed | User/AI message distinction |
| Message feedback function | ✅ Completed | Like/dislike |
| Module context association | ✅ Completed | Save to database |
| API interface complete | ✅ Completed | 9 interfaces, CRUD complete |
| Compiled | ✅ Completed | 0 errors |
| AI real reply | ⏳ To be expanded | Need to integrate AI model |

---

## 🎯Technical Highlights

1. **Zero new dependencies**: completely use the existing systemVue.js, Element UI, axios, no new libraries
2. **DDD architecture**: Strictly follow the Domain entity, Service, AppService layering
3. **Repository pattern**: Create corresponding Repository interface and implementation for each entity
4. **DTO standardization**: All DTOs inherit `DtoBase<int>` and unify base class attribute management
5. **Drag interaction**: Native HTML5 Drag API, no need for third-party libraries
6. **Modern UI**: In line with system style, responsive design, smooth animation
7. **API Security**: Use `[BackendJwtAuthorize]` to protect all interfaces

---

**Document creation date**: 2026-03-25
**Estimated execution time**: 10-15 minutes
**Difficulty Level**: ⭐⭐ (Medium)

# Step 01: Data Model Design and Implementation

## Task Overview
Create the three core data entities and their DTOs required by AdminChat: chat sessions, chat messages, and session-module associations.

## Goals
- ✅ Create the AdminChatSession entity (chat session table).
- ✅ Create the AdminChatMessage entity (chat message table).
- ✅ Create the AdminChatSessionModule entity (session-module association table).
- ✅ Create a corresponding DTO for each entity.
- ✅ Update the DbContext and Register configuration.

## Files Involved

### New Files
1. `tools/NcfWebAdminBaseline/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatSession.cs`
2. `tools/NcfWebAdminBaseline/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatMessage.cs`
3. `tools/NcfWebAdminBaseline/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatSessionModule.cs`
4. `tools/NcfWebAdminBaseline/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatSessionDto.cs`
5. `tools/NcfWebAdminBaseline/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatMessageDto.cs`
6. `tools/NcfWebAdminBaseline/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatSessionModuleDto.cs`

### Modified Files
7. `tools/NcfWebAdminBaseline/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminSenparcEntities.cs`

## Implementation Steps

### 1. Create the AdminChatSession Entity

**File path:** `tools/NcfWebAdminBaseline/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatSession.cs`

**Complete code example:**

```csharp
using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    /// <summary>
    /// AdminChatSession: Admin chat session.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminChatSession))] // The prefix is required to prevent system-wide collisions.
    [Serializable]
    public class AdminChatSession : EntityBase<int>
    {
        /// <summary>
        /// Session title (automatically extracted from the first message; up to 150 characters).
        /// </summary>
        [Required, MaxLength(150)]
        public string Title { get; private set; }

        /// <summary>
        /// User ID (foreign key to AdminUserInfo).
        /// </summary>
        [Required]
        public int UserId { get; private set; }

        /// <summary>
        /// Session status.
        /// </summary>
        [Required]
        public ChatSessionStatus Status { get; private set; }

        /// <summary>
        /// Time of the last message.
        /// </summary>
        public DateTime LastMessageTime { get; private set; }

        /// <summary>
        /// Private constructor for EF Core.
        /// </summary>
        private AdminChatSession() { }

        /// <summary>
        /// Create a new chat session.
        /// </summary>
        /// <param name="title">Session title.</param>
        /// <param name="userId">User ID.</param>
        public AdminChatSession(string title, int userId)
        {
            Title = title?.Length > 150 ? title.Substring(0, 150) : title ?? "New conversation";
            UserId = userId;
            Status = ChatSessionStatus.Active;
            LastMessageTime = DateTime.Now;
        }

        /// <summary>
        /// Update the session title.
        /// </summary>
        public void UpdateTitle(string title)
        {
            if (!string.IsNullOrEmpty(title))
            {
                Title = title.Length > 150 ? title.Substring(0, 150) : title;
                base.SetUpdateTime();
            }
        }

        /// <summary>
        /// Update the last-message time.
        /// </summary>
        public void UpdateLastMessageTime()
        {
            LastMessageTime = DateTime.Now;
            base.SetUpdateTime();
        }

        /// <summary>
        /// Archive the session.
        /// </summary>
        public void Archive()
        {
            Status = ChatSessionStatus.Archived;
            base.SetUpdateTime();
        }

        /// <summary>
        /// Delete the session (soft delete by changing its status).
        /// </summary>
        public void Delete()
        {
            Status = ChatSessionStatus.Deleted;
            base.SetUpdateTime();
        }

        /// <summary>
        /// Restore the session.
        /// </summary>
        public void Restore()
        {
            Status = ChatSessionStatus.Active;
            base.SetUpdateTime();
        }
    }

    /// <summary>
    /// Chat session status.
    /// </summary>
    public enum ChatSessionStatus
    {
        /// <summary>
        /// Active.
        /// </summary>
        Active = 0,
        /// <summary>
        /// Archived.
        /// </summary>
        Archived = 1,
        /// <summary>
        /// Deleted.
        /// </summary>
        Deleted = 2
    }
}
```

**Key technical points:**
- Inherit from `EntityBase<int>` to obtain Id, TenantId, AddTime, LastUpdateTime, Flag, and related fields automatically.
- Use the `[Table]` attribute to specify the table name; the `Register.DATABASE_PREFIX` prefix is required.
- Use `private set` for all setters and modify values through public methods, following domain-driven design principles.
- Use a private constructor for EF Core and a public constructor for business creation.

---

### 2. Create the AdminChatMessage Entity

**File path:** `tools/NcfWebAdminBaseline/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatMessage.cs`

**Complete code example:**

```csharp
using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    /// <summary>
    /// AdminChatMessage: Admin chat message.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminChatMessage))] // The prefix is required to prevent system-wide collisions.
    [Serializable]
    public class AdminChatMessage : EntityBase<int>
    {
        /// <summary>
        /// Session ID (foreign key).
        /// </summary>
        [Required]
        public int SessionId { get; private set; }

        /// <summary>
        /// Related session entity (navigation property).
        /// </summary>
        [ForeignKey(nameof(SessionId))]
        public AdminChatSession Session { get; private set; }

        /// <summary>
        /// Role type: User, Assistant (AI assistant), or System (system message).
        /// </summary>
        [Required]
        public ChatRoleType RoleType { get; private set; }

        /// <summary>
        /// Message content.
        /// </summary>
        [Required]
        public string Content { get; private set; }

        /// <summary>
        /// Message sequence number (one-based order within the session).
        /// </summary>
        [Required]
        public int Sequence { get; private set; }

        /// <summary>
        /// User feedback: Like (true), Unlike (false), or no feedback (null).
        /// </summary>
        public bool? UserFeedback { get; private set; }

        /// <summary>
        /// AI model identifier, such as gpt-4 or claude-3.5.
        /// </summary>
        [MaxLength(100)]
        public string ModelIdentifier { get; private set; }

        /// <summary>
        /// Message creation time (stored separately for convenient querying).
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; private set; }

        /// <summary>
        /// Private constructor for EF Core.
        /// </summary>
        private AdminChatMessage() { }

        /// <summary>
        /// Create a new chat message.
        /// </summary>
        /// <param name="sessionId">Session ID.</param>
        /// <param name="roleType">Role type.</param>
        /// <param name="content">Message content.</param>
        /// <param name="sequence">Message sequence number.</param>
        /// <param name="modelIdentifier">Optional AI model identifier.</param>
        public AdminChatMessage(int sessionId, ChatRoleType roleType, string content, int sequence, string modelIdentifier = null)
        {
            SessionId = sessionId;
            RoleType = roleType;
            Content = content ?? string.Empty;
            Sequence = sequence;
            ModelIdentifier = modelIdentifier;
            CreatedTime = DateTime.Now;
        }

        /// <summary>
        /// Set user feedback.
        /// </summary>
        /// <param name="isLike">true means like; false means dislike.</param>
        public void SetUserFeedback(bool isLike)
        {
            UserFeedback = isLike;
            base.SetUpdateTime();
        }

        /// <summary>
        /// Clear user feedback.
        /// </summary>
        public void ClearUserFeedback()
        {
            UserFeedback = null;
            base.SetUpdateTime();
        }
    }

    /// <summary>
    /// Chat role type.
    /// </summary>
    public enum ChatRoleType
    {
        /// <summary>
        /// User.
        /// </summary>
        User = 0,
        /// <summary>
        /// AI assistant.
        /// </summary>
        Assistant = 1,
        /// <summary>
        /// System message.
        /// </summary>
        System = 2
    }
}
```

**Key technical points:**
- Inherit from `DtoBase<int>` to obtain the integer Id property automatically.
- Also inherit base fields such as Flag, AddTime, LastUpdateTime, and TenantId.
- Use nullable `bool?` for the three states: like, dislike, and no feedback.
- Keep `CreatedTime` as a separate field for querying and sorting.
- Use `Sequence` to preserve message order.
- Provide a simplified `ChatMessageInputDto` for frontend submission; it does not need to inherit DtoBase.

---

### 3. Create the AdminChatSessionModule Entity

**File path:** `tools/NcfWebAdminBaseline/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatSessionModule.cs`

**Complete code example:**

```csharp
using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    /// <summary>
    /// AdminChatSessionModule: association between a chat session and an XNCF module.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminChatSessionModule))] // The prefix is required to prevent system-wide collisions.
    [Serializable]
    public class AdminChatSessionModule : EntityBase<int>
    {
        /// <summary>
        /// Session ID (foreign key).
        /// </summary>
        [Required]
        public int SessionId { get; private set; }

        /// <summary>
        /// Related session entity (navigation property).
        /// </summary>
        [ForeignKey(nameof(SessionId))]
        public AdminChatSession Session { get; private set; }

        /// <summary>
        /// XNCF module UID (Guid format).
        /// </summary>
        [Required, MaxLength(36)]
        public string XncfModuleUid { get; private set; }

        /// <summary>
        /// Module name (denormalized for display).
        /// </summary>
        [Required, MaxLength(100)]
        public string ModuleName { get; private set; }

        /// <summary>
        /// Module version.
        /// </summary>
        [MaxLength(50)]
        public string ModuleVersion { get; private set; }

        /// <summary>
        /// Time added to the session.
        /// </summary>
        [Required]
        public DateTime AddedTime { get; private set; }

        /// <summary>
        /// Private constructor for EF Core.
        /// </summary>
        private AdminChatSessionModule() { }

        /// <summary>
        /// Create a new session-module association.
        /// </summary>
        /// <param name="sessionId">Session ID.</param>
        /// <param name="xncfModuleUid">XNCF module UID.</param>
        /// <param name="moduleName">Module name.</param>
        /// <param name="moduleVersion">Module version.</param>
        public AdminChatSessionModule(int sessionId, string xncfModuleUid, string moduleName, string moduleVersion = null)
        {
            SessionId = sessionId;
            XncfModuleUid = xncfModuleUid ?? throw new ArgumentNullException(nameof(xncfModuleUid));
            ModuleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
            ModuleVersion = moduleVersion;
            AddedTime = DateTime.Now;
        }
    }
}
```

**Key technical points:**
- Inherit from `DtoBase<int>` to obtain the integer Id property automatically.
- The association table supports dragging a module into the chat dialog.
- Store ModuleName and ModuleVersion redundantly to avoid frequent JOIN queries.
- AddedTime records when the module was added for traceability.
- DTO constructors must copy all base-class properties.

---

### 4. Create AdminChatSessionDto

**File path:** `tools/NcfWebAdminBaseline/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatSessionDto.cs`

**Complete code example:**

```csharp
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    /// Data transfer object for AdminChatSession.
    /// </summary>
    [Serializable]
    public class AdminChatSessionDto : DtoBase<int>
    {
        public string Title { get; set; }
        
        public int UserId { get; set; }
        
        public ChatSessionStatus Status { get; set; }
        
        public DateTime LastMessageTime { get; set; }

        /// <summary>
        /// Related module list (optional; used for display).
        /// </summary>
        public List<AdminChatSessionModuleDto> Modules { get; set; }

        /// <summary>
        /// Last-message preview (optional; used in the session list).
        /// </summary>
        public string LastMessagePreview { get; set; }

        public AdminChatSessionDto() { }

        public AdminChatSessionDto(AdminChatSession entity)
        {
            if (entity == null) return;

            Id = entity.Id;
            Title = entity.Title;
            UserId = entity.UserId;
            Status = entity.Status;
            LastMessageTime = entity.LastMessageTime;
            AddTime = entity.AddTime;
            LastUpdateTime = entity.LastUpdateTime;
            TenantId = entity.TenantId;
            Flag = entity.Flag;
        }
    }
}
```

---

### 5. Create AdminChatMessageDto

**File path:** `tools/NcfWebAdminBaseline/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatMessageDto.cs`

**Complete code example:**

```csharp
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    /// Data transfer object for AdminChatMessage.
    /// </summary>
    [Serializable]
    public class AdminChatMessageDto : DtoBase<int>
    {
        public int SessionId { get; set; }
        
        public ChatRoleType RoleType { get; set; }
        
        public string Content { get; set; }
        
        public int Sequence { get; set; }
        
        public bool? UserFeedback { get; set; }
        
        public string ModelIdentifier { get; set; }
        
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Whether this message belongs to the current user (for frontend display).
        /// </summary>
        public bool IsCurrentUser { get; set; }

        public AdminChatMessageDto() { }

        public AdminChatMessageDto(AdminChatMessage entity)
        {
            if (entity == null) return;

            Id = entity.Id;
            SessionId = entity.SessionId;
            RoleType = entity.RoleType;
            Content = entity.Content;
            Sequence = entity.Sequence;
            UserFeedback = entity.UserFeedback;
            ModelIdentifier = entity.ModelIdentifier;
            CreatedTime = entity.CreatedTime;
            AddTime = entity.AddTime;
            LastUpdateTime = entity.LastUpdateTime;
            TenantId = entity.TenantId;
            Flag = entity.Flag;
        }
    }

    /// <summary>
    /// Simplified chat message DTO for frontend message submission.
    /// </summary>
    [Serializable]
    public class ChatMessageInputDto
    {
        /// <summary>
        /// Session ID.
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// Message content.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Related module UID list (optional; used as context).
        /// </summary>
        public List<string> ModuleUids { get; set; }
    }
}
```

---

### 6. Create AdminChatSessionModuleDto

**File path:** `tools/NcfWebAdminBaseline/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatSessionModuleDto.cs`

**Complete code example:**

```csharp
using Senparc.Ncf.Core.Models;
using System;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    /// Data transfer object for AdminChatSessionModule.
    /// </summary>
    [Serializable]
    public class AdminChatSessionModuleDto : DtoBase<int>
    {
        public int SessionId { get; set; }
        
        public string XncfModuleUid { get; set; }
        
        public string ModuleName { get; set; }
        
        public string ModuleVersion { get; set; }
        
        public DateTime AddedTime { get; set; }

        public AdminChatSessionModuleDto() { }

        public AdminChatSessionModuleDto(AdminChatSessionModule entity)
        {
            if (entity == null) return;

            Id = entity.Id;
            SessionId = entity.SessionId;
            XncfModuleUid = entity.XncfModuleUid;
            ModuleName = entity.ModuleName;
            ModuleVersion = entity.ModuleVersion;
            AddedTime = entity.AddedTime;
            AddTime = entity.AddTime;
            LastUpdateTime = entity.LastUpdateTime;
            TenantId = entity.TenantId;
            Flag = entity.Flag;
        }
    }
}
```

---

### 7. Update AdminSenparcEntities

**File path:** `tools/NcfWebAdminBaseline/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminSenparcEntities.cs`

**Modification point:** Add the following before the `//DOT REMOVE OR MODIFY THIS LINE` line.

**Add:**

```csharp
/// <summary>
/// Admin chat sessions.
/// </summary>
public DbSet<AdminChatSession> AdminChatSessions { get; set; }

/// <summary>
/// Admin chat messages.
/// </summary>
public DbSet<AdminChatMessage> AdminChatMessages { get; set; }

/// <summary>
/// Admin chat session-module associations.
/// </summary>
public DbSet<AdminChatSessionModule> AdminChatSessionModules { get; set; }
```

**Complete file structure after the change:**

```csharp
public class AdminSenparcEntities : XncfDatabaseDbContext
{
    public AdminSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }

    #region System tables (do not modify without a special reason)

    /// <summary>
    /// System settings.
    /// </summary>
    public DbSet<AdminUserInfo> SystemConfigs { get; set; }

    /// <summary>
    /// Admin chat sessions.
    /// </summary>
    public DbSet<AdminChatSession> AdminChatSessions { get; set; }

    /// <summary>
    /// Admin chat messages.
    /// </summary>
    public DbSet<AdminChatMessage> AdminChatMessages { get; set; }

    /// <summary>
    /// Admin chat session-module associations.
    /// </summary>
    public DbSet<AdminChatSessionModule> AdminChatSessionModules { get; set; }

    //DOT REMOVE OR MODIFY THIS LINE - do not remove or modify this line - Entities Point

    #endregion
}
```

---

## Acceptance Criteria

### Functional Checks
- [ ] AdminChatSession contains all required fields.
- [ ] AdminChatMessage contains all required fields and correct foreign-key relationships.
- [ ] AdminChatSessionModule contains all required fields and correct foreign-key relationships.
- [ ] Every DTO maps its entity correctly.
- [ ] AdminSenparcEntities contains all three DbSet properties.
- [ ] All files follow the namespace conventions.

### Technical Checks
- [ ] All entities inherit `EntityBase<int>`.
- [ ] All DTOs inherit `DtoBase<int>` (which provides Id automatically).
- [ ] Table names use `[Table]` with the required prefix.
- [ ] All required fields are marked `[Required]`.
- [ ] String fields have reasonable `MaxLength` values.
- [ ] Foreign-key relationships use `[ForeignKey]`.
- [ ] Entities use private constructors and public business methods.
- [ ] DTO conversion constructors copy all base-class properties.
- [ ] Enums are complete and documented.

### Quality Checks
- [ ] Code comments are clear and complete.
- [ ] The code follows NCF conventions.
- [ ] There are no compilation errors.
- [ ] There are no linting warnings.

---

## Test Recommendations

### Build Tests
1. Build the project and confirm there are no syntax errors.
2. Check that namespaces are correct.
3. Confirm that all referenced base classes and attributes exist.

### Code Review
1. Check that field definitions are complete.
2. Verify foreign-key relationships.
3. Confirm that enum values are appropriate.
4. Check that comments are clear.

---

## Notes

⚠️ **Important:**
- Use `Register.DATABASE_PREFIX` as the table prefix to avoid collisions with other modules.
- All entity properties must use `private set` and be changed through public methods.
- **DTOs must inherit `DtoBase<int>`**, which provides the integer Id automatically.
- DTO conversion constructors must copy all base properties (AddTime, LastUpdateTime, TenantId, Flag).
- DTO constructors must handle null input.
- Configure foreign-key relationships correctly so EF Core can generate migrations.
- Add DbSets to AdminSenparcEntities before the `//DOT REMOVE` comment.

⚠️ **Special attention:**
- The user will execute the migration manually; provide correct model definitions only.
- Do not create migration files at this stage.
- Ensure every entity is registered in the DbContext.

---

## Related Tasks
- Previous step: None (this is the first task).
- Next step: [Step 02: Service Layer Implementation](./step-02-service-layer.md)
- Related document: [scratchpad.md](../scratchpad.md)

---

## Progress Tracking

**Task breakdown:**
- [ ] **[TASK-01]** Create the AdminChatSession entity and DTO (0.5h)
- [ ] **[TASK-02]** Create the AdminChatMessage entity and DTO (0.5h)
- [ ] **[TASK-03]** Create the AdminChatSessionModule entity and DTO (0.5h)
- [ ] **[TASK-04]** Update AdminSenparcEntities (0.5h)

**Estimated total time**: 2 hours

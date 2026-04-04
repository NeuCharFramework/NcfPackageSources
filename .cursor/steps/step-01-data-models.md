# Step 01: Data model layer design and implementation

## 📋 Mission Overview
Create the three core data entities and their DTOs required for AdminChat functionality, including chat sessions, chat messages, and session-module association tables.

## 🎯 Goal
- ✅ Create AdminChatSession entity (chat session table)
- ✅ Create AdminChatMessage entity (chat message table)
- ✅ Create AdminChatSessionModule entity (session-module association table)
- ✅ Create corresponding DTO for each entity
- ✅ Update DbContext and Register configuration

## 📂Involved documents

### Create new file
1. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatSession.cs`
2. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatMessage.cs`
3. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatSessionModule.cs`
4. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatSessionDto.cs`
5. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatMessageDto.cs`
6. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatSessionModuleDto.cs`

### Modify files
7. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminSenparcEntities.cs`

## 🔧 Implementation steps

### 1. Create AdminChatSession entity

**File path**:`tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatSession.cs`

**Full code example**:

```csharp
using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    /// <summary>
    /// AdminChatSession：管理后台聊天会话
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminChatSession))] //必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class AdminChatSession : EntityBase<int>
    {
        /// <summary>
        /// 会话标题（从首条消息自动提取，最多150字符）
        /// </summary>
        [Required, MaxLength(150)]
        public string Title { get; private set; }

        /// <summary>
        /// 用户ID（外键到 AdminUserInfo）
        /// </summary>
        [Required]
        public int UserId { get; private set; }

        /// <summary>
        /// 会话状态
        /// </summary>
        [Required]
        public ChatSessionStatus Status { get; private set; }

        /// <summary>
        /// 最后一条消息时间
        /// </summary>
        public DateTime LastMessageTime { get; private set; }

        /// <summary>
        /// 私有构造函数（供 EF Core 使用）
        /// </summary>
        private AdminChatSession() { }

        /// <summary>
        /// 创建新的聊天会话
        /// </summary>
        /// <param name="title">会话标题</param>
        /// <param name="userId">用户ID</param>
        public AdminChatSession(string title, int userId)
        {
            Title = title?.Length > 150 ? title.Substring(0, 150) : title ?? "新对话";
            UserId = userId;
            Status = ChatSessionStatus.Active;
            LastMessageTime = DateTime.Now;
        }

        /// <summary>
        /// 更新会话标题
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
        /// 更新最后消息时间
        /// </summary>
        public void UpdateLastMessageTime()
        {
            LastMessageTime = DateTime.Now;
            base.SetUpdateTime();
        }

        /// <summary>
        /// 归档会话
        /// </summary>
        public void Archive()
        {
            Status = ChatSessionStatus.Archived;
            base.SetUpdateTime();
        }

        /// <summary>
        /// 删除会话（软删除，修改状态）
        /// </summary>
        public void Delete()
        {
            Status = ChatSessionStatus.Deleted;
            base.SetUpdateTime();
        }

        /// <summary>
        /// 恢复会话
        /// </summary>
        public void Restore()
        {
            Status = ChatSessionStatus.Active;
            base.SetUpdateTime();
        }
    }

    /// <summary>
    /// 聊天会话状态
    /// </summary>
    public enum ChatSessionStatus
    {
        /// <summary>
        /// 活跃中
        /// </summary>
        Active = 0,
        /// <summary>
        /// 已归档
        /// </summary>
        Archived = 1,
        /// <summary>
        /// 已删除
        /// </summary>
        Deleted = 2
    }
}
```

**Key technical points**:
- Inherited from`EntityBase<int>`, automatically obtain Id, TenantId, AddTime, LastUpdateTime, Flag and other fields
- use`[Table]`The attribute specifies the table name and must be prefixed`Register.DATABASE_PREFIX`
- All setters used`private set`, modified through public methods, in line with domain-driven design principles
- use`private`Constructors are used by EF Core and public constructors are used for business creation

---

### 2. Create AdminChatMessage entity

**File path**:`tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatMessage.cs`

**Full code example**:

```csharp
using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    /// <summary>
    /// AdminChatMessage：管理后台聊天消息
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminChatMessage))] //必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class AdminChatMessage : EntityBase<int>
    {
        /// <summary>
        /// 会话ID（外键）
        /// </summary>
        [Required]
        public int SessionId { get; private set; }

        /// <summary>
        /// 关联的会话实体（导航属性）
        /// </summary>
        [ForeignKey(nameof(SessionId))]
        public AdminChatSession Session { get; private set; }

        /// <summary>
        /// 角色类型：User（用户）、Assistant（AI助手）、System（系统消息）
        /// </summary>
        [Required]
        public ChatRoleType RoleType { get; private set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        [Required]
        public string Content { get; private set; }

        /// <summary>
        /// 消息序号（在同一会话中的顺序，从 1 开始）
        /// </summary>
        [Required]
        public int Sequence { get; private set; }

        /// <summary>
        /// 用户反馈：Like（true）、Unlike（false）、未反馈（null）
        /// </summary>
        public bool? UserFeedback { get; private set; }

        /// <summary>
        /// AI 模型标识（如：gpt-4、claude-3.5等）
        /// </summary>
        [MaxLength(100)]
        public string ModelIdentifier { get; private set; }

        /// <summary>
        /// 消息创建时间（独立字段，方便查询）
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; private set; }

        /// <summary>
        /// 私有构造函数（供 EF Core 使用）
        /// </summary>
        private AdminChatMessage() { }

        /// <summary>
        /// 创建新的聊天消息
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="roleType">角色类型</param>
        /// <param name="content">消息内容</param>
        /// <param name="sequence">消息序号</param>
        /// <param name="modelIdentifier">AI模型标识（可选）</param>
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
        /// 设置用户反馈
        /// </summary>
        /// <param name="isLike">true=喜欢，false=不喜欢</param>
        public void SetUserFeedback(bool isLike)
        {
            UserFeedback = isLike;
            base.SetUpdateTime();
        }

        /// <summary>
        /// 清除用户反馈
        /// </summary>
        public void ClearUserFeedback()
        {
            UserFeedback = null;
            base.SetUpdateTime();
        }
    }

    /// <summary>
    /// 聊天角色类型
    /// </summary>
    public enum ChatRoleType
    {
        /// <summary>
        /// 用户
        /// </summary>
        User = 0,
        /// <summary>
        /// AI 助手
        /// </summary>
        Assistant = 1,
        /// <summary>
        /// 系统消息
        /// </summary>
        System = 2
    }
}
```

**Key technical points**:
- Inherited from`DtoBase<int>`, automatically obtain the Id attribute of type int
- At the same time inherit basic fields such as Flag, AddTime, LastUpdateTime, TenantId, etc.
- Use nullable types`bool?`Indicates three states (like/dislike/no feedback)
- `CreatedTime`Independent fields facilitate querying and sorting
- `Sequence`Fields ensure message order
- Provides simplified`ChatMessageInputDto`Used for front-end submission (no need to inherit DtoBase)

---

### 3. Create AdminChatSessionModule entity

**File path**:`tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatSessionModule.cs`

**Full code example**:

```csharp
using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel
{
    /// <summary>
    /// AdminChatSessionModule：聊天会话与 XNCF 模块的关联表
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AdminChatSessionModule))] //必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class AdminChatSessionModule : EntityBase<int>
    {
        /// <summary>
        /// 会话ID（外键）
        /// </summary>
        [Required]
        public int SessionId { get; private set; }

        /// <summary>
        /// 关联的会话实体（导航属性）
        /// </summary>
        [ForeignKey(nameof(SessionId))]
        public AdminChatSession Session { get; private set; }

        /// <summary>
        /// XNCF 模块的 UID（Guid 格式）
        /// </summary>
        [Required, MaxLength(36)]
        public string XncfModuleUid { get; private set; }

        /// <summary>
        /// 模块名称（冗余字段，便于显示）
        /// </summary>
        [Required, MaxLength(100)]
        public string ModuleName { get; private set; }

        /// <summary>
        /// 模块版本号
        /// </summary>
        [MaxLength(50)]
        public string ModuleVersion { get; private set; }

        /// <summary>
        /// 添加到会话的时间
        /// </summary>
        [Required]
        public DateTime AddedTime { get; private set; }

        /// <summary>
        /// 私有构造函数（供 EF Core 使用）
        /// </summary>
        private AdminChatSessionModule() { }

        /// <summary>
        /// 创建新的会话-模块关联
        /// </summary>
        /// <param name="sessionId">会话ID</param>
        /// <param name="xncfModuleUid">XNCF模块UID</param>
        /// <param name="moduleName">模块名称</param>
        /// <param name="moduleVersion">模块版本</param>
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

**Key technical points**:
- Inherited from`DtoBase<int>`, automatically obtain the Id attribute of type int
- Association table is used to support the "drag and drop module to dialog box" function
- Redundant storage of ModuleName and ModuleVersion to avoid frequent JOIN queries
- AddedTime records the module addition time for easy traceability
- The DTO constructor copies all base class properties

---

### 4. Create AdminChatSessionDto

**File path**:`tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatSessionDto.cs`

**Full code example**:

```csharp
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    /// AdminChatSession 的数据传输对象
    /// </summary>
    [Serializable]
    public class AdminChatSessionDto : DtoBase<int>
    {
        public string Title { get; set; }
        
        public int UserId { get; set; }
        
        public ChatSessionStatus Status { get; set; }
        
        public DateTime LastMessageTime { get; set; }

        /// <summary>
        /// 关联的模块列表（可选，用于展示）
        /// </summary>
        public List<AdminChatSessionModuleDto> Modules { get; set; }

        /// <summary>
        /// 最后一条消息预览（可选，用于会话列表）
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

**File path**:`tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatMessageDto.cs`

**Full code example**:

```csharp
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    /// AdminChatMessage 的数据传输对象
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
        /// 是否为当前用户的消息（用于前端展示）
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
    /// 简化的聊天消息 DTO（用于前端发送消息）
    /// </summary>
    [Serializable]
    public class ChatMessageInputDto
    {
        /// <summary>
        /// 会话ID
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 关联的模块 UID 列表（可选，用于上下文）
        /// </summary>
        public List<string> ModuleUids { get; set; }
    }
}
```

---

### 6. Create AdminChatSessionModuleDto

**File path**:`tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatSessionModuleDto.cs`

**Full code example**:

```csharp
using Senparc.Ncf.Core.Models;
using System;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto
{
    /// <summary>
    /// AdminChatSessionModule 的数据传输对象
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

**File path**:`tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminSenparcEntities.cs`

**Modify location**: in`//DOT REMOVE OR MODIFY THIS LINE`Add before line

**Add content**:

```csharp
/// <summary>
/// 管理后台聊天会话
/// </summary>
public DbSet<AdminChatSession> AdminChatSessions { get; set; }

/// <summary>
/// 管理后台聊天消息
/// </summary>
public DbSet<AdminChatMessage> AdminChatMessages { get; set; }

/// <summary>
/// 管理后台聊天会话-模块关联
/// </summary>
public DbSet<AdminChatSessionModule> AdminChatSessionModules { get; set; }
```

**Modified complete file structure**:

```csharp
public class AdminSenparcEntities : XncfDatabaseDbContext
{
    public AdminSenparcEntities(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {
    }

    #region 系统表（无特殊情况不要修改）

    /// <summary>
    /// 系统设置
    /// </summary>
    public DbSet<AdminUserInfo> SystemConfigs { get; set; }

    /// <summary>
    /// 管理后台聊天会话
    /// </summary>
    public DbSet<AdminChatSession> AdminChatSessions { get; set; }

    /// <summary>
    /// 管理后台聊天消息
    /// </summary>
    public DbSet<AdminChatMessage> AdminChatMessages { get; set; }

    /// <summary>
    /// 管理后台聊天会话-模块关联
    /// </summary>
    public DbSet<AdminChatSessionModule> AdminChatSessionModules { get; set; }

    //DOT REMOVE OR MODIFY THIS LINE 请勿移除或修改本行 - Entities Point

    #endregion
}
```

---

## ✅ Acceptance Criteria

### Function acceptance
- [ ] AdminChatSession entity contains all required fields
- [ ] AdminChatMessage entity contains all required fields, foreign key relationships are correct
- [ ] AdminChatSessionModule entity contains all required fields, foreign key relationships are correct
- [ ] All DTO classes correctly map entities
- [ ] AdminSenparcEntities Three DbSet properties have been added
- [ ] All files follow namespace conventions

### Technical acceptance
- [ ] All entities inherit from`EntityBase<int>`
- [ ] All DTOs inherit from`DtoBase<int>`(Id attribute provided automatically)
- [ ] table name usage`[Table]`properties and add a prefix
- [ ] all required field markers`[Required]`
- [ ] String field settings are reasonable`MaxLength`
- [ ] Use of foreign key relationships`[ForeignKey]`characteristic
- [ ] Entities use private constructors and public business methods
- [ ] DTO's entity conversion constructor copies all base class properties
- [ ] The enumeration is fully defined and commented

### Quality acceptance
- [ ] Code comments are clear and complete
- [ ] Comply with the coding specifications of the NCF framework
- [ ] No compilation errors
- [ ] No linting warning

---

## 🔍 Testing suggestions

### Compile test
1. Compile the project to ensure there are no syntax errors
2. Check whether the namespace is correct
3. Confirm that all referenced base classes and attributes exist

### Code review
1. Check whether the field definition is complete
2. Verify whether the foreign key relationship is correct
3. Confirm whether the enumeration value is reasonable
4. Check whether the comments are clear

---

## 📝 Notes

⚠️ **Important**:
- required`Register.DATABASE_PREFIX`As a table name prefix to avoid conflicts with other modules
- All properties of the entity must be used`private set`, modified through public methods
- **DTO must be inherited`DtoBase<int>`**, this will automatically provide the Id attribute of type int, no need to manually define it
- The entity conversion constructor of DTO copies all base class properties (AddTime, LastUpdateTime, TenantId, Flag)
- The constructor of DTO should handle the null case
- Foreign key relationships must be set correctly to ensure that EF Core can correctly generate migration
- The DbSet in AdminSenparcEntities must be added at`//DOT REMOVE`Before commenting

⚠️ **Special Note**:
- Users will perform Migration manually, we only need to provide the correct Model definition
- Do not create migration files at this stage
- Make sure all entities are correctly added to the DbContext

---

## 🔗 Related tasks
- Previous step: None (this is the first task)
- Next step: [Step 02: Service layer implementation](./step-02-service-layer.md)
- Associated documents: [scratchpad.md](../scratchpad.md)

---

## 📊 Progress Tracking

**Task breakdown**:
- [ ] **[TASK-01]** Create AdminChatSession entity and DTO (0.5h)
- [ ] **[TASK-02]** Create AdminChatMessage entity and DTO (0.5h)
- [ ] **[TASK-03]** Create AdminChatSessionModule entity and DTO (0.5h)
- [ ] **[TASK-04]** Update AdminSenparcEntities (0.5h)

**Estimated total time**: 2 hours

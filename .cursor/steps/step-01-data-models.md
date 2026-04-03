# Step 01: 数据模型层设计与实现

## 📋 任务概述
创建 AdminChat 功能所需的三个核心数据实体及其 DTO，包括聊天会话、聊天消息和会话-模块关联表。

## 🎯 目标
- ✅ 创建 AdminChatSession 实体（聊天会话表）
- ✅ 创建 AdminChatMessage 实体（聊天消息表）
- ✅ 创建 AdminChatSessionModule 实体（会话-模块关联表）
- ✅ 为每个实体创建对应的 DTO
- ✅ 更新 DbContext 和 Register 配置

## 📂 涉及文件

### 新建文件
1. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatSession.cs`
2. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatMessage.cs`
3. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatSessionModule.cs`
4. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatSessionDto.cs`
5. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatMessageDto.cs`
6. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatSessionModuleDto.cs`

### 修改文件
7. `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminSenparcEntities.cs`

## 🔧 实现步骤

### 1. 创建 AdminChatSession 实体

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatSession.cs`

**完整代码示例**:

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

**关键技术点**：
- 继承自 `EntityBase<int>`，自动获得 Id、TenantId、AddTime、LastUpdateTime、Flag 等字段
- 使用 `[Table]` 特性指定表名，必须添加前缀 `Register.DATABASE_PREFIX`
- 所有 setter 使用 `private set`，通过公共方法修改，符合领域驱动设计原则
- 使用 `private` 构造函数供 EF Core 使用，公共构造函数用于业务创建

---

### 2. 创建 AdminChatMessage 实体

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatMessage.cs`

**完整代码示例**:

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

**关键技术点**：
- 继承自 `DtoBase<int>`，自动获得 int 类型的 Id 属性
- 同时继承 Flag、AddTime、LastUpdateTime、TenantId 等基础字段
- 使用可空类型 `bool?` 表示三态（喜欢/不喜欢/未反馈）
- `CreatedTime` 独立字段便于查询和排序
- `Sequence` 字段确保消息顺序
- 提供简化的 `ChatMessageInputDto` 用于前端提交（不需要继承 DtoBase）

---

### 3. 创建 AdminChatSessionModule 实体

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminChatSessionModule.cs`

**完整代码示例**:

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

**关键技术点**：
- 继承自 `DtoBase<int>`，自动获得 int 类型的 Id 属性
- 关联表用于支持"拖拽模块到对话框"功能
- 冗余存储 ModuleName 和 ModuleVersion，避免频繁 JOIN 查询
- AddedTime 记录模块添加时间，便于追溯
- DTO 的构造函数要复制所有基类属性

---

### 4. 创建 AdminChatSessionDto

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatSessionDto.cs`

**完整代码示例**:

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

### 5. 创建 AdminChatMessageDto

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatMessageDto.cs`

**完整代码示例**:

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

### 6. 创建 AdminChatSessionModuleDto

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/Dto/AdminChatSessionModuleDto.cs`

**完整代码示例**:

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

### 7. 更新 AdminSenparcEntities

**文件路径**: `tools/NcfSimulatedSite/Senparc.Areas.Admin/Domain/Models/DatabaseModel/AdminSenparcEntities.cs`

**修改位置**: 在 `//DOT REMOVE OR MODIFY THIS LINE` 行之前添加

**添加内容**:

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

**修改后的完整文件结构**:

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

## ✅ 验收标准

### 功能验收
- [ ] AdminChatSession 实体包含所有必需字段
- [ ] AdminChatMessage 实体包含所有必需字段，外键关系正确
- [ ] AdminChatSessionModule 实体包含所有必需字段，外键关系正确
- [ ] 所有 DTO 类能正确映射实体
- [ ] AdminSenparcEntities 已添加三个 DbSet 属性
- [ ] 所有文件遵循命名空间规范

### 技术验收
- [ ] 所有实体继承自 `EntityBase<int>`
- [ ] 所有 DTO 继承自 `DtoBase<int>`（自动提供 Id 属性）
- [ ] 表名使用 `[Table]` 特性并添加前缀
- [ ] 所有必需字段标记 `[Required]`
- [ ] 字符串字段设置合理的 `MaxLength`
- [ ] 外键关系使用 `[ForeignKey]` 特性
- [ ] 实体使用 private 构造函数和公共业务方法
- [ ] DTO 的实体转换构造函数要复制所有基类属性
- [ ] 枚举定义完整且有注释

### 质量验收
- [ ] 代码注释清晰完整
- [ ] 符合 NCF 框架的编码规范
- [ ] 无编译错误
- [ ] 无 linting 警告

---

## 🔍 测试建议

### 编译测试
1. 编译项目，确保无语法错误
2. 检查命名空间是否正确
3. 确认所有引用的基类和特性存在

### 代码审查
1. 检查字段定义是否完整
2. 验证外键关系是否正确
3. 确认枚举值是否合理
4. 检查注释是否清晰

---

## 📝 注意事项

⚠️ **重要**：
- 必须使用 `Register.DATABASE_PREFIX` 作为表名前缀，避免与其他模块冲突
- 实体的所有属性必须使用 `private set`，通过公共方法修改
- **DTO 必须继承 `DtoBase<int>`**，这样会自动提供 int 类型的 Id 属性，无需手动定义
- DTO 的实体转换构造函数要复制所有基类属性（AddTime、LastUpdateTime、TenantId、Flag）
- DTO 的构造函数要处理 null 情况
- 外键关系要正确设置，确保 EF Core 能正确生成 Migration
- AdminSenparcEntities 中的 DbSet 添加位置要在 `//DOT REMOVE` 注释之前

⚠️ **特别注意**：
- 用户将手动执行 Migration，我们只需提供正确的 Model 定义
- 不要在这个阶段创建 Migration 文件
- 确保所有实体都正确添加到 DbContext 中

---

## 🔗 相关任务
- 上一步：无（这是第一个任务）
- 下一步：[Step 02: 服务层实现](./step-02-service-layer.md)
- 关联文档：[scratchpad.md](../scratchpad.md)

---

## 📊 进度追踪

**任务拆解**：
- [ ] **[TASK-01]** 创建 AdminChatSession 实体和 DTO (0.5h)
- [ ] **[TASK-02]** 创建 AdminChatMessage 实体和 DTO (0.5h)
- [ ] **[TASK-03]** 创建 AdminChatSessionModule 实体和 DTO (0.5h)
- [ ] **[TASK-04]** 更新 AdminSenparcEntities (0.5h)

**预计总耗时**: 2 小时

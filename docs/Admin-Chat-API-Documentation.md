# AdminChat API 接口文档

## 📌 基础信息

**Base URL**: `/api/{XNCF_NAME}/AdminChatAppService/`  
**认证方式**: JWT Bearer Token（BackendJwtAuthorize）  
**内容类型**: `application/json`

---

## 📡 API 接口列表

### 会话侧栏与分类排序（2026-11-03）

- 侧栏顶部按钮可收起/展开整个侧栏，右边界可拖动调整宽度（215–420px）；聚焦边界后也可用左右方向键调整。Workspace 分类标题可折叠。布局偏好按账号保存在当前浏览器，不跨设备同步。
- 分类内会话通过拖动条目左侧手柄排序，也可聚焦手柄按 `Alt + ↑/↓`。不允许跨 Workspace 移动；“未使用 Workspace”分类同样支持排序。
- 排序保存在数据库，同一账号在其他浏览器/设备重新打开或刷新列表后读取。未手动排序时维持最近消息优先；已有手动顺序的分类中，新会话排在手动排序条目后面。排序不会改变消息时间。
- 本页发起的发送、继续执行、分支执行和审批请求，在原会话条目显示渐变状态条、动态音量柱及“进行中/正在停止”文字。收起分类或侧栏后仍保留活动圆点；系统开启减少动态效果时使用静态指示。此状态不代表跨设备任务监控。
- 任务运行时可切换会话查看历史；“思考中”、实时轨迹、生成内容和终止按钮仅属于发起任务的会话。切回原会话仍可查看实时进度；后台任务完成不会向正在查看的其他会话追加消息或自动跳转。当前页面仍保持一次执行一个任务的限制，不因切换会话启动并行请求。

**排序接口**：`POST /api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.ReorderSessionAsync`

使用现有管理员 Cookie/JWT 认证，用户与租户身份由服务端读取，请求不能指定用户：

```json
{ "sessionId": 123, "targetSessionId": 456, "insertAfter": false }
```

将 `sessionId` 放在同分类的 `targetSessionId` 前方，`insertAfter: true` 表示后方。两个会话都必须属于当前用户及租户且未删除/归档，否则明确返回错误，不保存部分顺序。成功响应的 `data` 是该分类完整会话 DTO 数组，包含持久化的 `id` / `sortOrder`。列表和详情 DTO 也包含可空的 `sortOrder`（空值表示尚未手动排序）。

部署前应用对应数据库的 `Add_AdminChat_Session_SortOrder` Migration，详见 [Migration 指南](./Admin-Chat-Migration-Guide.md)。

### 1. 创建会话

**接口**: `CreateSession`  
**方法**: POST  
**路径**: `/api/{XNCF_NAME}/AdminChatAppService/CreateSession`

**请求体**:
```json
{
  "InitialMessage": "你好，我想了解系统功能",
  "ModuleUids": ["module-uid-1", "module-uid-2"]
}
```

**响应**:
```json
{
  "success": true,
  "msg": "",
  "data": {
    "SessionId": 123,
    "Session": {
      "Id": 123,
      "Title": "你好，我想了解系统功能",
      "UserId": 1,
      "Status": 0,
      "LastMessageTime": "2026-03-25T22:30:00",
      "Messages": [...],
      "Modules": [...]
    }
  }
}
```

---

### 2. 获取会话列表

**接口**: `GetSessionList`  
**方法**: GET  
**路径**: `/api/{XNCF_NAME}/AdminChatAppService/GetSessionList?pageIndex=1&pageSize=20`

**查询参数**:
- `pageIndex` (int, 可选, 默认1): 页码
- `pageSize` (int, 可选, 默认20): 每页数量

**响应**:
```json
{
  "success": true,
  "msg": "",
  "data": {
    "Sessions": [
      {
        "Id": 123,
        "Title": "你好，我想了解系统功能",
        "UserId": 1,
        "Status": 0,
        "LastMessageTime": "2026-03-25T22:30:00"
      }
    ],
    "TotalCount": 10
  }
}
```

---

### 3. 获取会话详情

**接口**: `GetSessionDetail`  
**方法**: GET  
**路径**: `/api/{XNCF_NAME}/AdminChatAppService/GetSessionDetail?sessionId=123`

**查询参数**:
- `sessionId` (int, 必需): 会话ID

**响应**:
```json
{
  "success": true,
  "msg": "",
  "data": {
    "Session": {
      "Id": 123,
      "Title": "你好，我想了解系统功能",
      "Messages": [
        {
          "Id": 1,
          "SessionId": 123,
          "RoleType": 0,
          "Content": "你好，我想了解系统功能",
          "Sequence": 1,
          "UserFeedback": 0,
          "AddTime": "2026-03-25T22:30:00"
        },
        {
          "Id": 2,
          "SessionId": 123,
          "RoleType": 1,
          "Content": "您好！我是 AI 助手...",
          "Sequence": 2,
          "UserFeedback": 0,
          "AddTime": "2026-03-25T22:30:05"
        }
      ],
      "Modules": [
        {
          "Id": 1,
          "SessionId": 123,
          "XncfModuleUid": "module-uid-1",
          "ModuleName": "系统管理",
          "ModuleVersion": "1.0.0"
        }
      ]
    }
  }
}
```

---

### 4. 发送消息

**接口**: `SendMessage`  
**方法**: POST  
**路径**: `/api/{XNCF_NAME}/AdminChatAppService/SendMessage`

**请求体**:
```json
{
  "SessionId": 123,
  "Content": "请介绍一下系统核心功能"
}
```

**响应**:
```json
{
  "success": true,
  "msg": "",
  "data": {
    "UserMessage": {
      "Id": 3,
      "SessionId": 123,
      "RoleType": 0,
      "Content": "请介绍一下系统核心功能",
      "Sequence": 3
    },
    "AiMessage": {
      "Id": 4,
      "SessionId": 123,
      "RoleType": 1,
      "Content": "系统核心功能包括...",
      "Sequence": 4
    }
  }
}
```

---

### 5. 设置消息反馈

**接口**: `SetMessageFeedback`  
**方法**: POST  
**路径**: `/api/{XNCF_NAME}/AdminChatAppService/SetMessageFeedback`

**请求体**:
```json
{
  "MessageId": 4,
  "Feedback": 1
}
```

**参数说明**:
- `Feedback`: 0=None, 1=Like, 2=Dislike

**响应**:
```json
{
  "success": true,
  "msg": "",
  "data": {
    "Success": true
  }
}
```

---

### 6. 添加模块到会话

**接口**: `AddModuleToSession`  
**方法**: POST  
**路径**: `/api/{XNCF_NAME}/AdminChatAppService/AddModuleToSession`

**请求体**:
```json
{
  "SessionId": 123,
  "XncfModuleUid": "module-uid-3",
  "ModuleName": "用户管理",
  "ModuleVersion": "1.0.0"
}
```

**响应**:
```json
{
  "success": true,
  "msg": "",
  "data": {
    "Success": true,
    "Module": {
      "Id": 5,
      "SessionId": 123,
      "XncfModuleUid": "module-uid-3",
      "ModuleName": "用户管理"
    }
  }
}
```

---

### 7. 获取会话模块

**接口**: `GetSessionModules`  
**方法**: GET  
**路径**: `/api/{XNCF_NAME}/AdminChatAppService/GetSessionModules?sessionId=123`

**查询参数**:
- `sessionId` (int, 必需): 会话ID

**响应**:
```json
{
  "success": true,
  "msg": "",
  "data": {
    "Modules": [
      {
        "Id": 1,
        "XncfModuleUid": "module-uid-1",
        "ModuleName": "系统管理",
        "ModuleVersion": "1.0.0"
      }
    ]
  }
}
```

---

### 8. 删除会话

**接口**: `DeleteSession`  
**方法**: DELETE  
**路径**: `/api/{XNCF_NAME}/AdminChatAppService/DeleteSession?sessionId=123`

**查询参数**:
- `sessionId` (int, 必需): 会话ID

**响应**:
```json
{
  "success": true,
  "msg": "",
  "data": {
    "Success": true
  }
}
```

**说明**: 软删除，会话状态变为 Deleted（2），但数据不会真正删除。

---

### 9. 更新会话标题

**接口**: `UpdateSessionTitle`  
**方法**: POST  
**路径**: `/api/{XNCF_NAME}/AdminChatAppService/UpdateSessionTitle`

**请求体**:
```json
{
  "SessionId": 123,
  "NewTitle": "系统功能咨询"
}
```

**响应**:
```json
{
  "success": true,
  "msg": "",
  "data": {
    "Success": true
  }
}
```

---

## 📋 数据类型定义

### ChatSessionStatus (enum)
```csharp
Active = 0,    // 活跃
Archived = 1,  // 已归档
Deleted = 2    // 已删除
```

### ChatMessageRoleType (enum)
```csharp
User = 0,      // 用户消息
Assistant = 1, // AI 助手消息
System = 2     // 系统消息
```

### MessageFeedbackType (enum)
```csharp
None = 0,      // 无反馈
Like = 1,      // 点赞
Dislike = 2    // 点踩
```

---

## 🔒 权限验证

所有接口都受 `[BackendJwtAuthorize]` 保护，需要在请求头中携带有效的 JWT Token：

```http
Authorization: Bearer {your_jwt_token}
```

---

## 🧪 测试示例（使用 curl）

### 创建会话
```bash
curl -X POST http://localhost:5000/api/{XNCF_NAME}/AdminChatAppService/CreateSession \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "InitialMessage": "你好",
    "ModuleUids": []
  }'
```

### 发送消息
```bash
curl -X POST http://localhost:5000/api/{XNCF_NAME}/AdminChatAppService/SendMessage \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "SessionId": 123,
    "Content": "请介绍系统功能"
  }'
```

---

**文档版本**: v1.0  
**最后更新**: 2026-03-25  
**维护者**: NeuCharFramework Team

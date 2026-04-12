[English](Admin-Chat-API-Documentation.md)

# AdminChat API 接口文档

## 📌 基础信息

**Base URL**: `/api/{XNCF_NAME}/AdminChatAppService/`  
**认证方式**: JWT Bearer Token（BackendJwtAuthorize）  
**内容类型**: `application/json`

---

## 📡 API 接口列表

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
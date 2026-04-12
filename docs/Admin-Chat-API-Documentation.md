[中文版](Admin-Chat-API-Documentation.cn.md)

# AdminChat API interface documentation

## 📌 Basic information

**Base URL**: `/api/{XNCF_NAME}/AdminChatAppService/`
**Authentication method**: JWT Bearer Token (BackendJwtAuthorize)
**Content type**: `application/json`

---

## 📡 API interface list

### 1. Create session

**Interface**: `CreateSession`
**Method**: POST
**Path**: `/api/{XNCF_NAME}/AdminChatAppService/CreateSession`

**Request body**:```json
{
  "InitialMessage": "你好，我想了解系统功能",
  "ModuleUids": ["module-uid-1", "module-uid-2"]
}
```**response**:```json
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
```---

### 2. Get session list

**Interface**: `GetSessionList`
**Method**: GET
**Path**: `/api/{XNCF_NAME}/AdminChatAppService/GetSessionList?pageIndex=1&pageSize=20`

**Query Parameters**:

- `pageIndex` (int, optional, default 1): page number
- `pageSize` (int, optional, default 20): Number of pages per page

**Response**:```json
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
```---

### 3. Get session details

**Interface**: `GetSessionDetail`
**Method**: GET
**Path**: `/api/{XNCF_NAME}/AdminChatAppService/GetSessionDetail?sessionId=123`

**Query Parameters**:

- `sessionId` (int, required): session ID

**Response**:```json
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
```---

### 4. Send message

**Interface**: `SendMessage`
**Method**: POST
**Path**: `/api/{XNCF_NAME}/AdminChatAppService/SendMessage`

**Request body**:```json
{
  "SessionId": 123,
  "Content": "请介绍一下系统核心功能"
}
```**response**:```json
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
```---

### 5. Set message feedback

**Interface**: `SetMessageFeedback`
**Method**: POST
**Path**: `/api/{XNCF_NAME}/AdminChatAppService/SetMessageFeedback`

**Request body**:```json
{
  "MessageId": 4,
  "Feedback": 1
}
```**Parameter Description**:

- `Feedback`: 0=None, 1=Like, 2=Dislike

**Response**:```json
{
  "success": true,
  "msg": "",
  "data": {
    "Success": true
  }
}
```---

### 6. Add module to session

**Interface**: `AddModuleToSession`
**Method**: POST
**Path**: `/api/{XNCF_NAME}/AdminChatAppService/AddModuleToSession`

**Request body**:```json
{
  "SessionId": 123,
  "XncfModuleUid": "module-uid-3",
  "ModuleName": "用户管理",
  "ModuleVersion": "1.0.0"
}
```**response**:```json
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
```---

### 7. Get the session module

**Interface**: `GetSessionModules`
**Method**: GET
**Path**: `/api/{XNCF_NAME}/AdminChatAppService/GetSessionModules?sessionId=123`

**Query Parameters**:

- `sessionId` (int, required): session ID

**Response**:```json
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
```---

### 8. Delete session

**Interface**: `DeleteSession`
**Method**: DELETE
**Path**: `/api/{XNCF_NAME}/AdminChatAppService/DeleteSession?sessionId=123`

**Query Parameters**:

- `sessionId` (int, required): session ID

**Response**:```json
{
  "success": true,
  "msg": "",
  "data": {
    "Success": true
  }
}
```**Description**: Soft deletion, the session status changes to Deleted (2), but the data will not actually be deleted.

---

### 9. Update session title

**Interface**: `UpdateSessionTitle`
**Method**: POST
**Path**: `/api/{XNCF_NAME}/AdminChatAppService/UpdateSessionTitle`

**Request body**:```json
{
  "SessionId": 123,
  "NewTitle": "系统功能咨询"
}
```**response**:```json
{
  "success": true,
  "msg": "",
  "data": {
    "Success": true
  }
}
```---

## 📋 Data type definition

### ChatSessionStatus (enum)```csharp
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
```---

## 🔒 Permission verification

All interfaces are protected by `[BackendJwtAuthorize]` and need to carry a valid JWT Token in the request header:```http
Authorization: Bearer {your_jwt_token}
```---

## 🧪 Test example (using curl)

### Create session```bash
curl -X POST http://localhost:5000/api/{XNCF_NAME}/AdminChatAppService/CreateSession \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "InitialMessage": "你好",
    "ModuleUids": []
  }'
```### Send message```bash
curl -X POST http://localhost:5000/api/{XNCF_NAME}/AdminChatAppService/SendMessage \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "SessionId": 123,
    "Content": "请介绍系统功能"
  }'
```---

**Document version**: v1.0
**Last updated**: 2026-03-25
**Maintainer**: NeuCharFramework Team

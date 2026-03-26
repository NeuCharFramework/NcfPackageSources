# Admin Chat 认证配置说明

## 认证方式兼容性

`AdminChatAppService` 现在支持两种认证方式，只要其中一种通过即可访问 API：

### 1. Cookie 认证（网页登录）
- **认证方案**: `NcfAdminAuthorizeScheme`
- **使用场景**: 通过管理后台登录页面（`/Admin/Login`）登录
- **适用于**: 浏览器网页访问

### 2. JWT 认证（API Token）
- **认证方案**: `Bearer_Backend`
- **使用场景**: API 客户端、移动端、第三方集成
- **适用于**: 需要 Token 认证的场景

## 实现细节

### AdminOrJwtAuthorizeAttribute
新创建的认证属性，位于 `tools/NcfSimulatedSite/Senparc.Areas.Admin/AdminOrJwtAuthorizeAttribute.cs`

```csharp
[AdminOrJwtAuthorize("AdminOnly")]
public class AdminChatAppService : LocalAppServiceBase
{
    // API 方法可以通过 Cookie 或 JWT 任一方式访问
}
```

### 工作原理
ASP.NET Core 的 `AuthorizeAttribute.AuthenticationSchemes` 属性支持多个认证方案（用逗号分隔）。当请求到达时：
1. 框架会依次尝试配置的认证方案
2. 只要**任何一个**认证方案成功，就允许访问
3. 如果**所有**认证方案都失败，才会返回 401 Unauthorized

## 使用示例

### Cookie 认证访问（当前场景）
```javascript
// 用户通过 Login.cshtml 登录后，浏览器会自动携带 Cookie
axios.post('/api/AdminChat/CreateSessionAsync', data)
  .then(response => {
    // Cookie 认证自动完成，无需额外操作
  });
```

### JWT 认证访问（未来扩展）
```javascript
// 客户端需要先获取 JWT Token，然后在请求头中携带
axios.post('/api/AdminChat/CreateSessionAsync', data, {
  headers: {
    'Authorization': 'Bearer ' + jwtToken
  }
});
```

## 其他 AppService 认证对比

| AppService | 认证属性 | 认证方式 |
|------------|---------|---------|
| `AdminChatAppService` | `[AdminOrJwtAuthorize]` | Cookie **或** JWT |
| `AdminUserInfoAppService` | `[BackendJwtAuthorize]` | 仅 JWT |
| `StatAppService` | `[AdminAuthorize]` | 仅 Cookie |
| `ModuleAppService` | `[BackendJwtAuthorize]` | 仅 JWT |

## 优势

1. **向后兼容**: 现有 Cookie 登录完全可用
2. **未来扩展**: 支持 API Token 方式，便于移动端或第三方集成
3. **灵活性高**: 不同客户端可以选择最适合的认证方式
4. **无需修改前端**: 当前基于 Cookie 的前端代码无需任何改动

## 测试建议

### Cookie 认证测试
1. 通过 `/Admin/Login` 登录管理后台
2. 在首页输入框发送消息
3. 应能正常跳转到聊天页面

### JWT 认证测试（可选）
1. 使用 Postman 或其他 API 工具
2. 在请求头添加 `Authorization: Bearer {token}`
3. 调用 `/api/AdminChat/CreateSessionAsync` 等接口
4. 应能正常获取响应

## 注意事项

- 如果未来需要更严格的权限控制，可以在方法级别添加额外的授权检查
- Policy "AdminOnly" 确保只有管理员角色可以访问这些 API
- 两种认证方式共享相同的授权策略和权限检查

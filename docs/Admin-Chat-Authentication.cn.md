[English](Admin-Chat-Authentication.md)

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

## 问题修复历史

### 问题1: "登录过期，即将跳转到登录页面"
**原因**: `AdminChatAppService` 使用了 `[BackendJwtAuthorize]`，但用户使用 Cookie 登录
**修复**: 改用 `[AdminOrJwtAuthorize]` 支持两种认证方式

### 问题2: "跳转到 /Admin/Forbidden"
**原因**: Chat 页面需要 "AdminOnly" Policy，但在 Register.cs 中未配置
**修复**: 在 Register.cs 中添加 `options.Conventions.AuthorizePage("/AdminChat/Chat", "AdminOnly");`

## 实现细节

### AdminOrJwtAuthorizeAttribute
新创建的认证属性，位于 `AdminOrJwtAuthorizeAttribute.cs`

```csharp
[AdminOrJwtAuthorize("AdminOnly")]
public class AdminChatAppService : LocalAppServiceBase
{
    // API 方法可以通过 Cookie 或 JWT 任一方式访问
    // 同时要求用户具有 "AdminMember" Claim
}
```

### Register.cs 配置
```csharp
options.Conventions.AuthorizePage("/", "AdminOnly");                    // 首页
options.Conventions.AuthorizePage("/AdminChat/Chat", "AdminOnly");      // 聊天页面
options.Conventions.AllowAnonymousToPage("/Login");                     // 登录页允许匿名
```

### Policy "AdminOnly" 定义
```csharp
options.AddPolicy("AdminOnly", policy =>
{
    policy.RequireClaim("AdminMember");  // 要求 "AdminMember" Claim
});
```

### 登录时设置的 Claims
在 `AdminUserInfoService.LoginAsync` 方法中：
```csharp
var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, userInfo.UserName),
    new Claim(ClaimTypes.NameIdentifier, userInfo.Id.ToString(), ClaimValueTypes.Integer),
    new Claim("AdminMember", "", ClaimValueTypes.String)  // 关键 Claim
};
```

## 工作原理

### 认证流程
1. 用户通过 `Login.cshtml` 登录
2. 系统设置 Cookie 并添加 "AdminMember" Claim
3. 访问 Chat 页面或 API 时，框架检查：
   - 是否已认证（Cookie 或 JWT）
   - 是否具有 "AdminMember" Claim
4. 两个条件都满足，允许访问

### ASP.NET Core 多认证方案
`AuthenticationSchemes` 属性支持多个认证方案（用逗号分隔）：
```csharp
AuthenticationSchemes = $"{SiteConfig.NcfAdminAuthorizeScheme},{JwtScheme}";
```

当请求到达时：
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

| AppService | 认证属性 | 认证方式 | Policy |
|------------|---------|---------|--------|
| `AdminChatAppService` | `[AdminOrJwtAuthorize("AdminOnly")]` | Cookie **或** JWT | 需要 "AdminMember" Claim |
| `AdminUserInfoAppService` | `[BackendJwtAuthorize]` | 仅 JWT | 无 |
| `StatAppService` | `[AdminAuthorize("AdminOnly")]` | 仅 Cookie | 需要 "AdminMember" Claim |
| `ModuleAppService` | `[BackendJwtAuthorize]` | 仅 JWT | 无 |

## 优势

1. **向后兼容**: 现有 Cookie 登录完全可用
2. **未来扩展**: 支持 API Token 方式，便于移动端或第三方集成
3. **灵活性高**: 不同客户端可以选择最适合的认证方式
4. **安全性**: Policy 确保只有管理员可以访问

## 测试建议

### Cookie 认证测试
1. 通过 `/Admin/Login` 登录管理后台
2. 在首页输入框发送消息
3. 应能正常跳转到聊天页面，不会出现 Forbidden 错误

### JWT 认证测试（可选）
1. 使用 Postman 或其他 API 工具
2. 获取包含 "AdminMember" Claim 的 JWT Token
3. 在请求头添加 `Authorization: Bearer {token}`
4. 调用 `/api/AdminChat/CreateSessionAsync` 等接口
5. 应能正常获取响应

## 注意事项

- **Claim 要求**: 无论使用哪种认证方式，都必须包含 "AdminMember" Claim
- **Razor Pages 路径配置**: 新增的 Razor Pages 需要在 Register.cs 中配置授权策略
- **统一授权策略**: API 和页面使用相同的 "AdminOnly" Policy，保证安全性一致


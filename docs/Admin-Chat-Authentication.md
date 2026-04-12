[中文版](Admin-Chat-Authentication.cn.md)

# Admin Chat authentication configuration instructions

## Authentication method compatibility

`AdminChatAppService` now supports two authentication methods. As long as one of them passes, you can access the API:

### 1. Cookie authentication (web page login)
- **Authentication scheme**: `NcfAdminAuthorizeScheme`
- **Usage scenario**: Log in through the management backend login page (`/Admin/Login`)
- **Applicable to**: Browser web access

### 2. JWT authentication (API Token)
- **Authentication scheme**: `Bearer_Backend`
- **Usage scenarios**: API client, mobile terminal, third-party integration
- **Applicable to**: Scenarios that require Token authentication

## Problem fix history

### Question 1: "The login has expired and will be redirected to the login page soon"
**Cause**: `AdminChatAppService` uses `[BackendJwtAuthorize]`, but the user uses Cookie to log in
**Fix**: Use `[AdminOrJwtAuthorize]` instead to support two authentication methods

### Question 2: "Jump to /Admin/Forbidden"
**Cause**: Chat page requires "AdminOnly" Policy, but it is not configured in Register.cs
**Fix**: Add `options.Conventions.AuthorizePage("/AdminChat/Chat", "AdminOnly");` in Register.cs

## Implementation details

### AdminOrJwtAuthorizeAttribute
The newly created authentication attribute, located in `AdminOrJwtAuthorizeAttribute.cs````csharp
[AdminOrJwtAuthorize("AdminOnly")]
public class AdminChatAppService : LocalAppServiceBase
{
    // API 方法可以通过 Cookie 或 JWT 任一方式访问
    // 同时要求用户具有 "AdminMember" Claim
}
```### Register.cs configuration```csharp
options.Conventions.AuthorizePage("/", "AdminOnly");                    // 首页
options.Conventions.AuthorizePage("/AdminChat/Chat", "AdminOnly");      // 聊天页面
options.Conventions.AllowAnonymousToPage("/Login");                     // 登录页允许匿名
```### Policy "AdminOnly" Definition```csharp
options.AddPolicy("AdminOnly", policy =>
{
    policy.RequireClaim("AdminMember");  // 要求 "AdminMember" Claim
});
```### Claims set when logging in
In the `AdminUserInfoService.LoginAsync` method:```csharp
var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, userInfo.UserName),
    new Claim(ClaimTypes.NameIdentifier, userInfo.Id.ToString(), ClaimValueTypes.Integer),
    new Claim("AdminMember", "", ClaimValueTypes.String)  // 关键 Claim
};
```## Working principle

### Certification process
1. User logs in through `Login.cshtml`
2. The system sets cookies and adds "AdminMember" Claim
3. When accessing the Chat page or API, the framework checks:
   - Whether it is authenticated (Cookie or JWT)
   - Whether it has "AdminMember" Claim
4. If both conditions are met, access is allowed

### ASP.NET Core multi-authentication solution
The `AuthenticationSchemes` property supports multiple authentication schemes (separated by commas):```csharp
AuthenticationSchemes = $"{SiteConfig.NcfAdminAuthorizeScheme},{JwtScheme}";
```When the request arrives:
1. The framework will try the configured authentication schemes in sequence.
2. As long as **any** authentication scheme is successful, access is allowed
3. If **all** authentication schemes fail, 401 Unauthorized will be returned.

## Usage example

### Cookie authentication access (current scenario)```javascript
// 用户通过 Login.cshtml 登录后，浏览器会自动携带 Cookie
axios.post('/api/AdminChat/CreateSessionAsync', data)
  .then(response => {
    // Cookie 认证自动完成，无需额外操作
  });
```### JWT authenticated access (future expansion)```javascript
// 客户端需要先获取 JWT Token，然后在请求头中携带
axios.post('/api/AdminChat/CreateSessionAsync', data, {
  headers: {
    'Authorization': 'Bearer ' + jwtToken
  }
});
```## Other AppService certification comparison

| AppService | Authentication Properties | Authentication Method | Policy |
|------------|---------|---------|--------|
| `AdminChatAppService` | `[AdminOrJwtAuthorize("AdminOnly")]` | Cookie **or** JWT | Requires "AdminMember" Claim |
| `AdminUserInfoAppService` | `[BackendJwtAuthorize]` | JWT only | None |
| `StatAppService` | `[AdminAuthorize("AdminOnly")]` | Cookie only | Requires "AdminMember" Claim |
| `ModuleAppService` | `[BackendJwtAuthorize]` | JWT only | None |

## Advantages

1. **Backward Compatibility**: Existing cookie logins are fully usable
2. **Future Expansion**: Support API Token method to facilitate mobile or third-party integration
3. **High flexibility**: Different clients can choose the most suitable authentication method
4. **Security**: Policy ensures that only administrators have access

## Testing suggestions

### Cookie Certification Test
1. Log in to the management background through `/Admin/Login`
2. Send a message in the input box on the homepage
3. You should be able to jump to the chat page normally without Forbidden errors.

### JWT authentication test (optional)
1. Use Postman or other API tools
2. Get the JWT Token containing "AdminMember" Claim
3. Add `Authorization: Bearer {token}` to the request header
4. Call interfaces such as `/api/AdminChat/CreateSessionAsync`
5. The response should be obtained normally

## Notes

- **Claim requirement**: No matter which authentication method is used, the "AdminMember" Claim must be included
- **Razor Pages path configuration**: The new Razor Pages need to configure the authorization policy in Register.cs
- **Unified authorization policy**: API and page use the same "AdminOnly" Policy to ensure consistent security

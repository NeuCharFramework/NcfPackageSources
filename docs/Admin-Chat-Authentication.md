# Admin Chat Authentication Configuration Guide

## Authentication Compatibility

AdminChatAppService now supports two authentication methods. Access is allowed when either one succeeds.

### 1. Cookie Authentication (Web Sign-in)
- Authentication scheme: NcfAdminAuthorizeScheme
- Usage scenario: Sign in through the admin login page (/Admin/Login)
- Suitable for: Browser-based web access

### 2. JWT Authentication (API Token)
- Authentication scheme: Bearer_Backend
- Usage scenario: API clients, mobile apps, third-party integrations
- Suitable for: Token-based API access

## Issue Fix History

### Issue 1: Login expired, redirecting to login page
**Cause**: AdminChatAppService used [BackendJwtAuthorize], but the user signed in with Cookie authentication.
**Fix**: Switched to [AdminOrJwtAuthorize] to support both authentication methods.

### Issue 2: Redirected to /Admin/Forbidden
**Cause**: The Chat page requires the AdminOnly policy, but it was not configured in Register.cs.
**Fix**: Added options.Conventions.AuthorizePage("/AdminChat/Chat", "AdminOnly"); in Register.cs.

## Implementation Details

### AdminOrJwtAuthorizeAttribute
A newly introduced authorization attribute located in AdminOrJwtAuthorizeAttribute.cs.

```csharp
[AdminOrJwtAuthorize("AdminOnly")]
public class AdminChatAppService : LocalAppServiceBase
{
    // API methods can be accessed through either Cookie or JWT
    // Users must also have the AdminMember claim
}
```

### Register.cs Configuration
```csharp
options.Conventions.AuthorizePage("/", "AdminOnly");                    // Home page
options.Conventions.AuthorizePage("/AdminChat/Chat", "AdminOnly");      // Chat page
options.Conventions.AllowAnonymousToPage("/Login");                     // Login page allows anonymous
```

### AdminOnly Policy Definition
```csharp
options.AddPolicy("AdminOnly", policy =>
{
    policy.RequireClaim("AdminMember");  // Requires AdminMember claim
});
```

### Claims set during sign-in
In AdminUserInfoService.LoginAsync:

```csharp
var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, userInfo.UserName),
    new Claim(ClaimTypes.NameIdentifier, userInfo.Id.ToString(), ClaimValueTypes.Integer),
    new Claim("AdminMember", "", ClaimValueTypes.String)  // Key claim
};
```

## How It Works

### Authentication flow
1. User signs in through Login.cshtml.
2. System sets Cookie and adds AdminMember claim.
3. When visiting Chat page or API, framework checks:
   - Is the user authenticated (Cookie or JWT)?
   - Does the user have AdminMember claim?
4. Access is granted only when both conditions are met.

### Multiple authentication schemes in ASP.NET Core
The AuthenticationSchemes property supports multiple schemes separated by commas:

```csharp
AuthenticationSchemes = $"{SiteConfig.NcfAdminAuthorizeScheme},{JwtScheme}";
```

When a request arrives:
1. Framework tries configured schemes in order.
2. Access is allowed when any one scheme succeeds.
3. A 401 Unauthorized response is returned only when all schemes fail.

## Usage Examples

### Cookie-based access (current scenario)

```javascript
// After signing in via Login.cshtml, browser automatically includes Cookie
axios.post('/api/AdminChat/CreateSessionAsync', data)
  .then(response => {
    // Cookie authentication is automatic
  });
```

### JWT-based access (future extension)

```javascript
// Client first obtains JWT token, then sends it in Authorization header
axios.post('/api/AdminChat/CreateSessionAsync', data, {
  headers: {
    'Authorization': 'Bearer ' + jwtToken
  }
});
```

## Authentication Comparison with Other AppServices

| AppService | Authorization Attribute | Auth Method | Policy |
|------------|---------|---------|--------|
| AdminChatAppService | [AdminOrJwtAuthorize("AdminOnly")] | Cookie or JWT | Requires AdminMember claim |
| AdminUserInfoAppService | [BackendJwtAuthorize] | JWT only | None |
| StatAppService | [AdminAuthorize("AdminOnly")] | Cookie only | Requires AdminMember claim |
| ModuleAppService | [BackendJwtAuthorize] | JWT only | None |

## Benefits

1. Backward compatible: Existing Cookie sign-in works as-is.
2. Future-ready: Supports API token flow for mobile and third-party integrations.
3. Flexible: Different clients can choose the best authentication mode.
4. Secure: Policy ensures only admins can access protected resources.

## Test Recommendations

### Cookie authentication test
1. Sign in to admin console via /Admin/Login.
2. Send a message from the home page input box.
3. It should navigate to Chat page normally without Forbidden errors.

### JWT authentication test (optional)
1. Use Postman or another API tool.
2. Obtain a JWT token that includes AdminMember claim.
3. Add Authorization: Bearer {token} in request headers.
4. Call endpoints such as /api/AdminChat/CreateSessionAsync.
5. Response should be returned successfully.

## Notes

- Claim requirement: regardless of authentication mode, AdminMember claim is required.
- Razor Pages path policy: newly added Razor Pages must be configured in Register.cs.
- Unified authorization policy: API and page share the same AdminOnly policy for consistent security.

# API permission verification implementation instructions

## 📌 Overview

According to security requirements, user login permission verification has been added to all AppServices of the **AgentsManager** and **PromptRange** modules to prevent unauthorized access.

## 🔐 Implementation plan

### 1. Create unified permission verification features

Created in two modules respectively`ApiAuthorizeAttribute.cs`, supports **.NET regular Cookie authentication** and **JWT Bearer authentication**:

#### AgentsManager module
**File path**:`src/Extensions/Senparc.Xncf.AgentsManager/OHS/Local/AppService/ApiAuthorizeAttribute.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Senparc.Ncf.AreaBase.Admin.Filters;
using System;

namespace Senparc.Xncf.AgentsManager.OHS.Local.AppService
{
    /// <summary>
    /// API 权限验证特性
    /// 支持多种认证方案：Cookie (AdminAuthorize) 和 JWT (Bearer)
    /// </summary>
    public class ApiAuthorizeAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// 默认构造函数，支持 Cookie 和 JWT 两种认证方式
        /// </summary>
        public ApiAuthorizeAttribute()
        {
            // 支持多种认证方案：Admin Cookie 认证 和 JWT Bearer 认证
            // 只要任一方案通过，即可访问
            base.AuthenticationSchemes = $"{AdminAuthorizeAttribute.AuthenticationScheme},Bearer";
        }

        /// <summary>
        /// 带策略的构造函数
        /// </summary>
        /// <param name="policy">授权策略名称（如 "AdminOnly"）</param>
        public ApiAuthorizeAttribute(string policy) : this()
        {
            this.Policy = policy;
        }
    }
}
```

#### PromptRange module
**File path**:`src/Extensions/Senparc.Xncf.PromptRange/OHS/Local/AppService/ApiAuthorizeAttribute.cs`

(The content is the same, only the namespace is different)

### 2. Apply to AppService

#### AgentsManager module protected AppService

1. **PromptCatalyzerInitAppService** - PromptCatalyzer initialization service
   - ✅ `CheckStatus()`- Check initialization status
   - ✅ `GetAvailableModels()`- Get available AI models
   - ✅ `Initialize()`- Perform initialization

2. **PromptOptimizationAppService** - Prompt optimization service
   - ✅ `EnsureInitializedAsync()`- Make sure it is initialized
   - ✅ `OptimizeAsync()`- Perform optimization

#### PromptRange module protected AppService

1. **PromptItemAppService** - Prompt item management
   - ✅ `Add()`- Add Prompt
   - ✅ `Edit()`- Edit Prompt
   - ✅ `Delete()`- Delete Prompt
   - ✅ `GetList()`- Get list
- ✅ All other management methods

2. **PromptRangeAppService** - PromptRange management
- ✅ All management methods

3. **PromptResultAppService** - Prompt result management
- ✅ All rating and result management methods

4. **StatisticAppService** - statistical data service
- ✅ All statistical methods

5. **LlmModelAppService** - LLM model management
- ✅ All model management methods

### 3. Authentication scheme description

#### Authentication scheme support

```csharp
[ApiAuthorize("AdminOnly")]
public class YourAppService : AppServiceBase
{
    // 所有方法都受保护
}
```

- **Cookie Authentication**: used`AdminAuthorizeAttribute.AuthenticationScheme` (NcfAdminAuthorizeScheme)
- **JWT Authentication**: Use`Bearer` scheme
- **Strategy**:`AdminOnly`- Requires user to have`AdminMember`statement

#### Front-end interaction

front end`axios.js`Configured to handle authentication failures:

```javascript
servicePR.interceptors.response.use(
    response => { /* ... */ },
    error => {
        if (error.message.includes('401')) {
            app.$message({
                message: '登陆过期，即将跳转到登录页面',
                type: 'error',
                duration: 3 * 1000,
                onClose: function () {
                    window.location.href = '/Admin/Login?url=' + escape(window.location.pathname + window.location.search);
                }
            });
        } else if (error.message.includes('403')) {
            app.$message({
                message: '您没有访问权限~',
                type: 'error',
                duration: 3 * 1000
            });
        }
        return Promise.reject(error);
    }
);
```

## 🎯 Security

### Behavior when non-logged-in users access the API

1. **Cookie authentication failed** or **JWT Token is invalid/missing**
2. The server returns **401 Unauthorized**
3. Front-end interceptor catches errors
4. Automatically jump to`/Admin/Login`Login page

### Behavior when permissions are insufficient

1. The user is logged in but is missing`AdminMember`statement
2. The server returns **403 Forbidden**
3. The front end displays the prompt "You do not have access rights~"

## 📋 List of protected API endpoints

### AgentsManager module

| API endpoint | method | illustrate | Permissions |
|---------|------|------|------|
| `/api/PromptCatalyzerInit/CheckStatus` | GET | Check initialization status | AdminOnly |
| `/api/PromptCatalyzerInit/GetAvailableModels` | GET | Get available models | AdminOnly |
| `/api/PromptCatalyzerInit/Initialize` | POST | Perform initialization | AdminOnly |
| `/api/PromptOptimization/EnsureInitializedAsync` | POST | Make sure it is initialized | AdminOnly |
| `/api/PromptOptimization/OptimizeAsync` | POST | Optimize Prompt | AdminOnly |

### PromptRange module

| AppService | Number of protected methods | Permissions |
|-----------|--------------|------|
| PromptItemAppService | All (~20) | AdminOnly |
| PromptRangeAppService | All (~10) | AdminOnly |
| PromptResultAppService | All (~15) | AdminOnly |
| StatisticAppService | All (~5) | AdminOnly |
| LlmModelAppService | All (~8) | AdminOnly |

> **Note**:`ApiAppService`It is a test service and no permission verification is added (if it needs to be used in production, it needs to be evaluated separately)

## ✅ Verification results

### Compilation status
- ✅ **Senparc.Xncf.AgentsManager.csproj** - Compiled successfully, 0 errors
- ✅ **Senparc.Xncf.PromptRange.csproj** - Compiled successfully, 0 errors

### Security testing recommendations

1. **Access test without logging in**
- Clear Cookie/Token
- Attempt to access any API endpoint
- Expected: 401 error, automatically jump to the login page

2. **Insufficient permissions test**
- Log in using a non-administrator account
- Try to access the API endpoint
- Expected: 403 error, prompting no permissions

3. **Normal access test**
- Log in with an administrator account
- Access all features
- Expectation: Works normally

## 🔧 Technical details

### Certification process

```
用户请求 API
    ↓
[ApiAuthorize] 特性拦截
    ↓
检查 Cookie 或 JWT Token
    ↓
验证 AdminOnly 策略 (需要 AdminMember 声明)
    ↓
通过 → 执行方法
失败 → 返回 401/403
```

### Integrate with existing systems

- **Reuse existing authentication infrastructure**: Use`AdminAuthorizeAttribute.AuthenticationScheme`
- **Compatible with multiple authentication methods**: Cookie + JWT
- **Unified Policy Management**: Use`AdminOnly`Strategy
- **No changes required on the front end**: axios interceptor already supports 401/403 processing

## 📝 Best Practices

1. **Class level application**: Add on the AppService class`[ApiAuthorize("AdminOnly")]`, protect all methods
2. **Strategy Priority**: Use`AdminOnly`Policies rather than pure authentication checks
3. **Front-end friendly**: Make sure the front-end has clear error prompts and login jumps
4. **Complete test**: Test three scenarios: not logged in, insufficient permissions, and normal login.

## 🚀 Deployment Notes

1. Make sure the system is configured`AdminOnly`strategy and`AdminMember`statement
2. Confirm that the JWT Bearer authentication scheme has been registered in Startup/Program.cs
3. The front-end axios configuration has correctly handled 401/403 responses.
4. All new API endpoints should be tested for permission verification

---

**Implementation Date**: 2026-03-24
**Scope of Impact**: All management APIs of AgentsManager and PromptRange modules
**Security level**: Administrator only (AdminOnly)

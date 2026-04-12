[English](API-Authorization-Implementation.md)

# API 权限验证实现说明

## 📌 概述

根据安全要求，为 **AgentsManager** 和 **PromptRange** 模块的所有 AppService 添加了用户登录权限验证，防止未授权访问。

## 🔐 实现方案

### 1. 创建统一的权限验证特性

在两个模块中分别创建了 `ApiAuthorizeAttribute.cs`，支持 **.NET 常规 Cookie 认证** 和 **JWT Bearer 认证**：

#### AgentsManager 模块
**文件路径**: `src/Extensions/Senparc.Xncf.AgentsManager/OHS/Local/AppService/ApiAuthorizeAttribute.cs`

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

#### PromptRange 模块
**文件路径**: `src/Extensions/Senparc.Xncf.PromptRange/OHS/Local/AppService/ApiAuthorizeAttribute.cs`

（内容相同，仅命名空间不同）

### 2. 应用到 AppService

#### AgentsManager 模块受保护的 AppService

1. **PromptCatalyzerInitAppService** - PromptCatalyzer 初始化服务
   - ✅ `CheckStatus()` - 检查初始化状态
   - ✅ `GetAvailableModels()` - 获取可用 AI 模型
   - ✅ `Initialize()` - 执行初始化

2. **PromptOptimizationAppService** - Prompt 优化服务
   - ✅ `EnsureInitializedAsync()` - 确保已初始化
   - ✅ `OptimizeAsync()` - 执行优化

#### PromptRange 模块受保护的 AppService

1. **PromptItemAppService** - Prompt 条目管理
   - ✅ `Add()` - 添加 Prompt
   - ✅ `Edit()` - 编辑 Prompt
   - ✅ `Delete()` - 删除 Prompt
   - ✅ `GetList()` - 获取列表
   - ✅ 其他所有管理方法

2. **PromptRangeAppService** - PromptRange 管理
   - ✅ 所有管理方法

3. **PromptResultAppService** - Prompt 结果管理
   - ✅ 所有评分和结果管理方法

4. **StatisticAppService** - 统计数据服务
   - ✅ 所有统计方法

5. **LlmModelAppService** - LLM 模型管理
   - ✅ 所有模型管理方法

### 3. 认证方案说明

#### 认证方案支持

```csharp
[ApiAuthorize("AdminOnly")]
public class YourAppService : AppServiceBase
{
    // 所有方法都受保护
}
```

- **Cookie 认证**: 使用 `AdminAuthorizeAttribute.AuthenticationScheme` (NcfAdminAuthorizeScheme)
- **JWT 认证**: 使用 `Bearer` scheme
- **策略**: `AdminOnly` - 要求用户具有 `AdminMember` 声明

#### 前端交互

前端 `axios.js` 已配置处理认证失败：

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

## 🎯 安全保障

### 未登录用户访问 API 时的行为

1. **Cookie 认证失败** 或 **JWT Token 无效/缺失**
2. 服务器返回 **401 Unauthorized**
3. 前端拦截器捕获错误
4. 自动跳转到 `/Admin/Login` 登录页面

### 权限不足时的行为

1. 用户已登录但缺少 `AdminMember` 声明
2. 服务器返回 **403 Forbidden**
3. 前端显示"您没有访问权限~"提示

## 📋 受保护的 API 端点清单

### AgentsManager 模块

| API 端点 | 方法 | 说明 | 权限 |
|---------|------|------|------|
| `/api/PromptCatalyzerInit/CheckStatus` | GET | 检查初始化状态 | AdminOnly |
| `/api/PromptCatalyzerInit/GetAvailableModels` | GET | 获取可用模型 | AdminOnly |
| `/api/PromptCatalyzerInit/Initialize` | POST | 执行初始化 | AdminOnly |
| `/api/PromptOptimization/EnsureInitializedAsync` | POST | 确保已初始化 | AdminOnly |
| `/api/PromptOptimization/OptimizeAsync` | POST | 优化 Prompt | AdminOnly |

### PromptRange 模块

| AppService | 受保护方法数量 | 权限 |
|-----------|--------------|------|
| PromptItemAppService | 全部 (~20个) | AdminOnly |
| PromptRangeAppService | 全部 (~10个) | AdminOnly |
| PromptResultAppService | 全部 (~15个) | AdminOnly |
| StatisticAppService | 全部 (~5个) | AdminOnly |
| LlmModelAppService | 全部 (~8个) | AdminOnly |

> **注意**: `ApiAppService` 是测试服务，未添加权限验证（如需生产使用，需单独评估）

## ✅ 验证结果

### 编译状态
- ✅ **Senparc.Xncf.AgentsManager.csproj** - 编译成功，0 错误
- ✅ **Senparc.Xncf.PromptRange.csproj** - 编译成功，0 错误

### 安全测试建议

1. **未登录访问测试**
   - 清除 Cookie/Token
   - 尝试访问任意 API 端点
   - 预期：401 错误，自动跳转登录页

2. **权限不足测试**
   - 使用非管理员账户登录
   - 尝试访问 API 端点
   - 预期：403 错误，提示无权限

3. **正常访问测试**
   - 使用管理员账户登录
   - 访问所有功能
   - 预期：正常工作

## 🔧 技术细节

### 认证流程

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

### 与现有系统集成

- **复用现有认证基础设施**: 使用 `AdminAuthorizeAttribute.AuthenticationScheme`
- **兼容多种认证方式**: Cookie + JWT
- **统一策略管理**: 使用 `AdminOnly` 策略
- **前端无需改动**: axios 拦截器已支持 401/403 处理

## 📝 最佳实践

1. **类级别应用**: 在 AppService 类上添加 `[ApiAuthorize("AdminOnly")]`，保护所有方法
2. **策略优先**: 使用 `AdminOnly` 策略而非单纯的认证检查
3. **前端友好**: 确保前端有明确的错误提示和登录跳转
4. **测试完整**: 测试未登录、权限不足、正常登录三种场景

## 🚀 部署注意事项

1. 确保系统已配置 `AdminOnly` 策略和 `AdminMember` 声明
2. 确认 JWT Bearer 认证方案已在 Startup/Program.cs 中注册
3. 前端 axios 配置已正确处理 401/403 响应
4. 所有新 API 端点都应经过权限验证测试

---

**实施日期**: 2026-03-24
**影响范围**: AgentsManager 和 PromptRange 模块的所有管理 API
**安全级别**: 管理员专用（AdminOnly）

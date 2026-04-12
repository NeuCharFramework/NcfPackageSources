[中文版](PromptCatalyzer-404-Fix.cn.md)

#404 Bug Fix Instructions - PromptCatalyzer API Endpoint

## 🐛 Problem description

**Error message**:```
GET http://localhost:5000/api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/CheckStatus 404 (Not Found)
GET http://localhost:5000/api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/GetAvailableModels 404 (Not Found)
```**Reason**: The newly created `PromptCatalyzerInitAppService` class uses the `[ApiBind]` feature, but due to dependency configuration issues in the AgentsManager project, this feature does not work properly, resulting in the API endpoint not being registered.

## 🔧 Solution

### 1. Delete problem files
Removed problematic AppService implementation:
- `src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/PromptCatalyzerInitAppService.cs`

### 2. Create a traditional API Controller

**New file**: `src/Extensions/Senparc.Xncf.AgentsManager/OHS/Remote/Controllers/PromptCatalyzerInitController.cs`

Using the traditional ASP.NET Core Web API Controller approach:```csharp
[ApiController]
[Route("api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService")]
public class PromptCatalyzerInitController : ControllerBase
{
    // 三个 API 端点：
    // GET  /api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/CheckStatus
    // GET  /api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/GetAvailableModels  
    // POST /api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/Initialize
}
```### 3. API endpoint details

#### CheckStatus (GET)
- **Path**: `/api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/CheckStatus`
- **Function**: Check if PromptCatalyzer Agent already exists
- **Return**: `AppResponseBase<PromptCatalyzerStatusDto>`

#### GetAvailableModels (GET)
- **Path**: `/api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/GetAvailableModels`
- **Function**: Get all available Chat type AI Models
- **Return**: `AppResponseBase<AvailableModelsDto>`

#### Initialize (POST)
- **Path**: `/api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/Initialize`
- **Function**: Initialize PromptCatalyzer Agent and related Prompt resources
- **Parameter**: `{ "modelId": 1 }`
- **Return**: `AppResponseBase<InitializeResponseDto>`

### 4. Routing design

In order to keep the front-end code unchanged, the Controller's routing is set to the same format as the original AppService:
- Use `[Route("api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService")]`
- Methods use relative paths such as `[HttpGet("CheckStatus")]`

## ✅ Compilation verification```bash
dotnet build src/Extensions/Senparc.Xncf.AgentsManager/Senparc.Xncf.AgentsManager.csproj
# 结果: ✅ 0 错误，10 个警告
```## 🚀 Next step

**The application must be restarted** for the new Controller to take effect:

1. **Stop the current application** (if running)
2. **Clear compilation cache** (optional but recommended):```bash
   dotnet clean
   ```3. **Restart the application**:```bash
   dotnet run --project tools/NcfSimulatedSite/Senparc.Web/Senparc.Web.csproj
   ```4. **Refresh browser page** (Ctrl+Shift+R / Cmd+Shift+R)
5. **Test the "Optimize" button**

## 📋 Technical description

### Why use Controller instead of AppService?

1. **Compatibility issue**: `[ApiBind]` attribute cannot be parsed normally in AgentsManager project
2. **Dependency issue**: AgentsManager uses NuGet package reference instead of project reference, which may cause type resolution problems
3. **Reliability**: Traditional Controller is the standard method of ASP.NET Core, which is more stable and reliable

### Controller vs AppService comparison

| Features | AppService + [ApiBind] | Traditional Controller |
|------|------------------------|------------------------|
| Route registration | Dynamic automatic registration | ASP.NET Core automatic scanning |
| Dependency requirements | Senparc.CO2NET.WebApi | Built-in support |
| Configuration complexity | Low | Low |
| Stability | Depends on framework version | High (standard implementation) |

## 🔐 Permission verification

Controller level permission verification will be implemented in the next step, via:
- `[Authorize]` feature
- Or custom authorization filter

---

**Repair time**: 2026-03-24
**Scope of Impact**: PromptCatalyzer initialization function
**Status**: ✅ Fixed, waiting for verification by restarting the app

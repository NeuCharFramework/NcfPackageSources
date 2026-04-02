# 404 Fix Notes - PromptCatalyzer API Endpoints

## 🐛 Issue Description

**Errors**:

```
GET http://localhost:5000/api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/CheckStatus 404 (Not Found)
GET http://localhost:5000/api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/GetAvailableModels 404 (Not Found)
```

**Cause**: The newly created PromptCatalyzerInitAppService used the ApiBind attribute, but because of dependency configuration issues in the AgentsManager project, the attribute did not work correctly and the API endpoints were not registered.

## 🔧 Solution

### 1. Remove problematic file
Removed the problematic AppService implementation:

- src/Extensions/Senparc.Xncf.AgentsManager/Application/AppService/PromptCatalyzerInitAppService.cs

### 2. Create traditional API controller

**New file**: src/Extensions/Senparc.Xncf.AgentsManager/OHS/Remote/Controllers/PromptCatalyzerInitController.cs

Use the standard ASP.NET Core Web API Controller approach:

```csharp
[ApiController]
[Route("api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService")]
public class PromptCatalyzerInitController : ControllerBase
{
    // Three API endpoints:
    // GET  /api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/CheckStatus
    // GET  /api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/GetAvailableModels
    // POST /api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/Initialize
}
```

### 3. Endpoint details

#### CheckStatus (GET)
- Path: /api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/CheckStatus
- Purpose: Check whether PromptCatalyzer Agent already exists
- Return type: AppResponseBase<PromptCatalyzerStatusDto>

#### GetAvailableModels (GET)
- Path: /api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/GetAvailableModels
- Purpose: Get all available AI models of chat type
- Return type: AppResponseBase<AvailableModelsDto>

#### Initialize (POST)
- Path: /api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/Initialize
- Purpose: Initialize PromptCatalyzer Agent and related Prompt resources
- Parameters: { "modelId": 1 }
- Return type: AppResponseBase<InitializeResponseDto>

### 4. Routing design

To keep frontend code unchanged, controller routes use the same format as the original AppService:

- Base route: [Route("api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService")]
- Method route style: [HttpGet("CheckStatus")], etc.

## ✅ Build Verification

```bash
dotnet build src/Extensions/Senparc.Xncf.AgentsManager/Senparc.Xncf.AgentsManager.csproj
# Result: ✅ 0 errors, 10 warnings
```

## 🚀 Next Steps

Application restart is required for the new controller to take effect:

1. Stop the current application (if running).
2. Clean build cache (optional but recommended):

```bash
dotnet clean
```

3. Restart application:

```bash
dotnet run --project tools/NcfSimulatedSite/Senparc.Web/Senparc.Web.csproj
```

4. Hard refresh browser (Ctrl+Shift+R / Cmd+Shift+R).
5. Re-test the Optimize button.

## 📋 Technical Notes

### Why use Controller instead of AppService?

1. Compatibility issue: ApiBind could not be resolved correctly in AgentsManager.
2. Dependency issue: AgentsManager uses NuGet package references instead of project references, which may affect type resolution.
3. Reliability: Traditional Controller is standard ASP.NET Core implementation and is more stable.

### Controller vs AppService

| Capability | AppService + ApiBind | Traditional Controller |
|------|------------------------|------------------------|
| Route registration | Dynamic auto registration | ASP.NET Core auto discovery |
| Dependency requirement | Senparc.CO2NET.WebApi | Built-in support |
| Config complexity | Low | Low |
| Stability | Framework-version dependent | High (standard implementation) |

## 🔐 Authorization

Controller-level authorization will be implemented in the next step using:

- Authorize attribute
- Or a custom authorization filter

---

**Fix Time**: 2026-03-24  
**Impact Scope**: PromptCatalyzer initialization workflow  
**Status**: ✅ Fixed, pending verification after application restart

# Senparc.Xncf.SystemManager

`Senparc.Xncf.SystemManager` provides NCF system configuration, feedback, and administrative data services.

## Features

- Persists system configuration and feedback records.
- Exposes service and application-service layers for administrators.
- Includes area/province/city/district data models used by system forms.
- Supports NCF multi-database contexts, migrations, and localized resources.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.SystemManager" Version="0.26.0-preview3" />
```

## Key API

- `SystemConfigService` and `SystemConfigAppService` read and update system settings.
- `SystemConfig_GetRequestTempLogRequest` and `SystemConfig_UpdateNeuCharAccountRequest` model common configuration operations.
- `IFeedBackRepository`, `FeedBackRepository`, and `FeedBackService` manage feedback.
- `SystemManagerSenparcEntities` is the module database context.

Protect configuration and feedback endpoints with administrator authorization and redact account credentials or personal data in logs and responses.

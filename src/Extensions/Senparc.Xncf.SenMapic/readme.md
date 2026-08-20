# Senparc.Xncf.SenMapic

`Senparc.Xncf.SenMapic` is an NCF module for configuring crawl tasks, collecting page data, and producing sitemap-oriented output for monitored URLs.

## Features

- Manages crawl tasks, task items, URL collections, filters, strategies, and crawl status.
- Provides task and task-item services with DTOs for administrative workflows.
- Includes sitemap/report helpers and provider-specific EF Core contexts.
- Exposes models for URL metadata and source-specific crawl results.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.SenMapic" Version="0.26.0-preview3" />
```

## Key API

- `SenMapicTaskService` and `SenMapicTaskItemService` manage crawl plans and individual URLs.
- `SenMapicTask_CreateUpdateDto` and `SenMapicTaskItem_ListItemDto` model management operations.
- `SiteMapHandler` and `BuildGoogleSitemapWithReport` support sitemap generation and reporting.
- `AutoAlertSitemapUtility` provides automatic sitemap-related notification hooks.
- `SenMapicSemaphore` helps coordinate crawl concurrency.

The host must provide a clear crawl policy, robots/terms compliance, rate limits, URL allowlists, and content privacy rules. Do not treat this module as permission to crawl arbitrary third-party systems.

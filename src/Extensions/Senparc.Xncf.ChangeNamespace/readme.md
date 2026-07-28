# Senparc.Xncf.ChangeNamespace

`Senparc.Xncf.ChangeNamespace` is a legacy XNCF utility that changes namespaces across an NCF template source tree before publication.

## Features

- Matches source files and namespace rules through `MatchNamespace` and `MeetRule`.
- Exposes request models for source download, namespace change, and restore operations.
- Keeps an NCF-compatible module/service surface for older automated workflows.

## Installation

```xml
<PackageReference Include="Senparc.Xncf.ChangeNamespace" Version="0.26.0-preview3" />
```

## Key API

- `NameSpaceAppService` is the application-service entry point.
- `NameSpace_ChangeRequest` describes a namespace replacement operation.
- `NameSpace_DownloadSourceCodeRequest` describes source acquisition.
- `NameSpace_RestoreRequest` describes restoration of generated changes.
- `MatchNamespace` and `MeetRule` contain matching-rule data and evaluation support.

This module is legacy and no longer the preferred project-generation path. New projects should use the maintained .NET template package. If this module is retained, run it only on a disposable or version-controlled source copy and review every generated file.

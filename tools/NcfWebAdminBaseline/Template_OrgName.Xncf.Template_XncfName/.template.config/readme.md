## Overview

This folder contains the `dotnet new` template configuration and is synchronized from the authoritative template master into the template NuGet project.

- Project: https://github.com/dotnet/templating
- Excluding folders: https://github.com/dotnet/templating/issues/850#issuecomment-303870563

## Validate the template manually

```bash
dotnet new install ./
dotnet new XNCF -n MyOrg.Xncf.MyNewProject --force \
  --IntegrationToNcf true --Sample true --Function true --Web true --Database true \
  --OrgName MyOrg --XncfName MyNewProject \
  --Guid C4BC0C46-8438-4EEE-94F5-88C5B7731227 \
  --Icon "fa fa-star" --Description "Module description" --Version 0.2 --MenuName "Generated from template"
```
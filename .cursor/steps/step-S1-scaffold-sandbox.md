# Step S1: Scaffold Senparc.Xncf.Sandbox

## Task Overview
Use the installed XNCF template to generate an independent module, align net10.0 and ProjectReference settings, add it to the solution, and initialize the readme.

## Goals
- [x] `dotnet new XNCF` generated `src/Extensions/Senparc.Xncf.Sandbox`
- [x] UID=`BDF12490-AA0B-41B4-ADB3-63155ED95A93`, DB prefix `Senparc_Sandbox_`
- [x] Align with the repository: net10.0 + Basic ProjectReference
- [x] Add `src/NcfPackageSources.sln` / SimulatedSite host references
- [x] Create the module `readme.md` skeleton

## Files Involved
- `src/Extensions/Senparc.Xncf.Sandbox/**`
- `tools/NcfWebAdminBaseline/Senparc.Web/Senparc.Web.csproj`
- `src/NcfPackageSources.sln`

## Implementation Steps
1. `dotnet new XNCF -n Senparc.Xncf.Sandbox -o src/Extensions/Senparc.Xncf.Sandbox --OrgName Senparc --XncfName Sandbox --Guid BDF12490-AA0B-41B4-ADB3-63155ED95A93 --Version 0.1.0-preview1 --MenuName "Sandbox environment" --Icon "fa fa-cube" --Description "NCF independent sandbox orchestration: quickly create and destroy Docker/Wasm experiment environments" --TargetFramework net10.0 --Web true --Database true --Function true --UseWebApi true --Sample false --IntegrationToNcf true --XncfBaseVersion 0.24.1-preview6 --NcfAreaBaseVersion 0.23.2-preview6`
2. Change PackageReference entries to ProjectReference entries for this repository's `Senparc.Ncf.*` projects (align with FileManager).
3. Remove the product-irrelevant Color sample if it is generated.
4. Add the project with `dotnet sln`; add the ProjectReference to the host.
5. Initialize the readme with architecture, security, and TODO sections.

## Acceptance Criteria
- [ ] The module directory exists and the UID/prefix is unique (inspect reports no conflict).
- [ ] The project can restore/build (final success may follow later domain implementation).
- [ ] The readme contains important notes and user TODOs.

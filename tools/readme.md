# Tools

| Path | Description |
|------|-------------|
| [BuildXncfBuilderTemplate](BuildXncfBuilderTemplate) | Replaces placeholders in `Senparc.Xncf.XncfBuilder.Template` so files match `dotnet new` template parameters. |
| [NcfWebAdminBaseline](NcfWebAdminBaseline) | NCF Web Admin baseline referencing all XNCF module sources for integration, verification, and template sync; authoritative backend source for the [NCF template repo](https://github.com/NeuCharFramework/NCF) and the [Senparc.NCF.Template](https://www.nuget.org/packages/Senparc.NCF.Template) NuGet package. |
| [NcfWebAdminSyncTool](NcfWebAdminSyncTool) | Syncs Web Admin, module sources, and the XNCF template master from baseline into packaging projects. Requires the [NCF template repo](https://github.com/NeuCharFramework/NCF) as a sibling of this repository. |
| `remove-ncf-database.sql` | Script to drop NCF databases completely. |
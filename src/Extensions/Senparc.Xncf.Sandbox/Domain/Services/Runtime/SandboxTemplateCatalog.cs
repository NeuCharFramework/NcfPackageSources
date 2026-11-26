/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxTemplateCatalog.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20260815
    Change description: v0.2.0 add the NCF preview sandbox workload

    Modified by: Senparc - 20260817
    Change description: v0.2.0 enhance the jupyter-csharp template and sandbox session management

    Modified by: Senparc - 20260822
    Change description: v0.2.0 enhance sandbox preview, Jupyter workspace, and session lifecycle management

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

----------------------------------------------------------------*/

using Senparc.Xncf.Sandbox.Abstractions;

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

public static class SandboxTemplateCatalog
{
    private static readonly IReadOnlyDictionary<string, SandboxTemplateDefinition> Templates =
        new Dictionary<string, SandboxTemplateDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [SandboxTemplateKeys.PythonExec] = new SandboxTemplateDefinition
            {
                Key = SandboxTemplateKeys.PythonExec,
                DisplayName = "Python Exec",
                PreferredRuntime = SandboxRuntimeKind.Docker,
                Interactive = false,
                Image = "python:3.12-alpine",
                DefaultCpuLimit = 0.5,
                DefaultMemoryMb = 256,
                DefaultTtl = TimeSpan.FromMinutes(15)
            },
            [SandboxTemplateKeys.CsharpExec] = new SandboxTemplateDefinition
            {
                Key = SandboxTemplateKeys.CsharpExec,
                DisplayName = "C# Exec (.NET 10)",
                PreferredRuntime = SandboxRuntimeKind.Docker,
                Interactive = false,
                Image = "mcr.microsoft.com/dotnet/sdk:10.0",
                DefaultCpuLimit = 0.75,
                DefaultMemoryMb = 512,
                DefaultTtl = TimeSpan.FromMinutes(15)
            },
            [SandboxTemplateKeys.JupyterPython] = new SandboxTemplateDefinition
            {
                Key = SandboxTemplateKeys.JupyterPython,
                DisplayName = "JupyterLab (Python)",
                PreferredRuntime = SandboxRuntimeKind.Docker,
                Interactive = true,
                Image = "quay.io/jupyter/minimal-notebook:latest",
                ContainerPort = 8888,
                SupportsInteractiveControl = true,
                WorkspaceMountPath = "/home/jovyan/work",
                DefaultCpuLimit = 1,
                DefaultMemoryMb = 1024,
                DefaultTtl = TimeSpan.FromMinutes(45)
            },
            [SandboxTemplateKeys.JupyterCsharp] = new SandboxTemplateDefinition
            {
                Key = SandboxTemplateKeys.JupyterCsharp,
                DisplayName = "JupyterLab (C#)",
                PreferredRuntime = SandboxRuntimeKind.Docker,
                Interactive = true,
                // Build tools/SandboxImages/JupyterDotnet or override this image in appsettings.
                Image = "ncf-jupyter-dotnet:10.0",
                ContainerPort = 8888,
                SupportsInteractiveControl = true,
                WorkspaceMountPath = "/home/jovyan/work",
                DefaultCpuLimit = 1,
                DefaultMemoryMb = 1536,
                DefaultTtl = TimeSpan.FromMinutes(45)
            },
            [SandboxTemplateKeys.NcfPreview] = new SandboxTemplateDefinition
            {
                Key = SandboxTemplateKeys.NcfPreview,
                DisplayName = "NCF/XNCF Preview (pinned image required)",
                PreferredRuntime = SandboxRuntimeKind.Docker,
                Interactive = true,
                // This intentionally cannot be used safely until Images:Overrides:ncf-preview is
                // configured with an organisation-approved digest.
                Image = "ncf-preview:must-configure-digest",
                ContainerPort = 8080,
                DefaultCpuLimit = 1,
                DefaultMemoryMb = 1536,
                DefaultTtl = TimeSpan.FromMinutes(30)
            }
        };

    public static IReadOnlyCollection<SandboxTemplateDefinition> All => Templates.Values.ToArray();

    /// <summary>
    /// Looks up a sandbox template definition by key.
    /// </summary>
    /// <param name="templateKey">The template key.</param>
    /// <param name="template">Receives the template definition when found.</param>
    /// <returns><see langword="true"/> when the template is registered.</returns>
    public static bool TryGet(string templateKey, out SandboxTemplateDefinition template)
    {
        return Templates.TryGetValue(templateKey, out template!);
    }
}

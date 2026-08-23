/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxTemplateCatalog.cs
    文件功能描述：沙箱运行模板目录与查找服务


    创建标识：Senparc - 20260808

    修改标识：Senparc - 20260815
    修改描述：v0.2.0 增加 NCF 预览沙箱工作负载

    修改标识：Senparc - 20260817
    修改描述：v0.2.0 增强 jupyter-csharp 模板与沙箱会话管理

    修改标识：Senparc - 20260822
    修改描述：v0.2.0 增强沙箱预览、Jupyter 工作区与会话生命周期管理

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

    public static bool TryGet(string templateKey, out SandboxTemplateDefinition template)
    {
        return Templates.TryGetValue(templateKey, out template!);
    }
}

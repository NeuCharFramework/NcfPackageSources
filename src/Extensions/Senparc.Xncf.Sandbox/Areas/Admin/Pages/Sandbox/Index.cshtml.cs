using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Service;
using Senparc.Xncf.Sandbox.Abstractions;
using Senparc.Xncf.Sandbox.Domain.Services;
using System.Globalization;

namespace Senparc.Xncf.Sandbox.Areas.Sandbox.Pages;

public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
{
    private readonly SandboxOrchestrator _orchestrator;

    public Index(Lazy<XncfModuleService> xncfModuleService, SandboxOrchestrator orchestrator)
        : base(xncfModuleService)
    {
        _orchestrator = orchestrator;
    }

    public IReadOnlyList<SandboxSessionInfo> Sessions { get; private set; } = Array.Empty<SandboxSessionInfo>();

    public string DocsUrl { get; private set; } = SandboxDocsLinks.EnvironmentSetupZh;

    public async Task OnGetAsync()
    {
        DocsUrl = SandboxDocsLinks.GetEnvironmentSetupUrl(CultureInfo.CurrentUICulture.Name);
        Sessions = await _orchestrator.ListAsync().ConfigureAwait(false);
    }

    public async Task<IActionResult> OnGetStateAsync()
    {
        var sessions = await _orchestrator.ListAsync().ConfigureAwait(false);
        return new JsonResult(new
        {
            success = true,
            templates = _orchestrator.ListTemplates().Select(z => new
            {
                z.Key,
                z.DisplayName,
                z.Interactive,
                runtime = z.PreferredRuntime.ToString(),
                z.DefaultMemoryMb
            }),
            sessions
        });
    }

    public async Task<IActionResult> OnPostDestroyAsync([FromForm] string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            await _orchestrator.DestroyAsync(sessionId.Trim()).ConfigureAwait(false);
        }

        return new JsonResult(new { success = true });
    }
}

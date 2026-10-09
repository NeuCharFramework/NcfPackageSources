/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File: Index.cshtml.cs
    Description: Local fine-tuning administration page.
    Created: Senparc - 20261002
----------------------------------------------------------------*/

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Service;
using Senparc.Xncf.AIKernel.Domain.Services;

namespace Senparc.Xncf.AIKernel.Areas.AIFineTuning.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly FineTuningWorkerClient _worker;
        private readonly ILogger<Index> _logger;

        public Index(Lazy<XncfModuleService> xncfModuleService, FineTuningWorkerClient worker,
            ILogger<Index> logger) : base(xncfModuleService)
        {
            _worker = worker;
            _logger = logger;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnGetArtifactAsync(string workerAlias, string id, string artifactId)
        {
            try
            {
                var job = await _worker.GetJobAsync(workerAlias, id, HttpContext.RequestAborted);
                var artifact = job.Artifacts.SingleOrDefault(item => item.Id == artifactId)
                    ?? throw new NcfExceptionBase("The requested training artifact does not exist.");
                var response = await _worker.DownloadArtifactAsync(workerAlias, id, artifactId, HttpContext.RequestAborted);
                Response.RegisterForDispose(response);
                var stream = await response.Content.ReadAsStreamAsync(HttpContext.RequestAborted);
                Response.Headers.CacheControl = "private, no-store";
                Response.Headers.XContentTypeOptions = "nosniff";
                _logger.LogInformation("Fine-tuning artifact downloaded: job={JobId}, artifact={ArtifactId}, actor={Actor}, trace={Trace}",
                    $"{workerAlias}/{id}", artifactId, User.Identity?.Name, HttpContext.TraceIdentifier);
                return new FileStreamResult(stream, "application/octet-stream")
                {
                    FileDownloadName = System.IO.Path.GetFileName(artifact.Name),
                    EnableRangeProcessing = false
                };
            }
            catch (NcfExceptionBase ex)
            {
                _logger.LogWarning(ex, "Fine-tuning artifact download failed for {JobId}", id);
                return new ObjectResult(new { success = false, errorMessage = ex.Message }) { StatusCode = 502 };
            }
        }
    }
}

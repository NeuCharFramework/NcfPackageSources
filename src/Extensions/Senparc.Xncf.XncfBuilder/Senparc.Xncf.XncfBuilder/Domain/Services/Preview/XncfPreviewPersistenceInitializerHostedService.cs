/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfPreviewPersistenceInitializerHostedService.cs
    文件功能描述：在主站就绪后异步恢复 XNCF 预览持久化状态


    创建标识：Senparc - 20260813

    修改标识：Senparc - 20260813
    修改描述：v0.40.0-preview10 增强 XncfBuilder 预览状态持久化与后台初始化

----------------------------------------------------------------*/

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.Domain.Services.Preview
{
    /// <summary>
    /// Defers preview-history hydration until the host has started so database I/O from this
    /// optional module never delays the main site's readiness.
    /// </summary>
    public sealed class XncfPreviewPersistenceInitializerHostedService : BackgroundService
    {
        private readonly XncfPreviewService _previewService;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ILogger<XncfPreviewPersistenceInitializerHostedService> _logger;

        public XncfPreviewPersistenceInitializerHostedService(
            XncfPreviewService previewService,
            IHostApplicationLifetime applicationLifetime,
            ILogger<XncfPreviewPersistenceInitializerHostedService> logger = null)
        {
            _previewService = previewService;
            _applicationLifetime = applicationLifetime;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var applicationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using var registration = _applicationLifetime.ApplicationStarted.Register(
                    static state => ((TaskCompletionSource)state).TrySetResult(),
                    applicationStarted);
                await applicationStarted.Task.WaitAsync(stoppingToken).ConfigureAwait(false);

                await _previewService.InitializePersistenceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Host shutdown cancels the optional initialization work.
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize XNCF preview persistence after host startup.");
            }
        }
    }
}

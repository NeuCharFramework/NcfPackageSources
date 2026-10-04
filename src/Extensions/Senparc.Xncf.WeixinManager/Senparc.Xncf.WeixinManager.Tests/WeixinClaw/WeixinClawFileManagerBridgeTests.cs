using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.FileManager.Abstractions;
using Senparc.Xncf.WeixinManager.WeixinClaw;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Tests.WeixinClaw;

[TestClass]
public class WeixinClawFileManagerBridgeTests
{
    [TestMethod]
    public async Task ImportAsync_StoresReturnedFileId()
    {
        var requestClient = new StubRequestClient(request =>
            new FileManagerImportCompletedEvent(
                request.RequestId,
                request.TenantId,
                request.AccountId,
                request.MessageId,
                request.StorageKey,
                true,
                null,
                new FileManagerImportResult(
                    42,
                    request.FileName,
                    request.ContentType,
                    request.Content.LongLength,
                    request.ResourceKind.ToString())));
        var bridge = new WeixinClawFileManagerBridge(
            requestClient,
            NullLogger<WeixinClawFileManagerBridge>.Instance);
        var media = new WeixinClawStoredMedia
        {
            Name = "invoice.pdf",
            ContentType = "application/pdf",
            StorageKey = "media/account-9/message-1/invoice.pdf"
        };

        await bridge.ImportAsync(
            7,
            9,
            "message-1",
            media,
            new byte[] { 1, 2, 3 });

        Assert.AreEqual(42, media.FileManagerFileId);
        Assert.IsNull(media.FileManagerError);
        Assert.AreEqual(7, requestClient.LastRequest.TenantId);
        Assert.AreEqual(9, requestClient.LastRequest.AccountId);
        Assert.AreEqual("message-1", requestClient.LastRequest.MessageId);
        Assert.AreEqual("media/account-9/message-1/invoice.pdf", requestClient.LastRequest.StorageKey);
        Assert.AreEqual(FileManagerResourceKind.PrivateAttachment, requestClient.LastRequest.ResourceKind);
    }

    [TestMethod]
    public async Task ImportAsync_RecordsFailureWithoutFailingMessagePersistence()
    {
        var requestClient = new StubRequestClient(request =>
            new FileManagerImportCompletedEvent(
                request.RequestId,
                request.TenantId,
                request.AccountId,
                request.MessageId,
                request.StorageKey,
                false,
                "FileManager is unavailable.",
                null));
        var bridge = new WeixinClawFileManagerBridge(
            requestClient,
            NullLogger<WeixinClawFileManagerBridge>.Instance);
        var media = new WeixinClawStoredMedia
        {
            Name = "photo.jpg",
            ContentType = "image/jpeg",
            StorageKey = "media/account-5/message-2/photo.jpg"
        };

        await bridge.ImportAsync(
            3,
            5,
            "message-2",
            media,
            new byte[] { 4, 5, 6 });

        Assert.IsNull(media.FileManagerFileId);
        Assert.AreEqual("FileManager is unavailable.", media.FileManagerError);
    }

    private sealed class StubRequestClient : IEventBusRequestClient
    {
        private readonly Func<FileManagerImportRequestedEvent, FileManagerImportCompletedEvent> _responseFactory;

        public StubRequestClient(
            Func<FileManagerImportRequestedEvent, FileManagerImportCompletedEvent> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public FileManagerImportRequestedEvent LastRequest { get; private set; }

        public Task<TResponse> RequestAsync<TResponse>(
            IIntegrationRequest<TResponse> request,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
            where TResponse : class, IIntegrationResponse
        {
            LastRequest = (FileManagerImportRequestedEvent)(object)request;
            return Task.FromResult((TResponse)(object)_responseFactory(LastRequest));
        }
    }
}

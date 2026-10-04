using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Ncf.Shared.Abstractions.Events;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Services;
using Senparc.Xncf.WeixinManager.WeixinClaw;
using ClawPage = Senparc.Xncf.WeixinManager.Areas.Admin.Pages.WeixinManager.WeixinClaw.IndexModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Tests.WeixinClaw;

[TestClass]
public class WeixinClawSendPipelineTests
{
    [TestMethod]
    public async Task SendText_RetZeroWithoutServerIdPersistsSubmittedRecord()
    {
        using var fixture = new Fixture();

        var result = await fixture.Text.SendTextWithResultAsync(1, null, "hello");

        Assert.IsNull(result.MessageId);
        Assert.AreEqual("user@im.wechat", result.TargetUserId);
        Assert.IsTrue(result.ContextTokenUsed);
        Assert.AreEqual("sent", fixture.Records.Single().Status);
        Assert.IsNull(fixture.Records.Single().MessageId);
        using var json = JsonDocument.Parse(fixture.Handler.Requests.Single().Body);
        Assert.AreEqual(" ctx+/= ", json.RootElement.GetProperty("msg").GetProperty("context_token").GetString());
    }

    [TestMethod]
    public async Task SendText_BusinessFailurePersistsFailedRecord()
    {
        using var fixture = new Fixture();
        fixture.Handler.SendResponse = """{"ret":-14,"errmsg":"expired context"}""";

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            fixture.Text.SendTextWithResultAsync(1, null, "hello"));

        Assert.AreEqual("failed", fixture.Records.Single().Status);
        StringAssert.Contains(fixture.Records.Single().Error, "expired context");
    }

    [TestMethod]
    public async Task SendText_UsesSelectedInboundRecipientContextAndRunId()
    {
        using var fixture = new Fixture();
        await fixture.RecordService.AddInboundAsync(
            1, "inbound", 7, "older@im.wechat", "bot@im.bot",
            fixture.AccountService.ProtectContextToken("older-context"), "older-run",
            1, 2, "hello", DateTime.UtcNow);

        var result = await fixture.Text.SendTextWithResultAsync(
            1, "user@im.wechat", "reply", replyToRecordId: 1);

        Assert.AreEqual("older@im.wechat", result.TargetUserId);
        using var json = JsonDocument.Parse(fixture.Handler.Requests.Single().Body);
        var message = json.RootElement.GetProperty("msg");
        Assert.AreEqual("older-context", message.GetProperty("context_token").GetString());
        Assert.AreEqual("older-run", message.GetProperty("run_id").GetString());
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Send_InvalidReplyRecordDoesNotFallBackToLatestConversation(bool media)
    {
        using var fixture = new Fixture();

        if (media)
        {
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                fixture.Media.SendAsync(1, "test.txt", "text/plain", [1], WeixinClawMediaKind.File,
                    replyToRecordId: 999));
        }
        else
        {
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                fixture.Text.SendTextWithResultAsync(1, null, "hello", replyToRecordId: 999));
        }

        Assert.AreEqual(0, fixture.Handler.Requests.Count);
        Assert.AreEqual(0, fixture.Records.Count);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Send_DoesNotReuseAnotherRecipientsContext(bool media)
    {
        using var fixture = new Fixture();

        if (media)
        {
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                fixture.Media.SendAsync(1, "test.txt", "text/plain", [1], WeixinClawMediaKind.File,
                    toUserId: "other@im.wechat"));
        }
        else
        {
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                fixture.Text.SendTextWithResultAsync(1, "other@im.wechat", "hello"));
        }

        Assert.AreEqual(0, fixture.Handler.Requests.Count);
    }

    [DataTestMethod]
    [DataRow(WeixinClawMediaKind.Image, "image_item", 2, 16, 32)]
    [DataRow(WeixinClawMediaKind.Image, "image_item", 2, 17, 32)]
    [DataRow(WeixinClawMediaKind.File, "file_item", 4, 17, 32)]
    [DataRow(WeixinClawMediaKind.File, "file_item", 4, 32, 48)]
    [DataRow(WeixinClawMediaKind.Voice, "voice_item", 3, 17, 32)]
    public async Task SendMedia_UsesOfficialHexBase64KeyAndExactPlaintextAndCiphertextSizes(
        WeixinClawMediaKind kind, string itemName, int itemType, int rawSize, int encryptedSize)
    {
        using var fixture = new Fixture();
        var plaintext = Enumerable.Range(0, rawSize).Select(value => (byte)value).ToArray();

        var result = await fixture.Media.SendAsync(
            1, kind == WeixinClawMediaKind.Voice ? "test.mp3" : "test.bin",
            kind == WeixinClawMediaKind.Voice ? "audio/mpeg" : "application/octet-stream",
            plaintext, kind);

        Assert.IsNull(result.MessageId);
        Assert.AreEqual("sent", fixture.Records.Single().Status);
        using var uploadJson = JsonDocument.Parse(fixture.Handler.Requests[0].Body);
        var upload = uploadJson.RootElement;
        Assert.AreEqual((int)kind, upload.GetProperty("media_type").GetInt32());
        Assert.AreEqual(rawSize, upload.GetProperty("rawsize").GetInt32());
        Assert.AreEqual(encryptedSize, upload.GetProperty("filesize").GetInt32());
        Assert.AreEqual(Convert.ToHexString(MD5.HashData(plaintext)).ToLowerInvariant(),
            upload.GetProperty("rawfilemd5").GetString());
        var hexKey = upload.GetProperty("aeskey").GetString();
        Assert.AreEqual(32, hexKey.Length);
        Assert.AreEqual(hexKey.ToLowerInvariant(), hexKey);
        var encrypted = fixture.Handler.Requests[1].Body;
        Assert.AreEqual(encryptedSize, encrypted.Length);
        using var aes = Aes.Create();
        aes.Key = Convert.FromHexString(hexKey);
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        using var decryptor = aes.CreateDecryptor();
        CollectionAssert.AreEqual(plaintext, decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length));

        using var sendJson = JsonDocument.Parse(fixture.Handler.Requests[2].Body);
        var message = sendJson.RootElement.GetProperty("msg");
        Assert.AreEqual(2, message.GetProperty("message_type").GetInt32());
        Assert.AreEqual(" ctx+/= ", message.GetProperty("context_token").GetString());
        var item = message.GetProperty("item_list")[0];
        Assert.AreEqual(itemType, item.GetProperty("type").GetInt32());
        var content = item.GetProperty(itemName);
        var media = content.GetProperty("media");
        Assert.AreEqual(hexKey, Encoding.ASCII.GetString(
            Convert.FromBase64String(media.GetProperty("aes_key").GetString())));
        Assert.AreEqual("download-param", media.GetProperty("encrypt_query_param").GetString());
        if (kind == WeixinClawMediaKind.Image)
        {
            Assert.AreEqual(encryptedSize, content.GetProperty("mid_size").GetInt32());
        }
        else if (kind == WeixinClawMediaKind.File)
        {
            Assert.AreEqual(JsonValueKind.String, content.GetProperty("len").ValueKind);
            Assert.AreEqual(rawSize.ToString(CultureInfo.InvariantCulture), content.GetProperty("len").GetString());
        }
        else
        {
            Assert.AreEqual(7, content.GetProperty("encode_type").GetInt32());
            Assert.IsFalse(content.TryGetProperty("sample_rate", out _));
            Assert.IsFalse(content.TryGetProperty("bits_per_sample", out _));
            Assert.IsFalse(content.TryGetProperty("playtime", out _));
        }
    }

    [DataTestMethod]
    [DataRow("test.wav")]
    [DataRow("fake-silk.wav")]
    [DataRow("fake-mpeg.wav")]
    public async Task SendVoice_RejectsUnsupportedEncodingBeforeUploading(string fileName)
    {
        using var fixture = new Fixture();

        var error = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            fixture.Media.SendAsync(1, fileName, "audio/wav", [1], WeixinClawMediaKind.Voice));

        StringAssert.Contains(error.Message, "SILK");
        Assert.AreEqual(0, fixture.Handler.Requests.Count);
    }

    [TestMethod]
    public async Task PollAccount_PreservesReceivedContextAfterSavingCursorAndCanReply()
    {
        using var fixture = new Fixture();
        using var stopping = new CancellationTokenSource();
        var pollCount = 0;
        fixture.Handler.GetUpdates = () =>
        {
            if (++pollCount > 1)
            {
                stopping.Cancel();
                throw new OperationCanceledException(stopping.Token);
            }
            return """
                   {"ret":0,"get_updates_buf":"next","msgs":[{
                     "message_id":"inbound-1","seq":1,"message_type":1,"message_state":2,
                     "from_user_id":"phone@im.wechat","to_user_id":"bot@im.bot",
                     "context_token":" phone-context ","item_list":[{"type":1,"text_item":{"text":"hello"}}]
                   }]}
                   """;
        };
        var tenantScopes = new Mock<IBackgroundTenantScopeFactory>();
        tenantScopes.Setup(factory => factory.TryCreateScopeAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => fixture.Services.CreateScope());
        using var poller = new WeixinClawHostedService(
            fixture.Services.GetRequiredService<IServiceScopeFactory>(),
            tenantScopes.Object, Options.Create(new WeixinClawHostedServiceOptions()),
            NullLogger<WeixinClawHostedService>.Instance);
        var runAccount = typeof(WeixinClawHostedService).GetMethod(
            "RunAccountAsync", BindingFlags.Instance | BindingFlags.NonPublic);

        await ((Task)runAccount.Invoke(poller, [1, 1, stopping.Token])).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual("next", fixture.Account.GetUpdatesBuf);
        Assert.AreEqual("phone@im.wechat", fixture.Account.LastMessageFromUserId);
        Assert.AreEqual(" phone-context ", fixture.AccountService.UnprotectContextToken(fixture.Account));
        Assert.IsNotNull(fixture.Account.LastMessageAt);
        var reply = await fixture.Text.SendTextWithResultAsync(1, null, "reply");
        Assert.AreEqual("phone@im.wechat", reply.TargetUserId);
        Assert.AreEqual("sent", fixture.Records.Last().Status);
    }

    [TestMethod]
    public async Task DownloadInbound_VideoItemIsDecryptedAndStoredAsMp4()
    {
        using var fixture = new Fixture();
        var plaintext = Convert.FromHexString("000000186674797069736f6d00000000");
        var key = Enumerable.Range(0, 16).Select(value => (byte)value).ToArray();
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            using var encryptor = aes.CreateEncryptor();
            fixture.Handler.DownloadBody = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }
        var items = await fixture.Media.DownloadInboundAsync(
            1, 1, "video-1",
            [new WeixinClawMessageItem
            {
                Type = 5,
                VideoItem = new WeixinClawVideoItem
                {
                    Media = new WeixinClawCdnMedia
                    {
                        EncryptQueryParam = "download", AesKey = Convert.ToBase64String(key), EncryptType = 1
                    }
                }
            }]);

        Assert.AreEqual(1, items.Count);
        Assert.AreEqual("video", items[0].Kind);
        Assert.AreEqual("video/mp4", items[0].ContentType);
        Assert.IsNull(items[0].Error);
        Assert.IsTrue(items[0].StorageKey.EndsWith(".mp4", StringComparison.Ordinal));
        Assert.IsTrue(fixture.Storage.TryGetFile(items[0].StorageKey, out var storedPath));
        CollectionAssert.AreEqual(plaintext, await File.ReadAllBytesAsync(storedPath));
    }

    [TestMethod]
    public async Task MediaEndpoint_EnablesSeekingAndRetainsSeparateOriginalDownload()
    {
        using var fixture = new Fixture();
        var bytes = Convert.FromHexString("000000186674797069736f6d00000000");
        var storageKey = await fixture.Storage.SaveAsync(1, "video", 0, "old.bin", bytes);
        var content = "__NCF_WEIXINCLAW_MEDIA__" + JsonSerializer.Serialize(new WeixinClawStoredMessage
        {
            MediaItems = [new() { Kind = "video", StorageKey = storageKey, ContentType = "application/octet-stream" }]
        });
        var record = await fixture.RecordService.AddInboundAsync(
            1, "video", 1, "phone", "bot", null, null, 1, 2, content, DateTime.UtcNow);
        var page = fixture.CreatePage();

        var playable = (PhysicalFileResult)await page.OnGetMediaAsync(record.Id, playback: true);
        var original = (PhysicalFileResult)await page.OnGetMediaAsync(record.Id);

        Assert.AreEqual("video/mp4", playable.ContentType);
        Assert.IsTrue(playable.EnableRangeProcessing);
        Assert.AreEqual("application/octet-stream", original.ContentType);
        Assert.IsTrue(original.EnableRangeProcessing);
        CollectionAssert.AreEqual(bytes, await File.ReadAllBytesAsync(original.FileName));
        Assert.IsInstanceOfType(await page.OnGetMediaAsync(record.Id, index: 9), typeof(NotFoundResult));
        Assert.IsInstanceOfType(await page.OnGetMediaAsync(999), typeof(NotFoundResult));
    }

    private sealed class Fixture : IDisposable
    {
        private readonly ServiceProvider _serviceProvider = new ServiceCollection().BuildServiceProvider();
        private readonly HttpClient _httpClient;
        private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), "ncf-claw-test-" + Guid.NewGuid().ToString("N"));
        public RecordingHandler Handler { get; } = new();
        public WeixinClawAccount Account { get; }
        public WeixinClawAccountService AccountService { get; }
        public List<WeixinClawMessageRecord> Records { get; } = new();
        public WeixinClawMessageRecordService RecordService { get; }
        public WeixinClawMessageService Text { get; }
        public WeixinClawMediaService Media { get; }
        public WeixinClawMediaStorageService Storage { get; }
        public ServiceProvider Services { get; }

        public Fixture()
        {
            var accounts = new List<WeixinClawAccount>();
            AccountService = new WeixinClawAccountService(
                CreateRepository(accounts), _serviceProvider, new EphemeralDataProtectionProvider());
            Account = new WeixinClawAccount(
                "test", "https://ilink.example.test", AccountService.ProtectToken("bot-secret"), null) { Id = 1 };
            Account.SetAuthenticated(Account.BotTokenProtected, "bot@im.bot", "owner@im.wechat", Account.BaseUrl);
            Account.MarkMessageReceived("user@im.wechat", AccountService.ProtectContextToken(" ctx+/= "));
            accounts.Add(Account);
            RecordService = new WeixinClawMessageRecordService(CreateRepository(Records), _serviceProvider);
            _httpClient = new HttpClient(Handler);
            var api = new WeixinClawApi(_httpClient);
            Text = new WeixinClawMessageService(
                AccountService, RecordService, api, NullLogger<WeixinClawMessageService>.Instance);
            var environment = new Mock<IHostEnvironment>();
            environment.SetupGet(value => value.ContentRootPath).Returns(_storageRoot);
            Storage = new WeixinClawMediaStorageService(environment.Object);
            Media = new WeixinClawMediaService(
                AccountService, RecordService, api, Storage,
                new WeixinClawFileManagerBridge(
                    Mock.Of<IEventBusRequestClient>(), NullLogger<WeixinClawFileManagerBridge>.Instance));
            Services = new ServiceCollection()
                .AddSingleton(AccountService).AddSingleton(RecordService).AddSingleton(api).AddSingleton(Media)
                .AddSingleton(new WeixinClawMessageReceiptService(
                    CreateRepository(new List<WeixinClawMessageReceipt>()), _serviceProvider))
                .AddSingleton(new WeixinClawMessageDispatcher(Array.Empty<IWeixinClawMessageHandler>()))
                .BuildServiceProvider();
        }

        public ClawPage CreatePage() => new(
            new Lazy<XncfModuleService>(() => throw new InvalidOperationException("Module discovery is not used in this test.")),
            AccountService, Services.GetRequiredService<WeixinClawMessageReceiptService>(),
            null, Text, RecordService, null, Media, Storage, new WeixinClawMediaPlaybackService(),
            NullLogger<ClawPage>.Instance)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };

        private static IRepositoryBase<T> CreateRepository<T>(List<T> entities) where T : EntityBase<int>
        {
            var repository = new Mock<IRepositoryBase<T>> { DefaultValue = DefaultValue.Mock };
            repository.Setup(value => value.GetFirstOrDefaultObjectAsync(
                    It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<string[]>()))
                .ReturnsAsync((Expression<Func<T, bool>> predicate, string[] _) => entities.FirstOrDefault(predicate.Compile()));
            repository.Setup(value => value.SaveAsync(It.IsAny<T>()))
                .Callback<T>(entity =>
                {
                    if (entity.Id == 0)
                    {
                        entity.Id = entities.Count + 1;
                        entities.Add(entity);
                    }
                }).Returns(Task.CompletedTask);
            return repository.Object;
        }

        public void Dispose()
        {
            Services.Dispose();
            _serviceProvider.Dispose();
            _httpClient.Dispose();
            if (Directory.Exists(_storageRoot))
            {
                Directory.Delete(_storageRoot, recursive: true);
            }
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<(string Path, byte[] Body)> Requests { get; } = new();
        public string SendResponse { get; set; } = """{"ret":0}""";
        public Func<string> GetUpdates { get; set; }
        public byte[] DownloadBody { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add((request.RequestUri.AbsolutePath,
                request.Content == null ? [] : await request.Content.ReadAsByteArrayAsync(cancellationToken)));
            if (request.RequestUri.AbsolutePath == "/c2c/download")
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(DownloadBody) };
            }
            var content = request.RequestUri.AbsolutePath switch
            {
                "/ilink/bot/sendmessage" => SendResponse,
                "/ilink/bot/getuploadurl" => """{"upload_param":"upload-param"}""",
                "/ilink/bot/getupdates" => GetUpdates(),
                "/ilink/bot/msg/notifystart" or "/ilink/bot/msg/notifystop" or "/c2c/upload" => """{"ret":0}""",
                _ => throw new InvalidOperationException("Unexpected test request: " + request.RequestUri.AbsolutePath)
            };
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(content) };
            if (request.RequestUri.AbsolutePath == "/c2c/upload")
            {
                response.Headers.Add("x-encrypted-param", "download-param");
            }
            return response;
        }
    }
}

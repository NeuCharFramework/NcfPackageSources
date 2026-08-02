using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.FirmwareUpdate.Domain.Services;

namespace Senparc.Xncf.FirmwareUpdate.Tests;

[TestClass]
public sealed class NcfPackageMirrorServiceTests
{
    private static readonly string[] RuntimeIdentifiers =
    [
        "linux-arm64",
        "linux-x64",
        "osx-arm64",
        "osx-x64",
        "win-arm64",
        "win-x64"
    ];

    private string _testRoot = null!;

    [TestInitialize]
    public void Initialize()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "ncf-package-mirror-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    [TestMethod]
    public void SelectExpectedAssets_WhenBothPackageKindsExist_SeparatesHostAndDesktop()
    {
        var release = CreateRelease("v-test", includeBothPackageKinds: true);

        var hostAssets = NcfPackageMirrorService.SelectExpectedAssets(
            release,
            NcfPackageMirrorService.HostFeedDefinition);
        var desktopAssets = NcfPackageMirrorService.SelectExpectedAssets(
            release,
            NcfPackageMirrorService.DesktopFeedDefinition);

        Assert.AreEqual(6, hostAssets.Count);
        Assert.AreEqual(6, desktopAssets.Count);
        Assert.IsTrue(hostAssets.All(asset => asset.Name!.StartsWith("ncf-") && !asset.Name.StartsWith("ncf-desktop-")));
        Assert.IsTrue(desktopAssets.All(asset => asset.Name!.StartsWith("ncf-desktop-")));
    }

    [TestMethod]
    public async Task SyncFeedAsync_PublishesHostFolderAndKeepsLegacyJsonSchema()
    {
        const string tag = "v1.2.3-build456";
        var release = CreateRelease(tag, includeBothPackageKinds: false);
        var handler = new ReleaseHandler(
            NcfPackageMirrorService.GitHubReleasesApi,
            release,
            CreateAssetContentMap(release));
        using var client = new HttpClient(handler);
        var service = CreateService(client);
        Directory.CreateDirectory(Path.Combine(_testRoot, NcfPackageMirrorService.DesktopPackageFolderName, "keep-me"));
        Directory.CreateDirectory(Path.Combine(_testRoot, NcfPackageMirrorService.HostPackageFolderName, "remove-me"));

        var result = await service.SyncFeedAsync(
            client,
            _testRoot,
            NcfPackageMirrorService.HostFeedDefinition,
            CancellationToken.None);

        Assert.IsTrue(result.IsComplete, result.Message);
        foreach (var asset in release.Assets!.Where(asset => !asset.Name!.StartsWith("ncf-desktop-")))
        {
            Assert.IsTrue(File.Exists(Path.Combine(
                _testRoot,
                NcfPackageMirrorService.HostPackageFolderName,
                tag,
                asset.Name!)));
        }

        Assert.IsTrue(Directory.Exists(Path.Combine(
            _testRoot,
            NcfPackageMirrorService.DesktopPackageFolderName,
            "keep-me")));
        Assert.IsFalse(Directory.Exists(Path.Combine(
            _testRoot,
            NcfPackageMirrorService.HostPackageFolderName,
            "remove-me")));

        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(
            Path.Combine(_testRoot, NcfPackageMirrorService.LatestReleaseFileName)));
        CollectionAssert.AreEquivalent(
            new[] { "tag_name", "name", "assets" },
            manifest.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
        Assert.AreEqual(6, manifest.RootElement.GetProperty("assets").GetArrayLength());
        Assert.IsTrue(manifest.RootElement.GetProperty("assets")[0]
            .GetProperty("browser_download_url")
            .GetString()!
            .Contains("/NcfPackages/host/", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task SyncFeedAsync_PublishesDesktopFolderAndSeparateManifest()
    {
        const string tag = "desktop-v1.2.3-build456";
        var release = CreateRelease(tag, includeBothPackageKinds: true);
        var handler = new ReleaseHandler(
            NcfPackageMirrorService.NcfDesktopGitHubReleasesApi,
            release,
            CreateAssetContentMap(release));
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        var result = await service.SyncFeedAsync(
            client,
            _testRoot,
            NcfPackageMirrorService.DesktopFeedDefinition,
            CancellationToken.None);

        Assert.IsTrue(result.IsComplete, result.Message);
        var manifestPath = Path.Combine(
            _testRoot,
            NcfPackageMirrorService.LatestDesktopReleaseFileName);
        Assert.IsTrue(File.Exists(manifestPath));
        Assert.IsFalse(File.Exists(Path.Combine(
            _testRoot,
            NcfPackageMirrorService.LatestReleaseFileName)));

        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath));
        Assert.AreEqual(6, manifest.RootElement.GetProperty("assets").GetArrayLength());
        foreach (var asset in manifest.RootElement.GetProperty("assets").EnumerateArray())
        {
            StringAssert.StartsWith(asset.GetProperty("name").GetString(), "ncf-desktop-");
            StringAssert.Contains(
                asset.GetProperty("browser_download_url").GetString(),
                "/NcfPackages/desktop/");
        }
    }

    [TestMethod]
    public async Task SyncFeedAsync_WhenHashIsWrong_PreservesExistingManifest()
    {
        const string oldManifest = "{\"tag_name\":\"old\",\"name\":\"old\",\"assets\":[]}";
        var manifestPath = Path.Combine(_testRoot, NcfPackageMirrorService.LatestReleaseFileName);
        await File.WriteAllTextAsync(manifestPath, oldManifest);
        var release = CreateRelease("v-bad-hash", includeBothPackageKinds: false);
        release.Assets![0].Digest = $"sha256:{new string('0', 64)}";
        var handler = new ReleaseHandler(
            NcfPackageMirrorService.GitHubReleasesApi,
            release,
            CreateAssetContentMap(release));
        using var client = new HttpClient(handler);
        var service = CreateService(client);

        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => service.SyncFeedAsync(
            client,
            _testRoot,
            NcfPackageMirrorService.HostFeedDefinition,
            CancellationToken.None));

        Assert.AreEqual(oldManifest, await File.ReadAllTextAsync(manifestPath));
        Assert.IsFalse(Directory.EnumerateFiles(_testRoot, "*.tmp-*", SearchOption.AllDirectories).Any());
    }

    private static NcfPackageMirrorService CreateService(HttpClient client) => new(
        new StubHttpClientFactory(client),
        NullLogger<NcfPackageMirrorService>.Instance);

    private static NcfPackageMirrorService.GitHubReleaseDto CreateRelease(
        string tag,
        bool includeBothPackageKinds)
    {
        var assets = new List<NcfPackageMirrorService.GitHubAssetDto>();
        foreach (var runtimeIdentifier in RuntimeIdentifiers)
        {
            assets.Add(CreateAsset($"ncf-{runtimeIdentifier}-{tag}.zip"));
            if (includeBothPackageKinds)
            {
                assets.Add(CreateAsset($"ncf-desktop-{runtimeIdentifier}-{tag}.zip"));
            }
        }

        return new NcfPackageMirrorService.GitHubReleaseDto
        {
            TagName = tag,
            Name = $"Release {tag}",
            PublishedAt = DateTime.UtcNow,
            Assets = assets.ToArray()
        };
    }

    private static NcfPackageMirrorService.GitHubAssetDto CreateAsset(string name)
    {
        var content = Encoding.UTF8.GetBytes($"content:{name}");
        return new NcfPackageMirrorService.GitHubAssetDto
        {
            Name = name,
            BrowserDownloadUrl = $"https://github.com/NeuCharFramework/Test/releases/download/v-test/{name}",
            Size = content.Length,
            Digest = $"sha256:{Convert.ToHexStringLower(SHA256.HashData(content))}"
        };
    }

    private static Dictionary<string, byte[]> CreateAssetContentMap(
        NcfPackageMirrorService.GitHubReleaseDto release) =>
        release.Assets!.ToDictionary(
            asset => asset.BrowserDownloadUrl!,
            asset => Encoding.UTF8.GetBytes($"content:{asset.Name}"),
            StringComparer.Ordinal);

    private sealed class StubHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class ReleaseHandler(
        string releasesApi,
        NcfPackageMirrorService.GitHubReleaseDto release,
        IReadOnlyDictionary<string, byte[]> assetContent) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.AbsoluteUri;
            if (url == $"{releasesApi}/latest")
            {
                return JsonResponse(release);
            }

            if (url.StartsWith($"{releasesApi}?", StringComparison.Ordinal))
            {
                return JsonResponse(new[] { release });
            }

            if (assetContent.TryGetValue(url, out var content))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(content)
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static Task<HttpResponseMessage> JsonResponse<T>(T value) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json")
            });
    }
}

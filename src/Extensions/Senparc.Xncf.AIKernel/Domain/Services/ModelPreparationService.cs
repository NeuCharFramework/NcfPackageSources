/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ModelPreparationService.cs
    文件功能描述：准备微调所需模型与数据
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 准备微调所需模型与数据

----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AIKernel.Domain.Models.ModelPreparation;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public sealed class ModelPreparationService : IModelPreparationService
{
    private const long MaxManifestBytes = 16 * 1024 * 1024;
    private readonly HttpClient _http;
    private readonly ModelPreparationOptions _options;
    private readonly string _contentRoot;
    private readonly ILogger<ModelPreparationService> _logger;

    public ModelPreparationService(HttpClient http, IOptions<ModelPreparationOptions> options,
        IHostEnvironment environment, ILogger<ModelPreparationService> logger)
    {
        _http = http;
        _options = options.Value;
        _contentRoot = Path.GetFullPath(environment.ContentRootPath);
        _logger = logger;
    }

    public async Task<ModelPreparationStatus> GetStatusAsync(ModelPreparationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validated = Validate(request);
        var manifest = await GetManifestAsync(validated.Provider, validated.Model, validated.Revision,
            cancellationToken);
        return BuildStatus(validated, manifest);
    }

    public async Task<ModelPreparationResult> DownloadAsync(ModelPreparationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validated = Validate(request);
        var manifest = await GetManifestAsync(validated.Provider, validated.Model, validated.Revision,
            cancellationToken);
        var status = BuildStatus(validated, manifest);
        if (status.IsComplete)
            return new ModelPreparationResult { Status = status, Downloaded = false };

        var provider = validated.ProviderOptions;
        Directory.CreateDirectory(validated.TargetDirectory);
        foreach (var file in manifest.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var destination = ResolveChild(validated.TargetDirectory, file.Path);
            if (File.Exists(destination) && new FileInfo(destination).Length == file.Size)
                continue;

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            var temporary = destination + ".download";
            try
            {
                var uri = BuildProviderUri(provider, validated.Model, validated.Revision, file.Path,
                    provider.FilePathTemplate);
                using var response = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentLength is > 0 and var length &&
                    length != file.Size)
                    throw new NcfExceptionBase($"Model file size mismatch for '{file.Path}'.");
                await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var output = new FileStream(temporary, FileMode.Create, FileAccess.Write,
                    FileShare.None, 128 * 1024, FileOptions.SequentialScan);
                await CopyWithLimitAsync(input, output, file.Size, cancellationToken);
                File.Move(temporary, destination, true);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        return new ModelPreparationResult
        {
            Status = BuildStatus(validated, manifest),
            Downloaded = true
        };
    }

    private PreparedRequest Validate(ModelPreparationRequest request)
    {
        if (!_options.Enabled)
            throw new NcfExceptionBase("Model preparation is disabled.");
        if (request is null || string.IsNullOrWhiteSpace(request.Provider) ||
            string.IsNullOrWhiteSpace(request.Model) || string.IsNullOrWhiteSpace(request.Revision))
            throw new NcfExceptionBase("Provider, model and revision are required.");
        if (!_options.Providers.TryGetValue(request.Provider, out var provider) ||
            !Uri.TryCreate(provider.BaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(baseUri.UserInfo) || !string.IsNullOrEmpty(baseUri.Query) ||
            !string.IsNullOrEmpty(baseUri.Fragment))
            throw new NcfExceptionBase("The provider is not configured with a safe HTTP(S) endpoint.");
        if (!_options.AllowedModels.TryGetValue(request.Provider, out var models) ||
            !models.TryGetValue(request.Model, out var revisions) ||
            !revisions.Contains(request.Revision, StringComparer.Ordinal))
            throw new NcfExceptionBase("The provider, model or revision is not allowlisted.");

        var target = string.IsNullOrWhiteSpace(request.TargetDirectory)
            ? Path.Combine(_contentRoot, "App_Data", "AIKernel", "Models", request.Provider, request.Model,
                request.Revision)
            : Path.GetFullPath(request.TargetDirectory);
        var roots = new[] {
            string.IsNullOrWhiteSpace(_options.ControlledModelRoot) ? null :
                Path.GetFullPath(_options.ControlledModelRoot),
            Path.Combine(_contentRoot, "App_Data", "AIKernel", "Models")
        }.Where(root => root is not null).Select(root => root!).ToArray();
        if (!roots.Any(root => IsUnderRoot(target, root)) ||
            !IsPathFreeOfSymlinks(target, roots.First(root => IsUnderRoot(target, root))))
            throw new NcfExceptionBase("The target directory must be under a controlled model root.");
        if (_options.MaxFileBytes < 1 || _options.MaxTotalBytes < 1 || _options.MaxFiles < 1)
            throw new NcfExceptionBase("Model preparation limits are invalid.");
        return new PreparedRequest(request.Provider, request.Model, request.Revision, target, provider);
    }

    private async Task<ModelManifest> GetManifestAsync(string providerName, string model, string revision,
        CancellationToken cancellationToken)
    {
        var provider = _options.Providers[providerName];
        var uri = BuildProviderUri(provider, model, revision, "", provider.ManifestPathTemplate);
        using var response = await _http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > MaxManifestBytes)
            throw new NcfExceptionBase("The model manifest is too large.");
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var limited = new LimitedReadStream(stream, MaxManifestBytes);
        var manifest = await JsonSerializer.DeserializeAsync<ModelManifest>(limited,
            new JsonSerializerOptions(JsonSerializerDefaults.Web), cancellationToken);
        if (manifest?.Files is null || manifest.Files.Count == 0 ||
            manifest.Files.Count > _options.MaxFiles)
            throw new NcfExceptionBase("The model manifest is invalid or exceeds the file limit.");
        var files = manifest.Files.Select(file => ValidateManifestFile(file)).ToList();
        var total = files.Sum(file => file.Size);
        if (total > _options.MaxTotalBytes)
            throw new NcfExceptionBase("The model exceeds the configured size limit.");
        return new ModelManifest(files);
    }

    private ModelManifestFile ValidateManifestFile(ModelManifestFile file)
    {
        if (string.IsNullOrWhiteSpace(file.Path) || file.Path.StartsWith("/", StringComparison.Ordinal) ||
            file.Path.Contains('\\') || file.Path.Split('/').Any(part => part is "" or "." or "..") ||
            file.Size < 0 || file.Size > _options.MaxFileBytes)
            throw new NcfExceptionBase("The model manifest contains an unsafe file path or size.");
        return file;
    }

    private ModelPreparationStatus BuildStatus(PreparedRequest request, ModelManifest manifest)
    {
        var files = manifest.Files.Select(file =>
        {
            var path = ResolveChild(request.TargetDirectory, file.Path);
            var exists = File.Exists(path) && new FileInfo(path).Length == file.Size;
            return new ModelFileStatus(file.Path, file.Size, exists);
        }).ToList();
        return new ModelPreparationStatus
        {
            Provider = request.Provider,
            Model = request.Model,
            Revision = request.Revision,
            TargetDirectory = request.TargetDirectory,
            IsComplete = files.All(file => file.Exists),
            TotalBytes = files.Sum(file => file.Length),
            Files = files
        };
    }

    private static async Task CopyWithLimitAsync(Stream input, Stream output, long expected,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[128 * 1024];
        long total = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += read;
            if (total > expected)
                throw new NcfExceptionBase("The downloaded model file exceeds its manifest size.");
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }
        if (total != expected)
            throw new NcfExceptionBase("The downloaded model file is incomplete.");
    }

    private static string ResolveChild(string root, string relative)
    {
        var path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsUnderRoot(path, root))
            throw new NcfExceptionBase("The model manifest contains a path traversal.");
        return path;
    }

    private static bool IsUnderRoot(string path, string root) =>
        path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);

    private static bool IsPathFreeOfSymlinks(string path, string root)
    {
        var relative = Path.GetRelativePath(root, path);
        var current = root;
        if (File.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
            return false;
        foreach (var part in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (part is "." or "")
                continue;
            current = Path.Combine(current, part);
            if (File.Exists(current) || Directory.Exists(current))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    return false;
            }
        }
        return true;
    }

    private Uri BuildProviderUri(ModelProviderOptions provider, string model, string revision,
        string path, string template)
    {
        var baseUri = new Uri(provider.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        var relative = template.Replace("{model}", Uri.EscapeDataString(model))
            .Replace("{revision}", Uri.EscapeDataString(revision))
            .Replace("{path}", string.Join("/", path.Split('/').Select(Uri.EscapeDataString)));
        var uri = new Uri(baseUri, relative);
        if (!string.Equals(uri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase) ||
            uri.Scheme != baseUri.Scheme)
            throw new NcfExceptionBase("The provider URL resolved outside its allowlisted host.");
        return uri;
    }

    private sealed record PreparedRequest(string Provider, string Model, string Revision,
        string TargetDirectory, ModelProviderOptions ProviderOptions);
    private sealed record ModelManifest(List<ModelManifestFile> Files);
    private sealed record ModelManifestFile(string Path, long Size);

    private sealed class LimitedReadStream(Stream inner, long limit) : Stream
    {
        private long _read;
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await inner.ReadAsync(buffer, cancellationToken);
            _read += read;
            if (_read > limit) throw new NcfExceptionBase("The model manifest is too large.");
            return read;
        }
        public override bool CanRead => true; public override bool CanSeek => false; public override bool CanWrite => false;
        public override long Length => _read; public override long Position { get => _read; set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

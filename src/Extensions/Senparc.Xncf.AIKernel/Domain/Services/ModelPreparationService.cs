/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ModelPreparationService.cs
    File description: Related implementation.

    Created by: Senparc - 20261014

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add bilingual contextual documentation help with iframe dialog and new-window access

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
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
    private const long MaxSearchBytes = 2 * 1024 * 1024;
    private readonly HttpClient _http;
    private readonly ModelPreparationOptions _options;
    private readonly string _contentRoot;
    private readonly ILogger<ModelPreparationService> _logger;

    /// <summary>
    /// Initializes the model manifest/search/download service.
    /// </summary>
    /// <param name="http">HTTP client used for allowlisted provider downloads.</param>
    /// <param name="options">Model provider, allowlist, root, and size-limit options.</param>
    /// <param name="environment">Host environment used to resolve the default model root.</param>
    /// <param name="logger">Logger for download diagnostics.</param>
    public ModelPreparationService(HttpClient http, IOptions<ModelPreparationOptions> options,
        IHostEnvironment environment, ILogger<ModelPreparationService> logger)
    {
        _http = http;
        _options = options.Value;
        _contentRoot = Path.GetFullPath(environment.ContentRootPath);
        _logger = logger;
    }

    /// <summary>
    /// Reads and validates a provider manifest, then reports which allowlisted files are already present.
    /// </summary>
    /// <param name="request">The provider, model, revision, and target-directory request.</param>
    /// <param name="cancellationToken">Token used to cancel manifest retrieval.</param>
    /// <returns>The current preparation status.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when preparation is disabled, the request is not allowlisted, or the manifest is unsafe.</exception>
    public async Task<ModelPreparationStatus> GetStatusAsync(ModelPreparationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validated = Validate(request);
        var manifest = await GetManifestAsync(validated.Provider, validated.Model, validated.Revision,
            cancellationToken);
        return BuildStatus(validated, manifest);
    }

    /// <summary>
    /// Searches an allowlisted provider catalog using a bounded query and response size.
    /// </summary>
    /// <param name="request">The provider, query, and result-limit request.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>Allowlisted model/revision search results.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when configuration, query limits, provider settings, or response shape is invalid.</exception>
    public async Task<IReadOnlyList<ModelSearchItem>> SearchAsync(ModelSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            throw new NcfExceptionBase("Model preparation is disabled.");
        if (request is null || string.IsNullOrWhiteSpace(request.Provider) ||
            string.IsNullOrWhiteSpace(request.Query))
            throw new NcfExceptionBase("Provider and search query are required.");
        if (request.Query.Length > 100 || request.Limit is < 1 or > 50)
            throw new NcfExceptionBase("Search query must be at most 100 characters and limit must be between 1 and 50.");
        if (!_options.Providers.TryGetValue(request.Provider, out var provider) ||
            string.IsNullOrWhiteSpace(provider.SearchPathTemplate))
            throw new NcfExceptionBase("The provider has no configured safe model catalog.");
        ValidateProvider(provider);

        var searchTemplate = provider.SearchPathTemplate
            .Replace("{query}", Uri.EscapeDataString(request.Query), StringComparison.Ordinal)
            .Replace("{limit}", request.Limit.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
        var uri = BuildProviderUri(provider, "", "", "", searchTemplate);
        using var response = await _http.GetAsync(uri,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is > MaxSearchBytes)
            throw new NcfExceptionBase("The model search response is too large.");
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var limited = new LimitedReadStream(stream, MaxSearchBytes);
        var root = await JsonNode.ParseAsync(limited, cancellationToken: cancellationToken);
        var items = root is JsonArray array ? array : root?["models"] as JsonArray;
        if (items is null)
            throw new NcfExceptionBase("The model search response must be an array or an object with a models array.");

        var results = new List<ModelSearchItem>();
        foreach (var node in items)
        {
            var model = node?["model"]?.GetValue<string>() ?? "";
            var revision = node?["revision"]?.GetValue<string>() ?? "";
            if (string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(revision) ||
                !_options.AllowedModels.TryGetValue(request.Provider, out var models) ||
                !models.TryGetValue(model, out var revisions) ||
                !revisions.Contains(revision, StringComparer.Ordinal))
                continue;
            results.Add(new ModelSearchItem
            {
                Provider = request.Provider,
                Model = model,
                Revision = revision,
                Description = node?["description"]?.GetValue<string>() ?? "",
                SizeBytes = node?["sizeBytes"]?.GetValue<long>() ?? 0
            });
        }
        return results;
    }

    /// <summary>
    /// Downloads missing manifest files into a controlled, symlink-free model directory,
    /// enforcing declared sizes and optional SHA-256 hashes.
    /// </summary>
    /// <param name="request">The provider, model, revision, and target-directory request.</param>
    /// <param name="cancellationToken">Token used to cancel downloads.</param>
    /// <returns>The final preparation status and whether any files were downloaded.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when configuration, manifest, path, size, hash, or provider validation fails.</exception>
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
            if (File.Exists(destination) && new FileInfo(destination).Length == file.Size &&
                await MatchesHashAsync(destination, file.Sha256, cancellationToken))
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
                if (!await MatchesHashAsync(temporary, file.Sha256, cancellationToken))
                    throw new NcfExceptionBase($"Model file hash mismatch for '{file.Path}'.");
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

    /// <summary>
    /// Validates preparation settings, allowlists, controlled roots, symlinks, and size limits.
    /// </summary>
    /// <param name="request">The requested preparation operation.</param>
    /// <returns>A normalized prepared request.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when request, provider, model, path, or limits are invalid.</exception>
    private PreparedRequest Validate(ModelPreparationRequest request)
    {
        if (!_options.Enabled)
            throw new NcfExceptionBase("Model preparation is disabled.");
        if (request is null || string.IsNullOrWhiteSpace(request.Provider) ||
            string.IsNullOrWhiteSpace(request.Model) || string.IsNullOrWhiteSpace(request.Revision))
            throw new NcfExceptionBase("Provider, model and revision are required.");
        if (!_options.Providers.TryGetValue(request.Provider, out var provider))
            throw new NcfExceptionBase("The provider is not configured.");
        ValidateProvider(provider);
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

    /// <summary>
    /// Downloads and validates a provider manifest under the manifest-size and file-count limits.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <param name="model">The allowlisted model.</param>
    /// <param name="revision">The allowlisted revision.</param>
    /// <param name="cancellationToken">Token used to cancel retrieval.</param>
    /// <returns>A task containing the validated manifest.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the manifest is too large, malformed, unsafe, or exceeds quotas.</exception>
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

    /// <summary>
    /// Validates one manifest file path, declared size, and optional SHA-256 digest.
    /// </summary>
    /// <param name="file">The manifest file entry.</param>
    /// <returns>The validated entry.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when path, size, or digest constraints are violated.</exception>
    private ModelManifestFile ValidateManifestFile(ModelManifestFile file)
    {
        if (string.IsNullOrWhiteSpace(file.Path) || file.Path.StartsWith("/", StringComparison.Ordinal) ||
            file.Path.Contains('\\') || file.Path.Split('/').Any(part => part is "" or "." or "..") ||
            file.Size < 0 || file.Size > _options.MaxFileBytes ||
            (_options.RequireFileHashes && string.IsNullOrWhiteSpace(file.Sha256)) ||
            (file.Sha256 is not null && !System.Text.RegularExpressions.Regex.IsMatch(file.Sha256, "^[a-fA-F0-9]{64}$")))
            throw new NcfExceptionBase("The model manifest contains an unsafe file path or size.");
        return file;
    }

    /// <summary>
    /// Compares manifest entries with local files and builds preparation status.
    /// </summary>
    /// <param name="request">The normalized request.</param>
    /// <param name="manifest">The validated manifest.</param>
    /// <returns>The current file-completeness status.</returns>
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

    /// <summary>
    /// Copies a model file while enforcing its exact manifest byte count.
    /// </summary>
    /// <param name="input">The remote input stream.</param>
    /// <param name="output">The destination stream.</param>
    /// <param name="expected">The exact expected byte count.</param>
    /// <param name="cancellationToken">Token used to cancel copying.</param>
    /// <exception cref="NcfExceptionBase">Thrown when the stream is too large or incomplete.</exception>
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

    /// <summary>
    /// Computes a file SHA-256 digest and compares it with an optional expected digest.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <param name="expected">The expected hexadecimal digest.</param>
    /// <param name="cancellationToken">Token used to cancel hashing.</param>
    /// <returns>A task whose result indicates whether the digest matches.</returns>
    private static async Task<bool> MatchesHashAsync(string path, string? expected,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(expected))
            return true;
        await using var stream = File.OpenRead(path);
        var actual = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(actual).Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves a manifest-relative path below its controlled root.
    /// </summary>
    /// <param name="root">The controlled root.</param>
    /// <param name="relative">The manifest-relative path.</param>
    /// <returns>The normalized absolute path.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when path traversal escapes the root.</exception>
    private static string ResolveChild(string root, string relative)
    {
        var path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!IsUnderRoot(path, root))
            throw new NcfExceptionBase("The model manifest contains a path traversal.");
        return path;
    }

    /// <summary>
    /// Determines whether an absolute path is equal to or below a root.
    /// </summary>
    /// <param name="path">The candidate path.</param>
    /// <param name="root">The root path.</param>
    /// <returns><see langword="true"/> when the path is contained by the root.</returns>
    private static bool IsUnderRoot(string path, string root) =>
        path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks the existing path chain for reparse points/symbolic links.
    /// </summary>
    /// <param name="path">The target path.</param>
    /// <param name="root">The controlled root.</param>
    /// <returns><see langword="true"/> when no existing path component is a reparse point.</returns>
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

    /// <summary>
    /// Expands a provider URL template and verifies the resolved host and scheme remain allowlisted.
    /// </summary>
    /// <param name="provider">The provider options.</param>
    /// <param name="model">The model name.</param>
    /// <param name="revision">The model revision.</param>
    /// <param name="path">The file path parameter.</param>
    /// <param name="template">The provider URL template.</param>
    /// <returns>The validated provider URI.</returns>
    /// <exception cref="NcfExceptionBase">Thrown when the resolved URI escapes the provider endpoint.</exception>
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

    /// <summary>
    /// Validates that a provider endpoint is an absolute HTTP(S) URI without credentials or fragments.
    /// </summary>
    /// <param name="provider">The provider options.</param>
    /// <exception cref="NcfExceptionBase">Thrown when the endpoint is unsafe.</exception>
    private static void ValidateProvider(ModelProviderOptions provider)
    {
        if (!Uri.TryCreate(provider.BaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(baseUri.UserInfo) || !string.IsNullOrEmpty(baseUri.Query) ||
            !string.IsNullOrEmpty(baseUri.Fragment))
            throw new NcfExceptionBase("The provider is not configured with a safe HTTP(S) endpoint.");
    }

    private sealed record PreparedRequest(string Provider, string Model, string Revision,
        string TargetDirectory, ModelProviderOptions ProviderOptions);
    private sealed record ModelManifest(List<ModelManifestFile> Files);
    private sealed record ModelManifestFile(string Path, long Size, string? Sha256 = null);

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

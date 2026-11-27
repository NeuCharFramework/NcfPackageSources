/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxImageResolver.cs
    File description: Related implementation.

    Created by: Senparc - 20260808

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.4.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.Extensions.Options;

namespace Senparc.Xncf.Sandbox.Domain.Services.Runtime;

public interface ISandboxImageResolver
{
    string Resolve(string templateKey, string defaultImage);

    bool HasRegistryPrefix { get; }

    string? RegistryPrefix { get; }
}

public sealed class SandboxImageResolver : ISandboxImageResolver
{
    private readonly SandboxImageOptions _options;

    /// <summary>
    /// Initializes an image resolver from bound options.
    /// </summary>
    /// <param name="options">The configured sandbox image options.</param>
    public SandboxImageResolver(IOptions<SandboxImageOptions> options)
    {
        _options = options?.Value ?? new SandboxImageOptions();
    }

    /// <summary>
    /// Initializes an image resolver from direct options.
    /// </summary>
    /// <param name="options">The sandbox image options.</param>
    public SandboxImageResolver(SandboxImageOptions options)
    {
        _options = options ?? new SandboxImageOptions();
    }

    public bool HasRegistryPrefix => !string.IsNullOrWhiteSpace(_options.RegistryPrefix);

    public string? RegistryPrefix => string.IsNullOrWhiteSpace(_options.RegistryPrefix)
        ? null
        : _options.RegistryPrefix.Trim().TrimEnd('/');

    /// <summary>
    /// Resolves a template image override and applies an optional registry prefix.
    /// </summary>
    /// <param name="templateKey">The sandbox template key.</param>
    /// <param name="defaultImage">The default image name.</param>
    /// <returns>The final image reference.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="defaultImage"/> is blank.</exception>
    public string Resolve(string templateKey, string defaultImage)
    {
        if (string.IsNullOrWhiteSpace(defaultImage))
        {
            throw new ArgumentException("默认镜像不能为空。", nameof(defaultImage));
        }

        if (_options.Overrides != null
            && !string.IsNullOrWhiteSpace(templateKey)
            && _options.Overrides.TryGetValue(templateKey, out var overrideImage)
            && !string.IsNullOrWhiteSpace(overrideImage))
        {
            return overrideImage.Trim();
        }

        var prefix = RegistryPrefix;
        if (prefix == null)
        {
            return defaultImage.Trim();
        }

        var image = defaultImage.Trim();
        if (image.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase)
            || image.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return image;
        }

        // python:3.12-alpine -> {prefix}/python:3.12-alpine
        //Legacy comment translated to English.
        var leaf = image.Contains('/')
            ? image[(image.LastIndexOf('/') + 1)..]
            : image;
        return $"{prefix}/{leaf}";
    }
}

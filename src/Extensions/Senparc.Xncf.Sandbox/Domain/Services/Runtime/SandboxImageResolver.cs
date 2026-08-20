/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：SandboxImageResolver.cs
    文件功能描述：将模板默认镜像解析为最终 docker image 引用

    创建标识：Senparc - 20260808

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

    public SandboxImageResolver(IOptions<SandboxImageOptions> options)
    {
        _options = options?.Value ?? new SandboxImageOptions();
    }

    /// <summary>供单元测试直接构造。</summary>
    public SandboxImageResolver(SandboxImageOptions options)
    {
        _options = options ?? new SandboxImageOptions();
    }

    public bool HasRegistryPrefix => !string.IsNullOrWhiteSpace(_options.RegistryPrefix);

    public string? RegistryPrefix => string.IsNullOrWhiteSpace(_options.RegistryPrefix)
        ? null
        : _options.RegistryPrefix.Trim().TrimEnd('/');

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
        // mcr.microsoft.com/dotnet/sdk:10.0 -> {prefix}/sdk:10.0（更推荐用 Overrides 写全名）
        var leaf = image.Contains('/')
            ? image[(image.LastIndexOf('/') + 1)..]
            : image;
        return $"{prefix}/{leaf}";
    }
}

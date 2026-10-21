/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ModelPreparationOptions.cs
    文件功能描述：模型准备配置
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 配置模型准备参数

----------------------------------------------------------------*/

#nullable enable
using System;
using System.Collections.Generic;

namespace Senparc.Xncf.AIKernel.Domain.Services;

public sealed class ModelPreparationOptions
{
    public const string SectionName = "SenparcXncfAIKernel:ModelPreparation";
    public bool Enabled { get; set; }
    public string ControlledModelRoot { get; set; } = "";
    public long MaxFileBytes { get; set; } = 4L * 1024 * 1024 * 1024;
    public long MaxTotalBytes { get; set; } = 8L * 1024 * 1024 * 1024;
    public int MaxFiles { get; set; } = 4096;
    public Dictionary<string, ModelProviderOptions> Providers { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, Dictionary<string, string[]>> AllowedModels { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ModelProviderOptions
{
    public string BaseUrl { get; set; } = "";
    public string ManifestPathTemplate { get; set; } =
        "models/{model}/resolve/{revision}/manifest.json";
    public string FilePathTemplate { get; set; } =
        "models/{model}/resolve/{revision}/{path}";
}

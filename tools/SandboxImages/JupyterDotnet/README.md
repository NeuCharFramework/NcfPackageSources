# NCF Jupyter + .NET 镜像

此目录构建 `jupyter-csharp` 使用的独立镜像。镜像基于 Jupyter Minimal Notebook，增加：

- .NET 10 SDK；
- Microsoft .NET Interactive Jupyter Kernel；
- 构建时预热的 `CsvHelper`、`MathNet.Numerics`、`Newtonsoft.Json` NuGet 包。

镜像不会打进 NCF NuGet 包，也不会由 NCF Web 进程在运行时安装依赖。

## 本机构建

```bash
cd tools/SandboxImages/JupyterDotnet
docker build -t ncf-jupyter-dotnet:10.0 .
docker run --rm ncf-jupyter-dotnet:10.0 dotnet --info
docker run --rm ncf-jupyter-dotnet:10.0 jupyter kernelspec list
```

默认基础镜像使用国内网络中的第三方 Quay 代理。也可以切换回官方地址：

```bash
docker build \
  --build-arg JUPYTER_BASE_IMAGE=quay.io/jupyter/minimal-notebook:latest \
  -t ncf-jupyter-dotnet:10.0 .
```

如果构建机器访问 NuGet 官方源较慢，可以指定组织内部 NuGet 源：

```bash
docker build \
  --build-arg NUGET_SOURCE=https://your-nuget-feed/v3/index.json \
  -t ncf-jupyter-dotnet:10.0 .
```

## NCF 配置

本机测试可以直接使用：

```json
{
  "SenparcXncfSandbox": {
    "Images": {
      "Overrides": {
        "jupyter-csharp": "ncf-jupyter-dotnet:10.0"
      }
    }
  }
}
```

多台服务器或生产环境应推送到私有 Registry，并把 `jupyter-csharp` 改成完整仓库地址。

## Notebook 中测试

创建 `JupyterLab (C#)` 沙箱后，在 Kernel 列表中选择 C#，可以运行：

```csharp
using Newtonsoft.Json;

var value = new { Name = "NCF", Enabled = true };
Console.WriteLine(JsonConvert.SerializeObject(value));
```

包版本和清单由 `Learning.csproj` 管理；修改后需要重新构建镜像。

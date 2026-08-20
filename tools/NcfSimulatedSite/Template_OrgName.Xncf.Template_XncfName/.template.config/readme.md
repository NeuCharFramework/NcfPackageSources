## 说明

此文件夹存放 XNCF `dotnet new` 模板配置，并随权威母版同步到模板 NuGet 项目。

项目地址：https://github.com/dotnet/templating

关于排除文件夹：https://github.com/dotnet/templating/issues/850#issuecomment-303870563

## 手动验证模板

```bash
dotnet new install ./
dotnet new XNCF -n MyOrg.Xncf.MyNewProject --force \
  --IntegrationToNcf true --Sample true --Function true --Web true --Database true \
  --OrgName MyOrg --XncfName MyNewProject \
  --Guid C4BC0C46-8438-4EEE-94F5-88C5B7731227 \
  --Icon "fa fa-star" --Description "模块的说明" --Version 0.2 --MenuName "使用模板生成"
```

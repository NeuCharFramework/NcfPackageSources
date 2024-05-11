<img src="https://weixin.senparc.com/images/NCF/logo.png" width="300" />

# NcfPackageSources

|              |    .NET Core    |     CI/CD
|--------------|-----------------|---------------
|  Basic       | [![Build Status](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_apis/build/status/NeuCharFramework.NcfPackageSources?branchName=master)](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_build/latest?definitionId=48&branchName=master)
|  Extensions  | [![Build Status](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_apis/build/status/NeuCharFramework.NcfPackageSources?branchName=master)](https://mysenparc.visualstudio.com/NCF-and-PackageResources/_build/latest?definitionId=48&branchName=master)


## 项目介绍

本项目为 [NCF（NeuCharFramework）](https://github.com/NeuCharFramework/NCF) 模板官方包的核心基础库源码。

当您使用 [NCF](https://github.com/NeuCharFramework/NCF) 模板开发项目时，核心基础库将为您提供一系列基础能力的支撑，通常情况下您无需关心这些库的具体实现，只需要引用即可。

当您需要了解、修改或调试相关基础库时，您可以通过本项目获取源码。

[NCF](https://github.com/NeuCharFramework/NCF) 文档地址：[https://www.ncf.pub/docs/](https://www.ncf.pub/docs/)。

## 文件夹说明

|    文件夹     |    说明         |
|--------------|-----------------|
|  src/Basic       |  必须安装的基础官方库，以 `Separc.Ncf.` 开头
|  src/Extensions  |  可选的扩展包，以 `Senparc.Xncf.` 开头
|  src/Extensions/System  | 系统模块

### [XncfOrder(x)] 特性说明

> `XncfOrder` 特性用于指定模块的加载顺序，为降序排列，数字越大越在前<br>

`0`：默认值，可以不用设置

`1` ~ `5000`：需要预加载的重要模块

`5000` 以上：系统及基础模块，常规模块请勿占用

`59xx`：系统底层基础模块，常规模块请勿占用

`58xx`：AI 相关基础模，常规模块请勿占用块

## 欢迎贡献代码！

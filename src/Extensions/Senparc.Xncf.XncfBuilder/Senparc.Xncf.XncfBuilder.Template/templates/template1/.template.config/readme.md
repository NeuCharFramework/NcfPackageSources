## 说明

此文件夹用于存放 .NET 模板的配置文件

## 资源备忘
项目地址：https://github.com/dotnet/templating

关于排除文件夹的正确做法：https://github.com/dotnet/templating/issues/850#issuecomment-303870563

## 手动生成模板

### 安装模板

> PS E:\ ... \Template_OrgName.Xncf.Template_XncfName> dotnet new -i ./

### 生成模板

> PS E:\ ...\NCF > dotnet new xncf -n MyOrg.Xncf.MyNewProject --force --IntegrationToNcf --UseSample true --UseFunction true --UseWeb true --UseDatabase true --OrgName MyOrg --XncfName MyNewProject --Guid C4BC0C46-8438-4EEE-94F5-88C5B7731227 --Icon "fa fa-star" --Description "模块的说明" --Version 0.2 --MenuName "使用模板生成" 

### 引用项目

> PS E:\ ...\NCF > dotnet add ./Senparc.Web/Senparc.Web.csproj reference MyOrg.Xncf.MyNewProject/MyOrg.Xncf.MyNewProject.csproj

> PS E:\ ...\NCF > dotnet sln .\NCF.sln add MyOrg.Xncf.MyNewProject/MyOrg.Xncf.MyNewProject.csproj


[中文版](readme.cn.md)

﻿## Description

This folder is used to store configuration files for .NET templates

## Resource Memo
Project address: https://github.com/dotnet/templating

About the correct way to exclude folders: https://github.com/dotnet/templating/issues/850#issuecomment-303870563

## Manually generate templates

### Install template

> PS E:\ ... \Template_OrgName.Xncf.Template_XncfName> dotnet new -i ./

### Generate template

> PS E:\ ...\NCF > dotnet new xncf -n MyOrg.Xncf.MyNewProject --force --IntegrationToNcf --UseSample true --UseFunction true --UseWeb true --UseDatabase true --OrgName MyOrg --XncfName MyNewProject --Guid C4BC0C46-8438-4EEE-94F5-88C5B7731227 --Icon "fa fa-star" --Description "Description of module" --Version 0.2 --MenuName "Generate using template"

### Reference project

> PS E:\ ...\NCF > dotnet add ./Senparc.Web/Senparc.Web.csproj reference MyOrg.Xncf.MyNewProject/MyOrg.Xncf.MyNewProject.csproj

> PS E:\ ...\NCF > dotnet sln .\NCF.sln add MyOrg.Xncf.MyNewProject/MyOrg.Xncf.MyNewProject.csproj

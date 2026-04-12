[中文](readme.cn.md)

## Senparc.Ncf.DatabasePlant Module Overview

"Plant" here refers to a "maintenance tarmac" -- it is used when you need to perform maintenance on modules.


### How It Works

Senparc.Ncf.DatabasePlant references all officially implemented NCF database DatabaseConfiguration projects, such as Senparc.Ncf.Database.MySql, Senparc.Ncf.Database.SqlServer, etc.

### Purpose

With references to all database DatabaseConfiguration projects, any project (module) that references Senparc.Ncf.DatabasePlant gains the ability to work with all implemented databases.

However, deploying a bundle of database Providers to production is unnecessary overhead (although it typically does not affect runtime performance). Therefore, we recommend using it only during maintenance (Debug) and excluding it in production. To smoothly switch between these scenarios, add a build condition when referencing Senparc.Ncf.DatabasePlant:

``` XML
<ProjectReference Condition=" '$(Configuration)' != 'Release' " Include="..\..\..\Basic\Senparc.Ncf.DatabasePlant\Senparc.Ncf.DatabasePlant.csproj" />
```

This is exactly the origin of the "tarmac" name: we only let the project sit on the tarmac during Debug, where it can perform batch operations such as database migrations across all databases. When NCF takes off (Release), this package is automatically ignored and adds no extra burden to the system.

## Update Notes

Whenever any `Senparc.Ncf.Database.xx` library version is upgraded, the version of this project should also be upgraded to ensure it always references the latest library versions.
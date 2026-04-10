## Senparc.Web.DatabasePlant Module Overview

"Plant" here refers to a "maintenance tarmac" -- it is used when you need to perform maintenance on modules.

1. This project is used with the `dotnet ef migrations add` command to specify a startup project with a specific target framework (e.g. netcoreapp3.1 or net6.0, net8.0), allowing you to work with projects targeting any framework (e.g. netstandard2.1) without any modifications.

2. This project should not be referenced by the Senparc.Web project, so it will not be deployed to production -- it is only used during development.

3. Projects that need to execute database migration commands must be referenced by this project.


### How It Works

Senparc.Ncf.DatabasePlant references all officially implemented NCF database DatabaseConfiguration projects, such as Senparc.Ncf.Database.MySql, Senparc.Ncf.Database.SqlServer, etc.

### Purpose

With references to all database DatabaseConfiguration projects, any project (module) that references Senparc.Ncf.DatabasePlant gains the ability to work with all implemented databases.

However, deploying a bundle of database Providers to production is unnecessary overhead (although it typically does not affect runtime performance). Therefore, we recommend using it only during maintenance (Debug) and excluding it in production. To smoothly switch between these scenarios, add a build condition when referencing Senparc.Ncf.DatabasePlant:

``` XML
<ProjectReference Condition=" '$(Configuration)' != 'Release' " Include="..\..\..\Basic\Senparc.Ncf.DatabasePlant\Senparc.Ncf.DatabasePlant.csproj" />
```

This is exactly the origin of the "tarmac" name: we only let the project sit on the tarmac during Debug, where it can perform batch operations such as database migrations across all databases. When NCF takes off (Release), this package is automatically ignored and adds no extra burden to the system.

## Manual Operations

When you do not have an NCF project that can be executed directly (primarily the XncfBuilder module), you can use manual command-line operations to generate databases. For example, using SQLite:

```
dotnet ef migrations add Init -c SystemManagerSenparcEntities_Sqlite -s E:\Senparc项目\NeuCharFramework\NcfPackageSources\src\DevelopmentTools\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NcfPackageSources\src\Extensions\System\Senparc.Xncf.SystemManager\Domain\Migrations\Sqlite
```

Using MySQL:

```
dotnet ef migrations add Init -c SystemManagerSenparcEntities_MySql -s E:\Senparc项目\NeuCharFramework\NcfPackageSources\src\DevelopmentTools\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NcfPackageSources\src\Extensions\System\Senparc.Xncf.SystemManager\Domain\Migrations\Mysql
```

Using SQL Server:

```
dotnet ef migrations add Init -c SystemManagerSenparcEntities_SqlServer -s E:\Senparc项目\NeuCharFramework\NcfPackageSources\src\DevelopmentTools\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NcfPackageSources\src\Extensions\System\Senparc.Xncf.SystemManager\Domain\Migrations\SqlServer
```

Using PostgreSQL:

```
dotnet ef migrations add Init -c SystemManagerSenparcEntities_PostgreSQL -s E:\Senparc项目\NeuCharFramework\NcfPackageSources\src\DevelopmentTools\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NcfPackageSources\src\Extensions\System\Senparc.Xncf.SystemManager\Domain\Migrations\PostgreSQL
```

## Update Notes

Whenever any `Senparc.Ncf.Database.xx` library version is upgraded, the version of this project should also be upgraded to ensure it always references the latest library versions.
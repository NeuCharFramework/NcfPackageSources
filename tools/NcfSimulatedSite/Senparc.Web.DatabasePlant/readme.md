[中文版](readme.cn.md)

## Senparc.Web.DatabasePlant project description

Plant means "landing pad", which means you need to use it when you are preparing to "overhaul" the module.

1. This project is used to specify the startup project with a specific target framework (such as netcoreapp3.1 or net6.0) when using the `dotnet ef migrations add` command, so that projects of any target framework (such as netstandard2.1) can be operated without any modification;

2. This project should not be referenced by the Senparc.Web project, so it will not be released to the production environment and will only be used during development;

3. The project that needs to execute the database migration command must be referenced to this project.


### Principle

Senparc.Ncf.DatabasePlant refers to the DatabaseConfiguration projects of all databases officially implemented by NCF, such as: Senparc.Ncf.Database.MySql, Senparc.Ncf.Database.SqlServer, etc.

### Tongtu

Because there are references to DatabaseConfiguration of all databases, this means that once the project (module) references Senparc.Ncf.DatabasePlant, it will have the ability to operate all (already implemented) databases.

But we also know that deploying Providers with a bunch of databases to a production environment is a burden (although it usually has no impact on operating efficiency).
Therefore, we only recommend using it during "debug" and shielding it in the production environment. In order to smoothly switch between the two scenarios, we can add compilation conditions when referencing Senparc.Ncf.DatabasePlant, such as:``` XML
<ProjectReference Condition=" '$(Configuration)' != 'Release' " Include="..\..\..\Basic\Senparc.Ncf.DatabasePlant\Senparc.Ncf.DatabasePlant.csproj" />
```This is also the origin of the name "apron": we only let the project "lay" on the apron when debugging, and can perform various batch operations on the database such as Migration for multiple databases.
When NCF takes off (Release), this package will be automatically ignored and will not bring additional burden to the system.

## Manual operation

When there is no NCF project at hand that can be executed directly (mainly the XncfBuilder module), you can use the manual command line to generate the database, for example, take SQLite as an example:```
dotnet ef migrations add Init -c AdminSenparcEntities_Sqlite -s E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Areas.Admin\Domain\Migrations\Sqlite
```
```
dotnet ef migrations add Init -c AdminSenparcEntities_MySql -s E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Areas.Admin\Domain\Migrations\MySql
```
```
dotnet ef migrations add Init -c AdminSenparcEntities_SqlServer -s E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Areas.Admin\Domain\Migrations\SqlServer
```
```
dotnet ef migrations add Init -c AdminSenparcEntities_PostgreSQL -s E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Web.DatabasePlant -o E:\Senparc项目\NeuCharFramework\NCF\src\back-end\Senparc.Areas.Admin\Domain\Migrations\PostgreSQL
```## Update instructions

When any `Senparc.Ncf.Database.xx` class library version is upgraded, the current project version should also be upgraded to ensure that the latest class library version is referenced at all times.

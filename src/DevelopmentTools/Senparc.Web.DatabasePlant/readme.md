[中文版](readme.cn.md)

## Senparc.Web.DatabasePlant module overview

“Plant” means a maintenance apron (tarmac): you use it when you need to service modules.

1. Use this project as the startup project (with an explicit target framework such as netcoreapp3.1, net6.0, or net8.0) when running `dotnet ef migrations add`, so you can work with projects targeting any framework (for example netstandard2.1) without changing them.

2. Do not reference this project from Senparc.Web; it is not deployed to production and is for development only.

3. Any project that must run database migrations should reference this project.

### How it works

Senparc.Ncf.DatabasePlant references all official NCF database `DatabaseConfiguration` packages, such as Senparc.Ncf.Database.MySql and Senparc.Ncf.Database.SqlServer.

### Purpose

Because all database providers are referenced, any project (module) that references Senparc.Ncf.DatabasePlant can use every supported database.

Deploying many providers to production is unnecessary overhead (usually not a runtime issue). Prefer using this only during Debug and excluding it from production. To switch cleanly, add a conditional `ProjectReference` when referencing Senparc.Ncf.DatabasePlant, for example:

```xml
<ProjectReference Condition=" '$(Configuration)' != 'Release' " Include="..\..\..\Basic\Senparc.Ncf.DatabasePlant\Senparc.Ncf.DatabasePlant.csproj" />
```

That is where the “tarmac” name comes from: during Debug the project parks here so you can batch migrations across databases. After Release, the package is ignored and adds no cost.

## Manual operations

When you have no runnable NCF project (often when using the XncfBuilder module), you can use the CLI. Examples use placeholders; replace with paths on your machine.

SQLite:

```
dotnet ef migrations add Init -c SystemManagerSenparcEntities_Sqlite -s <path-to-Senparc.Web.DatabasePlant> -o <path-to-SystemManager-Domain-Migrations-Sqlite>
```

MySQL:

```
dotnet ef migrations add Init -c SystemManagerSenparcEntities_MySql -s <path-to-Senparc.Web.DatabasePlant> -o <path-to-SystemManager-Domain-Migrations-Mysql>
```

SQL Server:

```
dotnet ef migrations add Init -c SystemManagerSenparcEntities_SqlServer -s <path-to-Senparc.Web.DatabasePlant> -o <path-to-SystemManager-Domain-Migrations-SqlServer>
```

PostgreSQL:

```
dotnet ef migrations add Init -c SystemManagerSenparcEntities_PostgreSQL -s <path-to-Senparc.Web.DatabasePlant> -o <path-to-SystemManager-Domain-Migrations-PostgreSQL>
```

## Version updates

Whenever any `Senparc.Ncf.Database.*` library version changes, bump this project’s version so references stay aligned.

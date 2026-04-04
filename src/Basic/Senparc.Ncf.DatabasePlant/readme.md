## Senparc.Ncf.DatabasePlant project description

Plant means "landing pad", which means you need to use it when you are preparing to "overhaul" the module.


### Principle

Senparc.Ncf.DatabasePlant refers to the DatabaseConfiguration projects of all databases officially implemented by NCF, such as: Senparc.Ncf.Database.MySql, Senparc.Ncf.Database.SqlServer, etc.

### Tongtu

Because there are references to DatabaseConfiguration of all databases, this means that once the project (module) references Senparc.Ncf.DatabasePlant, it will have the ability to operate all (already implemented) databases.

But we also know that deploying Providers with a bunch of databases to a production environment is a burden (although it usually has no impact on operating efficiency).
Therefore, we only recommend using it during "debug" and shielding it in the production environment. In order to smoothly switch between the two scenarios, we can add compilation conditions when referencing Senparc.Ncf.DatabasePlant, such as:

``` XML
<ProjectReference Condition=" '$(Configuration)' != 'Release' " Include="..\..\..\Basic\Senparc.Ncf.DatabasePlant\Senparc.Ncf.DatabasePlant.csproj" />
```

This is also the origin of the name "apron": we only let the project "lay" on the apron when debugging, and can perform various batch operations on the database such as Migration for multiple databases.
When NCF takes off (Release), this package will be automatically ignored and will not bring additional burden to the system.

## Update instructions

when any`Senparc.Ncf.Database.xx`After the class library version is upgraded, the current project version should also be upgraded to ensure that the latest class library version is referenced at any time.
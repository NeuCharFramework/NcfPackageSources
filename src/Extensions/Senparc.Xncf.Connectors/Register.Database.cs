/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Database.cs
    File description: Related implementation.

    Created by: Senparc - 20261111

    Modified by: Senparc - 20261111
    Change description: v0.1.0 register the connector database and table prefix

    Modified by: Senparc - 20261119
    Change description: v0.1.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;

namespace Senparc.Xncf.Connectors;

public partial class Register : IXncfDatabase
{
    public const string DATABASE_PREFIX = "Senparc_Connectors_";

    public string DatabaseUniquePrefix => DATABASE_PREFIX;

    public Type TryGetXncfDatabaseDbContextType
        => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

    /// <summary>
    /// Configures connector entity mappings for the module database.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder.</param>
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
    }

    /// <summary>
    /// Registers connector database services required by the module.
    /// </summary>
    /// <param name="services">The dependency injection service collection.</param>
    public void AddXncfDatabaseModule(IServiceCollection services)
    {
    }
}

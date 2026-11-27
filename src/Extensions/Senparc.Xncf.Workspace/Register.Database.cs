/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Database.cs
    File description: Workspace database registration

    Created by: Senparc - 20261021

    Modified by: Senparc - 20261021
    Change description: v0.1.1 register the Workspace database module

    Modified by: Senparc - 20261103
    Change description: v0.2.2 maintain Workspace database registration

    Modified by: Senparc - 20261119
    Change description: v0.2.9 Localized workspace management and runtime surfaces

    Modified by: Senparc - 20261127
    Change description: v0.2.9.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

namespace Senparc.Xncf.Workspace;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Database;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Database;

public partial class Register : IXncfDatabase
{
    public const string DATABASE_PREFIX = "Senparc_Workspace_";
    public string DatabaseUniquePrefix => DATABASE_PREFIX;
    public Type TryGetXncfDatabaseDbContextType
        => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

    /// <summary>
    /// Applies Workspace entity mappings to the module database model.
    /// The current module uses shared conventions and has no explicit mappings here.
    /// </summary>
    /// <param name="modelBuilder">The Entity Framework Core model builder.</param>
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
    }

    /// <summary>
    /// Registers Workspace database services.
    /// The current module has no additional database services beyond shared infrastructure.
    /// </summary>
    /// <param name="services">The application dependency-injection service collection.</param>
    public void AddXncfDatabaseModule(IServiceCollection services)
    {
    }
}

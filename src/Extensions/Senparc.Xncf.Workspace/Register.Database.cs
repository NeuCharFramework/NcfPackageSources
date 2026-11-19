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

    public void OnModelCreating(ModelBuilder modelBuilder)
    {
    }

    public void AddXncfDatabaseModule(IServiceCollection services)
    {
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Register.Database.cs
    File description: Register.Database related implementation

    Created by: Senparc - 20260504

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.11 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Database;

namespace Senparc.Xncf.FirmwareUpdate;

public partial class Register : IXncfDatabase
{
    public const string DATABASE_PREFIX = "Senparc_FirmwareUpdate_";

    public string DatabaseUniquePrefix => DATABASE_PREFIX;

    public Type TryGetXncfDatabaseDbContextType => MultipleDatabasePool.Instance.GetXncfDbContextType(this);

    /// <summary>
    /// Applies FirmwareUpdate Entity Framework Core model configuration.
    /// The module currently relies on convention-based mapping.
    /// </summary>
    /// <param name="modelBuilder">The model builder supplied by Entity Framework Core.</param>
    public void OnModelCreating(ModelBuilder modelBuilder)
    {
    }

    /// <summary>
    /// Registers additional FirmwareUpdate database services.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    public void AddXncfDatabaseModule(IServiceCollection services)
    {
    }
}

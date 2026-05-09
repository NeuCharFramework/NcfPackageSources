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

    public void OnModelCreating(ModelBuilder modelBuilder)
    {
    }

    public void AddXncfDatabaseModule(IServiceCollection services)
    {
    }
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.Database.cs
    文件功能描述：注册连接器模块数据库上下文
    
    
    创建标识：Senparc - 20261111
    
    修改标识：Senparc - 20261111
    修改描述：v0.1.0 注册连接器数据库与表前缀

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

    public void OnModelCreating(ModelBuilder modelBuilder)
    {
    }

    public void AddXncfDatabaseModule(IServiceCollection services)
    {
    }
}

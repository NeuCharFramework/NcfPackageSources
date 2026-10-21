/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Register.Database.cs
    文件功能描述：工作区模块数据库注册
    
    
    创建标识：Senparc - 20261021
    
    修改标识：Senparc - 20261021
    修改描述：v0.1.1 注册工作区数据库模块

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

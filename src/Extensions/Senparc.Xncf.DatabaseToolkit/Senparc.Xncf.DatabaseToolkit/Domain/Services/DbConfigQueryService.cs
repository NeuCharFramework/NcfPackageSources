/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：DbConfigQueryService.cs
    文件功能描述：DbConfigQueryService 相关实现
    
    
    创建标识：Senparc - 20211012
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.DatabaseToolkit.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DatabaseToolkit.Domain.Services
{
    public class DbConfigQueryService : ClientServiceBase<DbConfig>
    {
        public DbConfigQueryService(IClientRepositoryBase<DbConfig> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        internal async Task<bool> IsAutoBackup()
        {
            var obj = await base.GetObjectAsync(z => true);
            if (obj == null)
            {
                throw new UnSetBackupException();
            }
            return obj.IsAutoBackup();// TO DTO
        }
    }
}

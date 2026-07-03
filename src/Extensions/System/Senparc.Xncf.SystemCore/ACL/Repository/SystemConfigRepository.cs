/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SystemConfigRepository.cs
    文件功能描述：SystemConfigRepository 相关实现
    
    
    创建标识：Senparc - 20211128
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Xncf.SystemCore.ACL.Repository
{
    public interface ISystemConfigRepository : IClientRepositoryBase<SystemConfig>
    {
    }

    public class SystemConfigRepository : ClientRepositoryBase<SystemConfig>, ISystemConfigRepository
    {
        public SystemConfigRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {

        }
    }
}


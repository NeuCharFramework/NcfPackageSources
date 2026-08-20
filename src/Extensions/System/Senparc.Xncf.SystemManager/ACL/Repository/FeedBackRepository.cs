/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FeedBackRepository.cs
    文件功能描述：FeedBackRepository 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Xncf.SystemManager.Domain.DatabaseModel;

namespace Senparc.Xncf.SystemManager.ACL.Repository
{
    public interface IFeedBackRepository : IClientRepositoryBase<FeedBack>
    {
    }

    public class FeedBackRepository : ClientRepositoryBase<FeedBack>, IFeedBackRepository
    {
        public FeedBackRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {

        }
    }
}
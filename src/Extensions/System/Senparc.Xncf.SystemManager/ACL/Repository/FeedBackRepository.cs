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
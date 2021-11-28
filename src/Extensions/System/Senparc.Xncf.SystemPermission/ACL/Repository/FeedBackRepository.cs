using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Xncf.SystemPermission.ACL
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
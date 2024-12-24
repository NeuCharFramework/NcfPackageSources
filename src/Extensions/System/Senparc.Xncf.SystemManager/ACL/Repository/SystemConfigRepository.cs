using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Xncf.SystemManager.ACL.Repository
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


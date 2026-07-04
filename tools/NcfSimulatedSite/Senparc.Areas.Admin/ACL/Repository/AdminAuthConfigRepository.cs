using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL
{
    public interface IAdminAuthConfigRepository : IClientRepositoryBase<AdminAuthConfig>
    {
    }

    public class AdminAuthConfigRepository : ClientRepositoryBase<AdminAuthConfig>, IAdminAuthConfigRepository
    {
        private AdminAuthConfigRepository() : base(null)
        {
        }

        public AdminAuthConfigRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {
        }
    }
}

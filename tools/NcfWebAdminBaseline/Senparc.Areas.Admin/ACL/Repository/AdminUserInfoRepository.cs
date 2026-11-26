using Senparc.Areas.Admin.Domain.Models;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL
{
    public interface IAdminUserInfoRepository : IClientRepositoryBase<AdminUserInfo>
    {
    }

    public class AdminUserInfoRepository : ClientRepositoryBase<AdminUserInfo>, IAdminUserInfoRepository
    {
        /// <summary>Reserved parameterless constructor for repository infrastructure.</summary>
        private AdminUserInfoRepository() : base(null) { }

        /// <summary>Initializes the administrator repository with the NCF database provider.</summary>
        /// <param name="ncfDbData">The database data provider used by the base repository.</param>
        public AdminUserInfoRepository(INcfDbData ncfDbData) : base(ncfDbData)
        {

        }
    }
}


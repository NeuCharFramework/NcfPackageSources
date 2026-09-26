using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL.Repository;

public interface IWeixinClawAdminBindingRepository : IClientRepositoryBase<WeixinClawAdminBinding>
{
}

public sealed class WeixinClawAdminBindingRepository
    : ClientRepositoryBase<WeixinClawAdminBinding>, IWeixinClawAdminBindingRepository
{
    private WeixinClawAdminBindingRepository() : base(null)
    {
    }

    public WeixinClawAdminBindingRepository(INcfDbData ncfDbData) : base(ncfDbData)
    {
    }
}

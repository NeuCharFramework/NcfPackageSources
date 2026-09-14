using Senparc.Areas.Admin.Domain.Models.DatabaseModel;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;

namespace Senparc.Areas.Admin.ACL;

public interface IAdminChatTrajectoryRepository : IClientRepositoryBase<AdminChatTrajectory>
{
}

public class AdminChatTrajectoryRepository : ClientRepositoryBase<AdminChatTrajectory>, IAdminChatTrajectoryRepository
{
    private AdminChatTrajectoryRepository() : base(null) { }

    public AdminChatTrajectoryRepository(INcfDbData ncfDbData) : base(ncfDbData)
    {
    }
}

public interface IAdminChatTrajectoryEventRepository : IClientRepositoryBase<AdminChatTrajectoryEvent>
{
}

public class AdminChatTrajectoryEventRepository : ClientRepositoryBase<AdminChatTrajectoryEvent>, IAdminChatTrajectoryEventRepository
{
    private AdminChatTrajectoryEventRepository() : base(null) { }

    public AdminChatTrajectoryEventRepository(INcfDbData ncfDbData) : base(ncfDbData)
    {
    }
}

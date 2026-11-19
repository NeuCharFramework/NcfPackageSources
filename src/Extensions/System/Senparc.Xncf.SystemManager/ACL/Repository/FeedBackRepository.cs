/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FeedBackRepository.cs
    File description: FeedBackRepository related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.15.14 Updated module behavior, localization metadata, and English source comments

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
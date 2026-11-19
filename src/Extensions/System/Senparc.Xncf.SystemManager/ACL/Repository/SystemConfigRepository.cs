/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SystemConfigRepository.cs
    File description: SystemConfigRepository related implementation

    Created by: Senparc - 20211128

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.15.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

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


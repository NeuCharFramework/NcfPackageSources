/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConfigService.cs
    File description: ConfigService related implementation

    Created by: Senparc - 20211109

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.XncfBuilder.OHS.PL;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Domain.Models.Services
{
    public class ConfigService : ServiceBase<Config>
    {
        public ConfigService(IRepositoryBase<Config> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {

        }
        public ConfigDto UpdateConfig(BuildXncf_BuildRequest request)
        {
            var config = base.GetObject(z => true);
            if (config == null)
            {
                config = new Config(request.SlnFilePath, request.OrgName, request.XncfName, request.Version, request.MenuName, request.Icon, request.Description);
            }
            else
            {
                base.Mapper.Map(request, config);
            }
            base.SaveObject(config);
            return base.Mapper.Map<ConfigDto>(config);
        }
    }
}

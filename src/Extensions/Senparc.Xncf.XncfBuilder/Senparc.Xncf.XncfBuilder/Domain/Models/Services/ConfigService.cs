/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ConfigService.cs
    File description: ConfigService related implementation

    Created by: Senparc - 20211109

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v0.42.0.0 add MCP endpoint gateway and tool invocation contracts

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
        /// <summary>
        /// Initializes the persisted XNCF Builder configuration service.
        /// </summary>
        /// <param name="repo">The configuration repository.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public ConfigService(IRepositoryBase<Config> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {

        }
        /// <summary>
        /// Creates or updates the persisted build configuration from a build request.
        /// </summary>
        /// <param name="request">The build request containing configuration values.</param>
        /// <returns>The persisted configuration DTO.</returns>
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

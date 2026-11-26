/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MpAccountService.cs
    File description: MpAccountService related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Xncf.WeixinManager.Domain.Services
{
    /// <summary>Provides cached lookup of configured Weixin public accounts.</summary>
    public class MpAccountService : ServiceBase<MpAccount>, IServiceBase<MpAccount>
    {

        private List<MpAccountDto> _allMpAccounts = null;

        /// <summary>Initializes the public-account service.</summary>
        /// <param name="repo">The public-account repository.</param>
        /// <param name="serviceProvider">The dependency-injection service provider.</param>
        public MpAccountService(IRepositoryBase<MpAccount> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// <summary>Gets a configured public account by identifier.</summary>
        /// <param name="accountId">The public-account identifier.</param>
        /// <returns>The matching DTO, or an empty DTO when lookup fails.</returns>
        public MpAccountDto GetMpAccount(int accountId)
        {
            try
            {
                var allMpAccounts = GetAllMpAccounts();
                return allMpAccounts.FirstOrDefault(z => z.Id == accountId);
            }
            catch
            {
                return new MpAccountDto();
            }
        }

        /// <summary>Gets all configured public accounts, using the process-local cache.</summary>
        /// <returns>The configured account DTO list, or an empty list when lookup fails.</returns>
        public List<MpAccountDto> GetAllMpAccounts()
        {
            try
            {
                if (_allMpAccounts == null)
                {
                    var accounts = GetFullList(z => z.AppId != null && z.AppId.Length > 0, z => z.Id, OrderingType.Ascending);
                    _allMpAccounts = new List<MpAccountDto>();
                    accounts.ForEach(z => _allMpAccounts.Add(Mapper.Map<MpAccountDto>(z)));
                }
                return _allMpAccounts;
            }
            catch
            {
                return new List<MpAccountDto>();
            }
        }
    }
}

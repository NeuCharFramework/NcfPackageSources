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
    public class MpAccountService : ServiceBase<MpAccount>, IServiceBase<MpAccount>
    {

        private List<MpAccountDto> _allMpAccounts = null;

        public MpAccountService(IRepositoryBase<MpAccount> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// 获取指定的 MpAccount 对象
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="accountId"></param>
        /// <returns></returns>
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

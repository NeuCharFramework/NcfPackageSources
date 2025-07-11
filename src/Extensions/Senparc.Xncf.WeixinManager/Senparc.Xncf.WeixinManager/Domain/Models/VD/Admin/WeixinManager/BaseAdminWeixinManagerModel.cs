using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Domain.Models.VD.Admin.WeixinManager
{
    public class BaseAdminWeixinManagerModel : Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        public BaseAdminWeixinManagerModel(Lazy<XncfModuleService> xncfModuleService) : base(xncfModuleService)
        {
        }
    }
}

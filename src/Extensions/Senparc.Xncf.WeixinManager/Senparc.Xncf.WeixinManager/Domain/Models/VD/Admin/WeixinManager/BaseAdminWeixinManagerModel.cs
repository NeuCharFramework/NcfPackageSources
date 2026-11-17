/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: BaseAdminWeixinManagerModel.cs

    File description: BaseAdminWeixinManagerModel related implementation

    

    

    Created by: Senparc - 20250712

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

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

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AdminXscfModulePageModelBase.cs
    File description: AdminXscfModulePageModelBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Service;
using Senparc.Ncf.XncfBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.AreaBase.Admin
{
    /// <summary>
    /// Page template for XNCF module
    /// </summary>
    public abstract class AdminXncfModulePageModelBase : AdminPageModelBase
    {
        private XncfModuleDto _xncfModuleDto;
        /// <summary>
        /// XncfModuleDto
        /// </summary>
        public XncfModuleDto XncfModuleDto
        {
            get
            {
                if (_xncfModuleDto == null)
                {
                    SetXncfModuleDto();
                }
                return _xncfModuleDto;
            }
        }
        /// <summary>
        /// XncfModuleDto.Uid
        /// </summary>
        public string XncfModuleUid => XncfModuleDto?.Uid;

        /// <summary>
        /// current XncfRegister in operation
        /// </summary>
        public virtual IXncfRegister XncfRegister => XncfModuleDto != null ? XncfRegisterList.FirstOrDefault(z => z.Uid == XncfModuleDto.Uid) : null;

        protected readonly Lazy<XncfModuleService> _xncfModuleService;

        protected AdminXncfModulePageModelBase(Lazy<XncfModuleService> xncfModuleService)
        {
            _xncfModuleService = xncfModuleService;
        }

        public virtual void SetXncfModuleDto()
        {
            if (Uid.IsNullOrEmpty())
            {
                throw new XncfPageException(null, "页面未提供UID！");
            }

            var xncfModule = _xncfModuleService.Value.GetObject(z => z.Uid == Uid);
            if (xncfModule == null)
            {
                throw new XncfPageException(null, "尚未注册 XNCF 模块，UID：" + Uid);
            }

            _xncfModuleDto = _xncfModuleService.Value.Mapper.Map<XncfModuleDto>(xncfModule);
        }
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BaseAdminPageModel.cs
    File description: BaseAdminPageModel.cs related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260729
    Change description: v0.2.0 enhance admin interaction and secure desktop Admin Chat synchronization

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Senparc.Areas.Admin.Domain.Models.VD;
using Senparc.Ncf.AreaBase.Admin;
using Senparc.Ncf.AreaBase.Admin.Filters;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.Authorization;
using Senparc.Ncf.Core.Models.VD;
using Senparc.Ncf.Core.WorkContext;
using Senparc.Ncf.XncfBase;
using Senparc.Weixin.WxOpen.AdvancedAPIs.DataCube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin
{
    public interface IBaseAdminPageModel : IBasePageModel
    {

    }

    //Temporarily revoke permission validation
    [ServiceFilter(typeof(AuthenticationResultFilterAttribute))]
    [AdminAuthorize(NcfAuthorizationPolicyNames.AdminOnly)]
    public class BaseAdminPageModel : AdminPageModelBase, IBaseAdminPageModel
    {
        public Senparc.Areas.Admin.Register _xncfRegister;
        protected readonly IServiceProvider _serviceProvider;

        public Senparc.Areas.Admin.Register XncfRegister
        {
            get
            {
                _xncfRegister = _xncfRegister ?? new Register();
                return _xncfRegister;
            }
        }


        public BaseAdminPageModel(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            //context
            if (!context.ModelState.IsValid)
            {
                //globalmodelvalidation
                var state = context.ModelState
                    .Where(_ => _.Value.ValidationState == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Invalid)
                    .Select(_ => new { _.Key, Errors = _.Value.Errors.Select(__ => __.ErrorMessage) });
                context.Result = BadRequest(new AjaxReturnModel<object>(state));
            }
            base.OnPageHandlerExecuting(context);
        }

        public override IActionResult RenderError(string message)
        {
            return base.RenderError(message);
        }
    }
}

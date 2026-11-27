/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PageModelBase.cs
    File description: PageModelBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260804
    Change description: v0.28.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
//using Microsoft.Data.SqlClient;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.VD;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Utility;

namespace Senparc.Ncf.Core.Models.VD
{
    public interface IPageModelBase : IBaseUiVD, IValidatorEnvironment
    {
        RouteData RouteData { get; set; }
    }


    public class PageModelBase : PageModel, IPageModelBase
    {
        public FullSystemConfig FullSystemConfig { get; set; }

        public MetaCollection MetaCollection { get; set; }

        public string UserName { get; set; }

        public bool IsAdmin { get; set; }

        public new RouteData RouteData { get => base.RouteData; set => throw new NotImplementedException(); }

        //public RouteData RouteData { get; set; }
        //Legacy comment translated to English.
        //public RouteData GetRouteData()
        //{
        //    return base.RouteData;
        //}

        //public void SetRouteData(RouteData value)
        //{
        //    throw new NotImplementedException();
        //}


        public string CurrentMenu { get; set; }

        public List<Messager> MessagerList { get; set; }

        //public FullAccount FullAccount { get; set; }

        public DateTime PageStartTime { get; set; }

        public DateTime PageEndTime { get; set; }

        //protected void SetupTraceInfo()
        //{
        //    ViewData["TraceIdent"] = HttpContext.TraceIdentifier;
        //    //ViewData["NumLogs"] = HttpRequestLog.GetHttpRequestLog(HttpContext.TraceIdentifier).RequestLogs.Count;
        //}


        public override void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
            base.OnPageHandlerSelected(context);
        }

        public override Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            //Legacy comment translated to English.
            try
            {
                if (SiteConfig.IsInstalling)
                {
                    FullSystemConfig = null;
                    throw new NcfUninstallException("系统进入安装状态", false);
                }
                else
                {
                    var fullSystemConfigCache = context.HttpContext.RequestServices.GetService<FullSystemConfigCache>();
                    FullSystemConfig = fullSystemConfigCache.Data;
                }

            }
            catch (NcfDatabaseUpgradeRequiredException)
            {
                context.Result = new RedirectResult("/Maintenance/DatabaseUpgrade");
                return Task.CompletedTask;
            }
            catch (Exception exception) when (DatabaseInstallState.IsDatabaseUnavailableForInstallation(exception))
            {
                // The provider exception can be wrapped by EF Core's retry strategy.
                // Redirect only when the complete exception chain indicates that the
                // database has not been created; schema mismatches use the maintenance page.
                context.Result = new RedirectResult("/Install");
                return Task.CompletedTask;
            }
            catch (NcfUninstallException)
            {
                //Legacy comment translated to English.
                context.Result = new RedirectResult("/Install");
                return Task.CompletedTask;
            }

            return base.OnPageHandlerExecutionAsync(context, next);
        }


        /// <summary>
        /// Checks whether the current HTTP context authenticated successfully with a scheme.
        /// </summary>
        /// <param name="authenticationScheme">The authentication scheme name.</param>
        /// <returns><see langword="true"/> when authentication succeeds.</returns>
        public async Task<bool> CheckLoginedAsync(string authenticationScheme)
        {
            var authenticate = await HttpContext.AuthenticateAsync(authenticationScheme);
            return authenticate.Succeeded;
        }

        /// <summary>
        /// Stores a serialized UI message in TempData for the next page response.
        /// </summary>
        /// <param name="messageType">The UI message type.</param>
        /// <param name="messageText">The message text.</param>
        /// <param name="showClose">Whether the UI should expose a close action.</param>
        public void SetMessager(MessageType messageType, string messageText, bool showClose = true)
        {
            TempData["Messager"] = new Messager(messageType, messageText).ToJson();
        }

        /// <summary>
        /// Creates a JSON AJAX response carrying a typed payload and success state.
        /// </summary>
        /// <typeparam name="T">The payload type.</typeparam>
        /// <param name="data">The response payload.</param>
        /// <param name="succed">Whether the operation succeeded.</param>
        /// <param name="msg">The response message.</param>
        /// <returns>A JSON action result.</returns>
        public IActionResult Ok<T>(T data, bool succed, string msg = "操作成功!")
        {
            AjaxReturnModel<T> returnModel = new AjaxReturnModel<T>(data);
            returnModel.Success = succed;
            returnModel.Msg = msg;
            return new JsonResult(returnModel);
        }

        /// <summary>
        /// Creates a JSON AJAX response containing only success state and message.
        /// </summary>
        /// <param name="isSuccess">Whether the operation succeeded.</param>
        /// <param name="message">The response message.</param>
        /// <returns>A JSON action result.</returns>
        public IActionResult Ok(bool isSuccess, string message)
        {
            if (!isSuccess)
            {
                return new OkObjectResult(new AjaxReturnModel() { Success = isSuccess, Msg = message });
            }
            return new OkObjectResult(new AjaxReturnModel() { Success = isSuccess, Msg = message });
        }

        /// <summary>
        /// Creates a successful JSON AJAX response carrying an object payload.
        /// </summary>
        /// <param name="data">The response payload.</param>
        /// <returns>A JSON action result.</returns>
        public IActionResult Ok(object data)
        {
            return new OkObjectResult(new AjaxReturnModel<object>(data) { Success = true });
        }
    }

}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: BaseAdminController.cs
    File description: BaseAdminController related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.0.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Senparc.Areas.Admin.Filter;
using Senparc.Areas.Admin.Models.VD;
using Senparc.CO2NET.Extensions;
using Senparc.Core.Models.VD;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.VD;
using System.Collections.Generic;
using System.Net;

namespace Senparc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [SenparcAdminAuthorize()]
    public class BaseAdminController : Controller, IResultFilter
    {
        /// <summary>
        /// Initializes the legacy administrator controller base.
        /// </summary>
        public BaseAdminController()
        {
        }

        public string UserName
        {
            get
            {
                if (User.Identity.IsAuthenticated)
                {
                    return User.Identity.Name;
                }
                return null;
            }
        }

        /// <inheritdoc/>
        /// <remarks>Preserves TempData messages before the action executes.</remarks>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            TempData["Messager"] = TempData["Messager"];

            base.OnActionExecuting(context);
        }


        /// <summary>
        /// Copies authenticated user, route, and TempData messages into the
        /// legacy administrator view-data model.
        /// </summary>
        /// <param name="context">The result execution context.</param>
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (ViewData.Model is IBaseAdminVD)
            {
                IBaseAdminVD vd = ViewData.Model as IBaseAdminVD;
                vd.UserName = UserName;
                vd.RouteData = RouteData;

                vd.MessagerList = vd.MessagerList ?? new List<Messager>();
                var tmpStr = (string)TempData["Messager"];
                if (!string.IsNullOrWhiteSpace(tmpStr))
                {
                    var obj = JsonConvert.DeserializeObject<Messager>(tmpStr);
                    if (obj != null)
                    {
                        vd.MessagerList.Add(obj);
                    }
                }
                else
                {
                    if (!ModelState.IsValid)
                    {
                        vd.MessagerList.Add(new Messager(MessageType.danger, "提交信息有错误，请检查。"));
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void OnResultExecuted(ResultExecutedContext context)
        {
        }



        /// <summary>
        /// Redirects the legacy create action to the shared edit action.
        /// </summary>
        /// <returns>A redirect action targeting the edit view.</returns>
        public ActionResult Create()
        {
            return RedirectToAction("Edit");
        }

        #region Legacy section

        [NonAction]
        /// <summary>
        /// Creates the legacy administrator success view with navigation metadata.
        /// </summary>
        /// <param name="message">The success message.</param>
        /// <param name="backAction">Optional return action.</param>
        /// <param name="backController">Optional return controller.</param>
        /// <param name="backRouteValues">Optional return route values.</param>
        /// <returns>The success view result.</returns>
        public virtual ActionResult RenderSuccess(string message, string backAction = null, string backController = null, RouteValueDictionary backRouteValues = null)
        {
            backAction = backAction ?? RouteData.Values["controller"] as string;
            backController = backController ?? RouteData.Values["controller"] as string;
            backRouteValues = backRouteValues ?? new RouteValueDictionary();
            SuccessVD vd = new SuccessVD()
            {
                Message = message,
                BackAction = backAction,
                BackController = backController,
                BackRouteValues = backRouteValues
            };
            return View("Success", vd);
        }
        [NonAction]
        /// <summary>
        /// Creates the legacy administrator error view while preserving route metadata.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The error view result.</returns>
        public virtual ActionResult RenderError(string message)
        {
            //preserve the original controller and action information
            ViewData["FakeControllerName"] = RouteData.Values["controller"] as string;
            ViewData["FakeActionName"] = RouteData.Values["action"] as string;

            return View("Error", new Error_ExceptionVD
            {
                //HandleErrorInfo = new HandleErrorInfo(new Exception(message), Url.RequestContext.RouteData.GetRequiredString("controller"), Url.RequestContext.RouteData.GetRequiredString("action"))
            });
        }
        [NonAction]
        /// <summary>
        /// Renders a not-found result using the current request path.
        /// </summary>
        /// <returns>The legacy 404 view result.</returns>
        public ActionResult RenderError404()
        {
            //return RedirectToAction("Error404", "Error", new { aspxerrorpath = Request.Url.PathAndQuery });
            return Error404(Request.PathAndQuery());
        }
        /// <summary>
        /// Renders the legacy administration 404 view and sets the HTTP status code.
        /// </summary>
        /// <param name="aspxerrorpath">The original request path to expose to the error view.</param>
        /// <returns>The rendered 404 action result.</returns>
        [NonAction]
        public virtual ActionResult Error404(string aspxerrorpath = null)
        {
            if (string.IsNullOrEmpty(aspxerrorpath) && !Request.UrlReferrer().IsNullOrEmpty())
            {
                aspxerrorpath = Request.PathAndQuery();
            }
            Error_Error404VD vd = new Error_Error404VD
            {
                Url = aspxerrorpath
            };
            var result = View("Error404", vd);//HttpNotFound();
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return result;
        }

        #endregion

        /// <summary>
        /// Creates the legacy JSON envelope used by AJAX callers.
        /// </summary>
        /// <param name="success">Whether the operation succeeded.</param>
        /// <param name="result">The operation payload.</param>
        /// <returns>A JSON result containing success and payload fields.</returns>
        [NonAction]
        public JsonResult RenderJsonSuccessResult(bool success, object result)
        {
            return Json(new
            {
                Success = success,
                Result = result
            });
        }
        /// <summary>
        /// Stores an administrator-area message in TempData for the next result.
        /// </summary>
        /// <param name="messageType">The message severity/type.</param>
        /// <param name="messageText">The message text.</param>
        /// <param name="showClose">Whether the UI may dismiss the message.</param>
        public void SetMessager(MessageType messageType, string messageText, bool showClose = true)
        {
            TempData["Messager"] = new Messager(messageType, messageText).ToJson();
        }
    }
}

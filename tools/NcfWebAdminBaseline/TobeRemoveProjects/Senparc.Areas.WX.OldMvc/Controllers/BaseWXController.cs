/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: BaseWXController.cs
    File description: BaseWXController related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.VD;

namespace Senparc.Areas.WX.Controllers
{
    [Area("WX")]
    //[WxAuthorize]
    public class BaseWXController : Controller, IResultFilter
    {
        
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
        /// <remarks>Preserves legacy TempData messages before action execution.</remarks>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            TempData["Messager"] = TempData["Messager"];
            base.OnActionExecuting(context);
        }

        /// <inheritdoc/>
        public void OnResultExecuting(ResultExecutingContext context)
        {
            
        }

        /// <inheritdoc/>
        public void OnResultExecuted(ResultExecutedContext context)
        { }

        [NonAction]
        /// <summary>
        /// Creates the legacy Weixin success view with navigation metadata.
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
        /// Creates the legacy Weixin error view while preserving route metadata.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The error view result.</returns>
        public virtual ActionResult RenderError(string message)
        {
            //preserve the original controller and action information
            ViewData["FakeControllerName"] = RouteData.Values["controller"] as string;
            ViewData["FakeActionName"] = RouteData.Values["action"] as string;

            return View("Error", new Error_ExceptionVD() { Message = message });
        }

        /// <summary>
        /// Creates the legacy JSON response envelope and optionally wraps it in a JSONP callback.
        /// </summary>
        /// <param name="success">Whether the operation succeeded.</param>
        /// <param name="result">The response payload.</param>
        /// <param name="jsonSerializerSettings">Optional serializer settings.</param>
        /// <param name="jsonpCallbackFunction">An optional JSONP callback name.</param>
        /// <returns>A JSON or JSONP action result.</returns>
        [NonAction]
        public ActionResult RenderJsonSuccessResult(bool success, object result,
            Newtonsoft.Json.JsonSerializerSettings jsonSerializerSettings = null,
            string jsonpCallbackFunction = null)
        {
            var json = new
            {
                Success = success,
                Result = result
            };
            if (!jsonpCallbackFunction.IsNullOrEmpty())
            {
                return Content($"{jsonpCallbackFunction}({json.ToJson()})");
            }
            //return Json(json, jsonRequestBehavior);
            //https://stackoverflow.com/questions/4155014/json-asp-net-mvc-maxjsonlength-exception
            //Legacy comment translated to English.

            jsonSerializerSettings = jsonSerializerSettings ?? new Newtonsoft.Json.JsonSerializerSettings();
            return new JsonResult(json);
        }

        /// <summary>
        /// Stores one legacy UI message in request TempData.
        /// </summary>
        /// <param name="messageType">The message type.</param>
        /// <param name="messageText">The message text.</param>
        /// <param name="showClose">Whether the message can be dismissed.</param>
        public void SetMessager(MessageType messageType, string messageText, bool showClose = true)
        {
            TempData["Messager"] = new Messager(messageType, messageText).ToJson();
        }
    }
}

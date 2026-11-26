/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: BaseUserController.cs
    File description: BaseUserController related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Senparc.Areas.User.Models.VD;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.VD;
using Senparc.Mvc.Controllers;
using System;
using System.Collections.Generic;

namespace Senparc.Areas.User.Controllers
{
    [Area("User")]
    [UserAuthorize("UserOnly")]
    public class BaseUserController : BaseController, IResultFilter
    {
        // public List<Neural> CurrentNeurals { get; set; }
        public new string UserName
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

        public bool IsLogined
        {
            get
            {
                //Legacy comment translated to English.
                var authenticate = HttpContext.AuthenticateAsync(UserAuthorizeAttribute.AuthenticationScheme)
                    .GetAwaiter().GetResult();

                return authenticate.Succeeded;
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Preserves TempData messages before delegating user context loading
        /// and action preparation to the base controller.
        /// </remarks>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            TempData["Messager"] = TempData["Messager"];
            //var userService = SenparcDI.GetService<AccountService>();

            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Copies authenticated user, route, and serialized TempData message information
        /// into the legacy user view-data model before rendering.
        /// </summary>
        /// <param name="context">The result execution context.</param>
        public new void OnResultExecuting(ResultExecutingContext context)
        {
            if (ViewData.Model is IBaseUserVD vd)
            {
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
                //else
                //{
                //    if (!ModelState.IsValid)
                //    {
                //Legacy comment translated to English.
                //    }
                //}
            }
            base.OnResultExecuting(context);
        }

        /// <inheritdoc/>
        public new void OnResultExecuted(ResultExecutedContext context)
        {
            base.OnResultExecuted(context);
        }

        [NonAction]
        /// <inheritdoc/>
        /// <remarks>Uses the user-area success view model and navigation route values.</remarks>
        public override ActionResult RenderSuccess(string message, string backAction = null, string backController = null, RouteValueDictionary backRouteValues = null)
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
        /// <inheritdoc/>
        /// <remarks>Uses the user-area error view model while preserving route diagnostics.</remarks>
        public override ActionResult RenderError(string message, Exception ex = null)
        {
            //preserve the original controller and action information
            ViewData["FakeControllerName"] = RouteData.Values["controller"] as string;
            ViewData["FakeActionName"] = RouteData.Values["action"] as string;

            return View("Error", new Error_ExceptionVD() { Message = message, Exception = ex });
        }

        /// <summary>
        /// Creates the legacy JSON response envelope and optionally wraps it in JSONP.
        /// </summary>
        /// <param name="success">Whether the operation succeeded.</param>
        /// <param name="result">The response payload.</param>
        /// <param name="jsonSerializerSettings">Optional serializer settings.</param>
        /// <param name="jsonpCallbackFunction">An optional JSONP callback name.</param>
        /// <returns>A JSON or JSONP action result.</returns>
        [NonAction]
        public new ActionResult RenderJsonSuccessResult(bool success, object result,
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
        /// Stores a serialized user-area message in TempData for the next rendered result.
        /// </summary>
        /// <param name="messageType">The message severity/type.</param>
        /// <param name="messageText">The message text.</param>
        /// <param name="showClose">Whether the UI may dismiss the message.</param>
        public new void SetMessager(MessageType messageType, string messageText, bool showClose = true)
        {
            TempData["Messager"] = new Messager(messageType, messageText).ToJson();
        }
    }
}

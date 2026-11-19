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

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            TempData["Messager"] = TempData["Messager"];
            base.OnActionExecuting(context);
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            
        }

        public void OnResultExecuted(ResultExecutedContext context)
        { }

        [NonAction]
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
        public virtual ActionResult RenderError(string message)
        {
            //preserve the original controller and action information
            ViewData["FakeControllerName"] = RouteData.Values["controller"] as string;
            ViewData["FakeActionName"] = RouteData.Values["action"] as string;

            return View("Error", new Error_ExceptionVD() { Message = message });
        }

        /// <summary>
        /// displayJsonresult
        /// </summary>
        /// <param name="success">whethersuccess</param>
        ///Legacy comment translated to English.
        /// <returns></returns>
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

        public void SetMessager(MessageType messageType, string messageText, bool showClose = true)
        {
            TempData["Messager"] = new Messager(messageType, messageText).ToJson();
        }
    }
}

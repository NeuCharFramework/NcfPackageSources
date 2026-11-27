/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: BaseController.cs
    File description: BaseController related implementation

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
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Core.Models;
using Senparc.Core.Models.VD;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.VD;
using Senparc.Ncf.Log;
using Senparc.Ncf.Utility;
using System;
using System.Collections.Generic;
using BaseVD = Senparc.Ncf.Core.Models.VD;
using Microsoft.Extensions.DependencyInjection;

namespace Senparc.Mvc.Controllers
{
    //[RequireHttps]
    //[SenparcHandleError]
    [UserAuthorize("UserAnonymous")]
    public class BaseController : Controller, IResultFilter, IValidatorEnvironment
    {
        //private ISystemConfigService _systemConfigService;
        protected FullSystemConfig _fullSystemConfig;
        protected DateTime PageStartTime { get; set; }
        protected DateTime PageEndTime { get; set; }
        /// <summary>
        /// Initializes the legacy controller and records the request start time
        /// used by view-model timing metadata.
        /// </summary>
        public BaseController()
        {
            PageStartTime = DateTime.Now;
        }

        public FullAccount FullAccount { get; set; }

        public string UserName => User.Identity.IsAuthenticated ? User.Identity.Name : null;

        public bool IsAdmin => base.HttpContext.Session.GetString("AdminLogin") as string == "1" && FullAccount != null;

        /// <inheritdoc/>
        /// <remarks>
        /// Loads system/account context before the action and converts unexpected
        /// setup failures into the legacy error view.
        /// </remarks>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                TempData["Messager"] = TempData["Messager"];
                var fullSystemConfigCache = context.HttpContext.RequestServices.GetService<FullSystemConfigCache>();
                _fullSystemConfig = fullSystemConfigCache.Data;

                var fullAccountCache = context.HttpContext.RequestServices.GetService<FullAccountCache>();
                if (this.UserName != null)
                {
                    FullAccount = fullAccountCache.GetObject(this.UserName);
                    if (FullAccount != null)
                    {
                        //...

                    }
                    else
                    {
                        //Legacy comment translated to English.
                        LogUtility.Account.Error($"发生严重错误，用户已登录，但缓存及数据库中不存在：{this.UserName}");
                    }
                }

                FullAccount = FullAccount ?? new FullAccount();
                FullAccount.LastActiveTime = DateTime.Now;

                base.OnActionExecuting(context);
            }
            catch (Exception ex)
            {
                context.Result = RenderError(ex.Message, ex);
            }

        }

        /// <inheritdoc/>
        /// <remarks>
        /// Copies authenticated account, system configuration, messages, route,
        /// and page timing data into recognized legacy view-data models.
        /// </remarks>
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (ViewData.Model is IBaseVD)
            {
                var vd = ViewData.Model as IBaseVD;
                vd.UserName = this.UserName;
                vd.IsAdmin = this.IsAdmin;
                vd.FullAccount = this.FullAccount;
                vd.RouteData = this.RouteData;

                vd.FullSystemConfig = _fullSystemConfig;

                vd.MessagerList = vd.MessagerList ?? new List<Messager>();
                if (TempData["Messager"] as List<Messager> != null)
                {
                    vd.MessagerList.AddRange(TempData["Messager"] as List<Messager>);
                }
                //else
                //{
                //    if (!ModelState.IsValid)
                //    {
                //Legacy comment translated to English.
                //    }
                //}

                if (vd.FullSystemConfig == null)
                {
                    LogUtility.WebLogger.Error("FullSystemConfig为null");
                    vd.FullSystemConfig = new FullSystemConfig();
                }
            }

            if (ViewData.Model is BaseVD.IBaseUiVD)
            {
                var vd = ViewData.Model as BaseVD.IBaseUiVD;
                PageEndTime = DateTime.Now;
                vd.PageStartTime = PageStartTime;
                vd.PageEndTime = PageEndTime;
            }
        }

        /// <summary>
        /// Redirects the legacy create action to the edit action.
        /// </summary>
        /// <returns>A redirect action to the edit endpoint.</returns>
        public ActionResult Create()
        {
            return RedirectToAction("Edit");
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
        public ActionResult RenderJsonSuccessResult(bool success, object result,
            Newtonsoft.Json.JsonSerializerSettings jsonSerializerSettings = null,
            /*JsonRequestBehavior jsonRequestBehavior,*/
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
            else
            {
                //return Json(json, jsonRequestBehavior);
                //https://stackoverflow.com/questions/4155014/json-asp-net-mvc-maxjsonlength-exception
                //Legacy comment translated to English.

                jsonSerializerSettings = jsonSerializerSettings ?? new Newtonsoft.Json.JsonSerializerSettings()
                {
                    //customsettings
                };

                return new JsonResult(json, jsonSerializerSettings);
                //{
                //    JsonRequestBehavior = jsonRequestBehavior,
                //    MaxJsonLength = Int32.MaxValue
                //};
            }
        }


        #region Legacy section

        [NonAction]
        /// <summary>
        /// Creates the legacy success view response with navigation metadata.
        /// </summary>
        /// <param name="message">The user-facing success message.</param>
        /// <param name="backAction">Optional action to use for navigation.</param>
        /// <param name="backController">Optional controller to use for navigation.</param>
        /// <param name="backRouteValues">Optional route values for navigation.</param>
        /// <returns>The legacy success view result.</returns>
        public virtual ActionResult RenderSuccess(string message, string backAction = null,
            string backController = null, RouteValueDictionary backRouteValues = null)
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
            return View("Success");
        }

        [NonAction]
        /// <summary>
        /// Creates the legacy error view response while preserving the current route
        /// metadata for diagnostics.
        /// </summary>
        /// <param name="message">The user-facing error message.</param>
        /// <param name="ex">The optional underlying exception.</param>
        /// <returns>The legacy error view result.</returns>
        public virtual ActionResult RenderError(string message, Exception ex = null)
        {
            //preserve the original controller and action information
            ViewData["FakeControllerName"] = RouteData.Values["controller"] as string;
            ViewData["FakeActionName"] = RouteData.Values["action"] as string;

            return View("Error", new Error_ExceptionVD
            {
                Message = message,
                Exception = ex
            });
        }

        #endregion

        /// <summary>
        /// Appends a message notification to the request TempData message collection.
        /// </summary>
        /// <param name="messageType">The message severity/type.</param>
        /// <param name="messageText">The message text.</param>
        /// <param name="showClose">Whether the UI may dismiss the message.</param>
        public void SetMessager(MessageType messageType, string messageText, bool showClose = true)
        {
            if (TempData["Messager"] == null || !(TempData["Messager"] is List<Messager>))
            {
                TempData["Messager"] = new List<Messager>();
            }

            (TempData["Messager"] as List<Messager>).Add(new Messager(messageType, messageText, showClose));
        }


        /// <inheritdoc/>
        public void OnResultExecuted(ResultExecutedContext context)
        {
        }
    }
}

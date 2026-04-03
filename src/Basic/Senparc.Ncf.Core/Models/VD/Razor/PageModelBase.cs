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
using System.Data.Common;
using Senparc.Ncf.Core.Config;

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
        //Another way to write it:
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
            //Get cache system information
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
            catch (/*SqlException*/ DbException)
            {
                //If the database is not created
                context.Result = new RedirectResult("/Install");

                return Task.CompletedTask;

            }
            catch (NcfUninstallException)
            {
                //Requires installation
                context.Result = new RedirectResult("/Install");
                return Task.CompletedTask;
            }

            return base.OnPageHandlerExecutionAsync(context, next);
        }


        /// <summary>
        /// Check if you are logged in under a specific Scheme
        /// </summary>
        /// <param name="authenticationScheme">Scheme name</param>
        /// <returns></returns>
        public async Task<bool> CheckLoginedAsync(string authenticationScheme)
        {
            var authenticate = await HttpContext.AuthenticateAsync(authenticationScheme);
            return authenticate.Succeeded;
        }

        public void SetMessager(MessageType messageType, string messageText, bool showClose = true)
        {
            TempData["Messager"] = new Messager(messageType, messageText).ToJson();
        }

        public IActionResult Ok<T>(T data, bool succed, string msg = "操作成功!")
        {
            AjaxReturnModel<T> returnModel = new AjaxReturnModel<T>(data);
            returnModel.Success = succed;
            returnModel.Msg = msg;
            return new JsonResult(returnModel);
        }

        public IActionResult Ok(bool isSuccess, string message)
        {
            if (!isSuccess)
            {
                return new OkObjectResult(new AjaxReturnModel() { Success = isSuccess, Msg = message });
            }
            return new OkObjectResult(new AjaxReturnModel() { Success = isSuccess, Msg = message });
        }

        public IActionResult Ok(object data)
        {
            return new OkObjectResult(new AjaxReturnModel<object>(data) { Success = true });
        }
    }

}

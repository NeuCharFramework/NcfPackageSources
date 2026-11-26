/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: FeedBackController.cs
    File description: FeedBackController related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.User.Models.VD;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Log;
using Senparc.Mvc.Filter;
using Senparc.Service;
using Senparc.Ncf.Utility;
using System;
using System.Linq;
using Senparc.Ncf.Service;

namespace Senparc.Areas.User.Controllers
{
    [MenuFilter("FeedBack")]
    public class FeedBackController : BaseUserController
    {
        private readonly FeedBackService _feedBackService;

        /// <summary>
        /// Initializes the legacy feedback controller with its persistence service.
        /// </summary>
        /// <param name="feedBackService">The feedback service used by the controller actions.</param>
        public FeedBackController(FeedBackService feedBackService)
        {
            _feedBackService = feedBackService;
        }

        /// <summary>
        /// Renders the legacy feedback list page. The page index is retained for
        /// compatibility with the original route and client workflow.
        /// </summary>
        /// <param name="pageIndex">The requested one-based page number.</param>
        /// <returns>The feedback list view.</returns>
        public ActionResult Index(int pageIndex = 1)
        {
            var vd = new FeedBack_IndexVD()
            {
            };
            return View(vd);
        }

        /// <summary>
        /// Returns a descending-by-time page of feedback records with account summary data.
        /// </summary>
        /// <param name="pageIndex">The one-based page number.</param>
        /// <param name="pageSize">The maximum number of feedback records per page.</param>
        /// <returns>A JSON success envelope containing the projected feedback page.</returns>
        [HttpGet, Route("api/[controller]/get")]
        public IActionResult Get(int pageIndex = 1, int pageSize = 20)
        {
            var seh = new SenparcExpressionHelper<FeedBack>();
            var where = seh.BuildWhereExpression();

            var modelList = _feedBackService.GetObjectList(pageIndex, pageSize, where, z => z.AddTime, OrderingType.Descending, new string[] { nameof(Account) });

            return RenderJsonSuccessResult(true, modelList.Select(z => new
            {
                z.Id,
                z.Content,
                z.AddTime,
                z.Account?.PicUrl,
                z.Account?.Email,
                z.Account?.UserName
            }));
        }

        /// <summary>
        /// Returns one feedback record with its account summary data.
        /// </summary>
        /// <param name="id">The feedback identifier.</param>
        /// <returns>A JSON success envelope, or a failure envelope when the record is missing.</returns>
        [HttpGet, Route("api/[controller]/get/{id}")]
        public IActionResult Get(int id)
        {
            var obj = _feedBackService.GetObject(z => z.Id == id, new string[] { nameof(Account) });
            if (obj == null)
            {
                return RenderJsonSuccessResult(false, new
                {
                    Message = "信息不存在"
                });
            }

            return RenderJsonSuccessResult(true, new
            {
                obj.Id,
                obj.Content,
                obj.AddTime,
                obj.Account?.PicUrl,
                obj.Account?.NickName,
                obj.Account?.UserName
            });
        }

        /// <summary>
        /// Renders the legacy feedback editor page.
        /// </summary>
        /// <param name="id">The optional feedback identifier being edited.</param>
        /// <returns>The feedback editor view.</returns>
        public ActionResult Edit(int id = 0)
        {
            var vd = new FeedBack_EditVD();

            return View(vd);
        }

        /// <summary>
        /// Creates or updates feedback for the current account after model validation.
        /// Persistence failures are logged and returned in the JSON failure envelope.
        /// </summary>
        /// <param name="model">The feedback content and optional identifier.</param>
        /// <returns>A JSON success or failure envelope.</returns>
        [HttpPost]
        public IActionResult Edit([FromBody]FeedBack model)
        {
            if (!ModelState.IsValid)
            {
                return RenderJsonSuccessResult(false, new
                {
                    Message = ModelState.Values.First(z => z.Errors.Count > 0).Errors[0].ErrorMessage
                });
            }
            try
            {
                var obj = _feedBackService.CreateOrUpdate(model.Content, FullAccount.Id, model.Id);
                return RenderJsonSuccessResult(true, new
                {
                    id = obj.Id
                });
            }
            catch (Exception ex)
            {
                LogUtility.SystemLogger.Error(ex.Message, ex);
                return RenderJsonSuccessResult(false, new
                {
                    Message = $"保存失败【{ex.Message}】"
                });
            }
        }

        /// <summary>
        /// Deletes one feedback record owned by the current account.
        /// </summary>
        /// <param name="id">The feedback identifier to delete.</param>
        /// <returns>A JSON success or failure envelope.</returns>
        [HttpGet]
        public ActionResult Delete(int id)
        {
            try
            {
                var obj = _feedBackService.GetObject(z => z.Id == id && z.AccountId == FullAccount.Id);
                if (obj == null)
                {
                    return RenderJsonSuccessResult(false, new
                    {
                        Message = "信息不存在"
                    });
                }
                _feedBackService.DeleteObject(obj);
                return RenderJsonSuccessResult(true, new
                {
                    Message = "删除成功"
                });
            }
            catch (Exception ex)
            {
                LogUtility.SystemLogger.Error(ex.Message, ex);
                return RenderJsonSuccessResult(false, new
                {
                    Message = "删除失败"
                });
            }
        }
    }
}

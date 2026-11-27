/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AccountController.cs
    File description: Senparc.Areas.Admin.Controllers related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261127
    Change description: v0.0.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Models.VD;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Mvc.Filter;
using Senparc.Office;
using Senparc.Ncf.Service;
using Senparc.Ncf.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Senparc.Ncf.Service;

namespace Senparc.Areas.Admin.Controllers
{
    [MenuFilter("Account")]
    public class AccountController : BaseAdminController
    {
        private readonly AccountService _accountService;
        private readonly SenparcEntities _senparcEntities;

        /// <summary>
        /// Initializes the legacy administrator account controller.
        /// </summary>
        /// <param name="accountService">The account persistence service.</param>
        /// <param name="senparcEntities">The database context used by legacy views.</param>
        public AccountController(AccountService accountService, SenparcEntities senparcEntities)
        {
            _accountService = accountService;
            _senparcEntities = senparcEntities;
        }

        /// <summary>
        /// Lists administrator-visible accounts with optional keyword filtering and paging.
        /// </summary>
        /// <param name="kw">Optional name, nickname, user-name, or phone filter.</param>
        /// <param name="pageIndex">The one-based page number.</param>
        /// <returns>The account list view.</returns>
        public ActionResult Index(string kw = null, int pageIndex = 1)
        {
            var seh = new SenparcExpressionHelper<Account>();
            seh.ValueCompare.AndAlso(true, z => !z.Flag)
                .AndAlso(!kw.IsNullOrEmpty(), z => z.RealName.Contains(kw) || z.NickName.Contains(kw) || z.UserName.Contains(kw) || z.Phone.Contains(kw));
            var where = seh.BuildWhereExpression();

            var modelList = _accountService.GetObjectList(pageIndex, 20, where, z => z.Id, OrderingType.Descending);
            var vd = new Account_IndexVD()
            {
                AccountList = modelList,
                kw = kw
            };
            return View(vd);
        }
        /// <summary>
        /// Loads the account edit view for an existing account or a new account.
        /// </summary>
        /// <param name="id">The account identifier; zero creates a new account.</param>
        /// <returns>The edit view or a legacy error view when the account is missing.</returns>
        public ActionResult Edit(int id = 0)
        {
            bool isEdit = id > 0;
            var vd = new Account_EditVD();
            if (isEdit)
            {
                var model = _accountService.GetObject(z => z.Id == id);
                if (model == null)
                {
                    return RenderError("信息不存在！");
                }
                vd.Id = model.Id;
                vd.UserName = model.UserName;
                vd.NickName = model.NickName;
                vd.RealName = model.RealName;
                vd.Phone = model.Phone;
                vd.Note = model.Note;
            }
            vd.IsEdit = isEdit;
            return View(vd);
        }

        [HttpPost]
        /// <summary>
        /// Validates and persists legacy account profile fields, returning to the list
        /// after success or redisplaying validation failures.
        /// </summary>
        /// <param name="model">The posted account edit model.</param>
        /// <returns>The edit view, error view, or list redirect.</returns>
        public async Task<IActionResult> Edit(Account_EditVD model)
        {
            bool isEdit = model.Id > 0;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            Account account = null;
            if (isEdit)
            {
                account = _accountService.GetObject(z => z.Id == model.Id);
                if (account == null)
                {
                    return RenderError("信息不存在！");
                }
            }
            else
            {
                account = new Account()
                {
                    AddTime = DateTime.Now,
                    UserName = _accountService.GetNewUserName(),
                    NickName = ""
                };
            }
            try
            {
                if (_accountService.CheckPhoneExisted(account.Id, model.Phone))
                {
                    ModelState.AddModelError("Phone", "手机号码重复");
                    return View(model);
                }

                await this.TryUpdateModelAsync(account, ""
               , z => z.RealName
               , z => z.Phone
               , z => z.Note);

                this._accountService.SaveObject(account);
                base.SetMessager(MessageType.success, $"{(isEdit ? "修改" : "新增")}成功！");
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                base.SetMessager(MessageType.danger, $"{(isEdit ? "修改" : "新增")}失败！");
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        /// <summary>
        /// Deletes the selected account records and redirects to the account list.
        /// </summary>
        /// <param name="ids">The account identifiers selected for deletion.</param>
        /// <returns>A redirect to the account list.</returns>
        public ActionResult Delete(List<int> ids)
        {
            try
            {
                var objList = _accountService.GetFullList(z => ids.Contains(z.Id), z => z.Id, OrderingType.Ascending);
                _accountService.DeleteAll(objList);
                SetMessager(MessageType.success, "删除成功！");
            }
            catch (Exception e)
            {
                SetMessager(MessageType.danger, $"删除失败【{e.Message}！");
            }
            return RedirectToAction("Index");
        }
    }
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: AccountPayLogService.cs
    File description: AccountPayLogService related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20261119
    Change description: v0.3.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.3.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.Service.ACL;
using Senparc.Ncf.Core.Enums;
using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Ncf.Log;
using Senparc.Ncf.Repository;
using System;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.Accounts.Domain.Services;

namespace Senparc.Xncf.Accounts.Domain.Services
{

    public class AccountPayLogService : BaseClientService<AccountPayLog>
    {
        public AccountPayLogService(AccountPayLogRepository accountPayLogRepo, IServiceProvider serviceProvider)
            : base(accountPayLogRepo, serviceProvider)
        {
            
        }

        /// <summary>
        /// Loads a payment log by its external order number and optional navigation includes.
        /// </summary>
        /// <param name="orderNumber">The external order number.</param>
        /// <param name="includes">Optional related-property include paths.</param>
        /// <returns>The matching payment log, or <see langword="null"/>.</returns>
        public AccountPayLog GetByOrderNumber(string orderNumber, params string[] includes)
        {
            return this.GetObject(z => z.OrderNumber == orderNumber, includes);
        }

        /// <summary>
        /// Creates and persists an unpaid payment log initialized from the account,
        /// order, money, points, and payment-type details.
        /// </summary>
        /// <param name="fullAccount">The account receiving the payment result.</param>
        /// <param name="orderNumber">The external order number.</param>
        /// <param name="ip">The originating client address.</param>
        /// <param name="totalFee">The total payment amount.</param>
        /// <param name="description">The order description.</param>
        /// <param name="getPoints">The points associated with the payment.</param>
        /// <param name="payType">The payment channel.</param>
        /// <returns>The persisted unpaid payment log.</returns>
        public AccountPayLog CreateOrder(FullAccount fullAccount, string orderNumber, string ip, decimal totalFee, string description, decimal getPoints, AccountPayLog_PayType payType)
        {
            AccountPayLog accountPayLog = new AccountPayLog()
            {
                OrderNumber = orderNumber, //Legacy comment translated to English.
                AddTime = DateTime.Now,
                CompleteTime = DateTime.Now,
                AccountId = fullAccount.Id,
                AddIp = ip,
                PayMoney = totalFee,
                Price = totalFee,
                Fee = totalFee,
                Description = description ?? "",
                Status = (int)AccountPayLog_Status.未支付,
                Type = 1,
                GetPoints = getPoints,
                PayType = (int)payType,
                UsedPoints = 0,
                TotalPrice = totalFee,
                OrderType = 0,
                PayParam = ""
            };
            SaveObject(accountPayLog);
            return accountPayLog;
        }

        /// <summary>
        /// Marks a payment log as cancelled and persists its completion time.
        /// </summary>
        /// <param name="accountPayLog">The payment log to cancel.</param>
        public void CancelOrder(AccountPayLog accountPayLog)
        {
            accountPayLog.CompleteTime = DateTime.Now;
            accountPayLog.Status = (int)AccountPayLog_Status.已取消;
            SaveObject(accountPayLog);
        }


        /// <summary>
        /// Completes an unpaid payment inside a transaction, credits the account,
        /// and persists both account and payment-log changes.
        /// </summary>
        /// <param name="accountPayLog">The unpaid payment log to complete.</param>
        public void PayLogFinish(AccountPayLog accountPayLog)
        {
            if (accountPayLog.Status != (int)AccountPayLog_Status.未支付)
            {
                return;
            }
            //try
            //{
            AccountService accountService = _serviceProvider.GetService<AccountService>();
            Account account = accountService.GetObject(z => z.Id == accountPayLog.AccountId);
            BeginTransaction(() =>
            {
                accountPayLog.Status = (int)AccountPayLog_Status.已支付;
                accountPayLog.CompleteTime = DateTime.Now;
                account.Points += accountPayLog.PayMoney;
                accountService.SaveObject(account);
                SaveObject(accountPayLog);
            }, ex =>
            {
                LogUtility.AccountPayLog.Error($"支付完成，发生错误：{ex.Message}", ex);
            });
            //using (var transcation = BeginTransaction())
            //{

            //    transcation.Commit();
            //}
            //}
            //catch (Exception ex)
            //{

            //Legacy comment translated to English.
            //}
        }

        public override void SaveObject(AccountPayLog obj)
        {
            bool isInsert = base.IsInsert(obj);
            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("AccountPayLog{2}：{0}（ID：{1}，当前状态：{3}）", obj.OrderNumber, obj.Id,
                isInsert ? "新增" : "编辑", (AccountPayLog_Status)obj.Status);
        }

        public override void DeleteObject(AccountPayLog obj)
        {
            LogUtility.WebLogger.InfoFormat("AccountPayLog被删除：{0}（ID：{1}）", obj.OrderNumber, obj.Id);
            base.DeleteObject(obj);
        }
    }
}

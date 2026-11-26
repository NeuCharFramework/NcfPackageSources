/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PointsLogService.cs
    File description: Related implementation.

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260724
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.3.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/
using Senparc.Service.ACL;
using Senparc.Xncf.Accounts.Domain.Cache;
using Senparc.Ncf.Core.Enums;
using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Ncf.Log;
using Senparc.Ncf.Service;
using System;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.Accounts.Domain;

namespace Senparc.Service
{
    public class PointsLogService :  BaseClientService<PointsLog>
    {
        public PointsLogService(PointsLogRepository pointsLogRepo, IServiceProvider serviceProvider)
            : base(pointsLogRepo, serviceProvider)
        {

        }


        /// <summary>
        /// Records a points change using the current account balance and user identity.
        /// </summary>
        /// <param name="account">The account whose points change.</param>
        /// <param name="points">The signed points delta.</param>
        /// <param name="description">The ledger description.</param>
        /// <param name="notRecordIfPointIsZero">Whether a zero delta should be ignored.</param>
        /// <param name="throwIfNotEnoughPoints">Whether a negative balance should throw.</param>
        public void ChangePoint(Account account, decimal points, string description, bool notRecordIfPointIsZero = true, bool throwIfNotEnoughPoints = false)
        {
            ChangePoint(account.Id, account.Points, account.UserName, points, description, notRecordIfPointIsZero, throwIfNotEnoughPoints);
        }

        /// <summary>
        /// Persists a points ledger row, optionally rejects insufficient balance,
        /// and invalidates the account's cached projection.
        /// </summary>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="accountPoints">The balance before the change.</param>
        /// <param name="userName">The cached account key to invalidate.</param>
        /// <param name="points">The signed points delta.</param>
        /// <param name="description">The ledger description.</param>
        /// <param name="notRecordIfPointIsZero">Whether a zero delta should be ignored.</param>
        /// <param name="throwIfNotEnoughPoints">Whether a negative balance should throw.</param>
        /// <exception cref="Exception">Thrown when the requested deduction exceeds the available balance and rejection is enabled.</exception>
        public void ChangePoint(int accountId, decimal accountPoints, string userName, decimal points, string description, bool notRecordIfPointIsZero = true, bool throwIfNotEnoughPoints = false)
        {
            if (notRecordIfPointIsZero && points == 0)
            {
                return;//Legacy comment translated to English.
            }

            if (throwIfNotEnoughPoints && points < 0 && accountPoints + points < 0)
            {
                //Legacy comment translated to English.
                throw new Exception($"积分不足,还需{Convert.ToInt32(accountPoints + points)}积分！");
            }

            var beforePoints = accountPoints;

            var afterPoints = beforePoints + points;

            var pointsLog = new PointsLog()
            {
                AccountId = accountId,
                AddTime = DateTime.Now,
                Description = description,
                Points = points,
                BeforePoints = beforePoints,
                AfterPoints = afterPoints
            };
            this.SaveObject(pointsLog);

            //Legacy comment translated to English.
            var fullAccountCache = _serviceProvider.GetService<FullAccountCache>();
            fullAccountCache.RemoveObject(userName);
        }

        /// <summary>
        /// Reassigns all points ledger rows from one account identifier to another.
        /// </summary>
        /// <param name="oldAccountId">The source account identifier.</param>
        /// <param name="newAccountId">The destination account identifier.</param>
        public void ChangeAccount(int oldAccountId, int newAccountId)
        {
            var qointsLogList = GetFullList(z => z.AccountId == oldAccountId, z => z.Id, OrderingType.Ascending);

            for (int i = 0, count = qointsLogList.Count; i < count; i++)
            {
                var qointsLog = qointsLogList[i];
                using (this.InstanceAutoDetectChangeContextWrap().InstanceCloseAutoDetectChangeContext())
                {
                    qointsLog.AccountId = newAccountId;
                    SaveObject(qointsLog);
                }


            }
        }

        public override void SaveObject(PointsLog obj)
        {
            var isInsert = base.IsInsert(obj);
            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("PointsLog{2}：{0}（ID：{1}）", obj.Description, obj.Id, isInsert ? "新增" : "编辑");
        }
        public override void DeleteObject(PointsLog obj)
        {
            LogUtility.WebLogger.InfoFormat("PointsLog被删除：{0}（ID：{1}）", obj.Description, obj.Id);
            base.DeleteObject(obj);
        }
    }
}


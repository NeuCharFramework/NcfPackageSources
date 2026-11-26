using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Senparc.Core.Email;
using Senparc.Core.Enums;
using Senparc.Core.Extensions;
using Senparc.Core.Models;
using Senparc.Repository;
using Senparc.Log;
using Senparc.Core.Config;

namespace Senparc.Service
{
    public interface IProjectPayLogService : IBaseClientService<ProjectPayLog>
    {
        /// <summary>Gets a payment log by order number.</summary>
        /// <param name="orderNumber">The payment order number.</param>
        /// <param name="includes">Optional related-navigation paths.</param>
        /// <returns>The matching payment log, or <see langword="null"/>.</returns>
        ProjectPayLog GetBuyOrderNumber(string orderNumber, string[] includes = null);
        /// <summary>Creates and persists an unpaid payment order for a project demand.</summary>
        /// <param name="projectDemand">The demand associated with the order.</param>
        /// <param name="description">The order description.</param>
        /// <param name="type">The payment order type.</param>
        /// <returns>The persisted payment log.</returns>
        ProjectPayLog CreateOrder(ProjectDemand projectDemand, string description, int type);
        /// <summary>Marks a payment order as cancelled and closed.</summary>
        /// <param name="projectPayLog">The order to cancel.</param>
        void CancelOrder(ProjectPayLog projectPayLog);
    }

    public class ProjectPayLogService : BaseClientService<ProjectPayLog>, IProjectPayLogService
    {
        public ProjectPayLogService(IProjectPayLogRepository projectPayRepo)
            : base(projectPayRepo)
        {
        }

        private string GetAvalibleOrderNumber()
        {
            string orderNumber = null;
            ProjectPayLog payLog = null;

            do
            {
                orderNumber = "C00{0}".With(DateTime.Now.ToString("yyyyMMddHHmmss"));
                payLog = GetBuyOrderNumber(orderNumber);
            } while (payLog != null);

            return orderNumber;
        }

        public ProjectPayLog GetBuyOrderNumber(string orderNumber, string[] includes = null)
        {
            return this.GetObject(z => z.OrderNumber == orderNumber, includes);
        }

        public ProjectPayLog CreateOrder(ProjectDemand projectDemand, string description, int type)
        {
            decimal payMoney = SiteConfig.PROJECTDMANDDEPOSIT;

            var projectPayLog = new ProjectPayLog()
            {
            OrderNumber = null,
                AddTime = DateTime.Now,
                CompleteTime = DateTime.Now,
                ProjectDemandId = projectDemand.Id,
                PayMoney = (projectDemand.Price == 0 ? payMoney : projectDemand.Price),
                Description = description,
                Status = (int)ProjectPay_Status.未支付,
                Type = type
            };
            SaveObject(projectPayLog);

            ////sendEmail
            //if (account.EmailChecked)
            //{
            //    SendEmail sendEmail = new SendEmail(SendEmailType.OrderCreate);
            //    var sendEmailPara = new SendEmailParameter_OrderCreate(account.Email, account.UserName,
            //                                                            ProjectPayLog.OrderNumber,
            //                                                            ProjectPayLog.PayMoney,
            //                                                            ProjectPayLog.Description);
            //    sendEmail.Send(sendEmailPara, true);
            //}

            return projectPayLog;
        }

        public void CancelOrder(ProjectPayLog projectPayLog)
        {
            projectPayLog.CompleteTime = DateTime.Now;
            projectPayLog.Status = (int)ProjectPay_Status.已取消;
            projectPayLog.Type = (int)ProjectPay_Type.交易关闭;
            SaveObject(projectPayLog);

            //var account = ProjectPayLog.Account;
            //if (account.EmailChecked)
            //{
            //    SendEmail sendEmail = new SendEmail(SendEmailType.OrderCancelled);
            //    var sendEmailPara = new SendEmailParameter_OrderCancelled(account.Email, account.UserName,
            //        ProjectPayLog.OrderNumber,
            //        ProjectPayLog.PayMoney,
            //        ProjectPayLog.Description);
            //    sendEmail.Send(sendEmailPara, true);
            //}
        }

        public override void SaveObject(ProjectPayLog obj)
        {
            var isInsert = base.IsInsert(obj);

            if (isInsert)
            {
                obj.OrderNumber = GetAvalibleOrderNumber();
            }

            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("AccountPayLog{2}：{0}（ID：{1}，当前状态：{3}）", obj.OrderNumber, obj.Id, isInsert ? "新增" : "编辑", (ProjectPay_Status)obj.Status);
        }
        public override void DeleteObject(ProjectPayLog obj)
        {
            LogUtility.WebLogger.InfoFormat("ProjectPay被删除：{0}（ID：{1}）", obj.OrderNumber, obj.Id);
            base.DeleteObject(obj);
        }

    }
}


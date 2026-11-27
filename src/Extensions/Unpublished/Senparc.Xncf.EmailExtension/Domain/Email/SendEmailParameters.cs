/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SendEmailParameters.cs
    File description: SendEmailParameters related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.12.16.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.CO2NET.Extensions;
using System;

namespace Senparc.Ncf.Core.Email
{
    /// <summary>
    /// Supplies account and activation values for a live-code email.
    /// </summary>
    public class SendEmailParameter_LiveCode : SendEmailParameter
    {
        public string Password { get; set; }
        public Guid LiveCode { get; set; }
        public string ReturnUrl { get; set; }
        public string EncodedUserName { get; set; }

        /// <summary>
        /// Initializes live-code email values and derives an encoded user name.
        /// </summary>
        /// <param name="toEmail">The recipient email address.</param>
        /// <param name="userName">The account user name.</param>
        /// <param name="password">The account password.</param>
        /// <param name="liveCode">The live activation code.</param>
        /// <param name="returnUrl">The continuation URL.</param>
        public SendEmailParameter_LiveCode(string toEmail, string userName, string password, Guid liveCode, string returnUrl)
        {
            ToEmail = toEmail; UserName = userName;
            Password = password; LiveCode = liveCode; ReturnUrl = returnUrl;
            EncodedUserName = userName.UrlEncode();
        }
    }
    /// <summary>
    /// Supplies the continuation instruction for a live-code handoff email.
    /// </summary>
    public class SendEmailParameter_PassLiveCode : SendEmailParameter
    {
        public string ContinueToDo { get; set; }
        /// <summary>
        /// Initializes continuation values for a live-code handoff email.
        /// </summary>
        /// <param name="toEmail">The recipient email address.</param>
        /// <param name="userName">The account user name.</param>
        /// <param name="continueToDo">The continuation instruction.</param>
        public SendEmailParameter_PassLiveCode(string toEmail, string userName, string continueToDo)
        {
            ToEmail = toEmail; UserName = userName;
            ContinueToDo = continueToDo;
        }
    }
    /// <summary>
    /// Supplies the reset code for a password-reset email.
    /// </summary>
    public class SendEmailParameter_ResetPassword : SendEmailParameter
    {
        public string Code { get; set; }

        /// <summary>
        /// Initializes password-reset email values.
        /// </summary>
        /// <param name="toEmail">The recipient email address.</param>
        /// <param name="userName">The account user name.</param>
        /// <param name="code">The reset code.</param>
        public SendEmailParameter_ResetPassword(string toEmail, string userName, string code)
        {
            ToEmail = toEmail; UserName = userName; Code = code;
        }
    }


    /// <summary>
    /// Supplies invitation code and registration URL values.
    /// </summary>
    public class SendEmailParameter_InviteCode : SendEmailParameter
    {
        public string Code { get; set; }
        public string RegUrl { get; set; }
        /// <summary>
        /// Initializes invitation-code email values.
        /// </summary>
        /// <param name="toEmail">The recipient email address.</param>
        /// <param name="userName">The account user name.</param>
        /// <param name="code">The invitation code.</param>
        /// <param name="regUrl">The registration URL.</param>
        public SendEmailParameter_InviteCode(string toEmail, string userName, string code, string regUrl)
        {
            ToEmail = toEmail; UserName = userName; Code = code; RegUrl = regUrl;
        }
    }

    /// <summary>
    /// Supplies order details for an order-created email.
    /// </summary>
    public class SendEmailParameter_OrderCreate : SendEmailParameter
    {
        public string OrderNumber { get; set; }
        public string Money { get; set; }
        public string Note { get; set; }
        /// <summary>
        /// Initializes order-created email values and formats the monetary amount.
        /// </summary>
        /// <param name="toEmail">The recipient email address.</param>
        /// <param name="userName">The account user name.</param>
        /// <param name="orderNumber">The order number.</param>
        /// <param name="money">The order amount.</param>
        /// <param name="note">The order note.</param>
        public SendEmailParameter_OrderCreate(string toEmail, string userName, string orderNumber, decimal money, string note)
        {
            ToEmail = toEmail; UserName = userName;
            OrderNumber = orderNumber;
            Money = money.ToString("C");
            Note = note;
        }
    }

    /// <summary>
    /// Supplies order details for a payment-success email.
    /// </summary>
    public class SendEmailParameter_OrderPaySuccess : SendEmailParameter
    {
        public string OrderNumber { get; set; }
        public string Money { get; set; }
        public string Note { get; set; }
        /// <summary>
        /// Initializes payment-success email values and formats the monetary amount.
        /// </summary>
        /// <param name="toEmail">The recipient email address.</param>
        /// <param name="userName">The account user name.</param>
        /// <param name="orderNumber">The order number.</param>
        /// <param name="money">The paid amount.</param>
        /// <param name="note">The payment note.</param>
        public SendEmailParameter_OrderPaySuccess(string toEmail, string userName, string orderNumber, decimal money, string note)
        {
            ToEmail = toEmail; UserName = userName;
            OrderNumber = orderNumber;
            Money = money.ToString("C");
            Note = note;
        }
    }

    /// <summary>
    /// Supplies order details for an order-cancelled email.
    /// </summary>
    public class SendEmailParameter_OrderCancelled : SendEmailParameter
    {
        public string OrderNumber { get; set; }
        public string Money { get; set; }
        public string Note { get; set; }
        /// <summary>
        /// Initializes order-cancelled email values and formats the monetary amount.
        /// </summary>
        /// <param name="toEmail">The recipient email address.</param>
        /// <param name="userName">The account user name.</param>
        /// <param name="orderNumber">The order number.</param>
        /// <param name="money">The cancelled amount.</param>
        /// <param name="note">The cancellation note.</param>
        public SendEmailParameter_OrderCancelled(string toEmail, string userName, string orderNumber, decimal money, string note)
        {
            ToEmail = toEmail; UserName = userName;
            OrderNumber = orderNumber;
            Money = money.ToString("C");
            Note = note;
        }
    }

}

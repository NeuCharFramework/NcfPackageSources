/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SendEmailParameters.cs
    File description: SendEmailParameters related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.CO2NET.Extensions;
using System;

namespace Senparc.Ncf.Core.Email
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SendEmailParameter_LiveCode : SendEmailParameter
    {
        public string Password { get; set; }
        public Guid LiveCode { get; set; }
        public string ReturnUrl { get; set; }
        public string EncodedUserName { get; set; }

        public SendEmailParameter_LiveCode(string toEmail, string userName, string password, Guid liveCode, string returnUrl)
        {
            ToEmail = toEmail; UserName = userName;
            Password = password; LiveCode = liveCode; ReturnUrl = returnUrl;
            EncodedUserName = userName.UrlEncode();
        }
    }
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SendEmailParameter_PassLiveCode : SendEmailParameter
    {
        public string ContinueToDo { get; set; }
        public SendEmailParameter_PassLiveCode(string toEmail, string userName, string continueToDo)
        {
            ToEmail = toEmail; UserName = userName;
            ContinueToDo = continueToDo;
        }
    }
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SendEmailParameter_ResetPassword : SendEmailParameter
    {
        public string Code { get; set; }

        public SendEmailParameter_ResetPassword(string toEmail, string userName, string code)
        {
            ToEmail = toEmail; UserName = userName; Code = code;
        }
    }


    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SendEmailParameter_InviteCode : SendEmailParameter
    {
        public string Code { get; set; }
        public string RegUrl { get; set; }
        public SendEmailParameter_InviteCode(string toEmail, string userName, string code, string regUrl)
        {
            ToEmail = toEmail; UserName = userName; Code = code; RegUrl = regUrl;
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SendEmailParameter_OrderCreate : SendEmailParameter
    {
        public string OrderNumber { get; set; }
        public string Money { get; set; }
        public string Note { get; set; }
        public SendEmailParameter_OrderCreate(string toEmail, string userName, string orderNumber, decimal money, string note)
        {
            ToEmail = toEmail; UserName = userName;
            OrderNumber = orderNumber;
            Money = money.ToString("C");
            Note = note;
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SendEmailParameter_OrderPaySuccess : SendEmailParameter
    {
        public string OrderNumber { get; set; }
        public string Money { get; set; }
        public string Note { get; set; }
        public SendEmailParameter_OrderPaySuccess(string toEmail, string userName, string orderNumber, decimal money, string note)
        {
            ToEmail = toEmail; UserName = userName;
            OrderNumber = orderNumber;
            Money = money.ToString("C");
            Note = note;
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SendEmailParameter_OrderCancelled : SendEmailParameter
    {
        public string OrderNumber { get; set; }
        public string Money { get; set; }
        public string Note { get; set; }
        public SendEmailParameter_OrderCancelled(string toEmail, string userName, string orderNumber, decimal money, string note)
        {
            ToEmail = toEmail; UserName = userName;
            OrderNumber = orderNumber;
            Money = money.ToString("C");
            Note = note;
        }
    }

}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SendEmailParameters.Core.cs
    File description: SendEmailParameters.Core related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Extensions;

namespace Senparc.Ncf.Core.Email
{
    public class SendEmailParameter
    {
        public string ToEmail { get; set; }
        public string UserName { get; set; }
        public string UserNameUrlEncode { get { return UserName.UrlEncode(); } }
        //public string Year { get { return DateTime.Now.Year.ToString(); } }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SendEmailParameter_Test : SendEmailParameter
    {
        public string TestType { get; set; }
        public string Domain { get; set; }
        public string EmailType { get; set; }
        public SendEmailParameter_Test(string toEmail, string userName, string testType, string domain, string emailType)
        {
            ToEmail = toEmail; UserName = userName; TestType = testType; Domain = domain; EmailType = emailType;
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SendEmailParameter_CustomEmail : SendEmailParameter
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public SendEmailParameter_CustomEmail(string toEmail, string userName, string title, string content)
        {
            ToEmail = toEmail; UserName = userName;
            Title = title; Content = content;
        }
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SendEmailParameters.Core.cs
    File description: SendEmailParameters.Core related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.12.16.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Extensions;

namespace Senparc.Ncf.Core.Email
{
    /// <summary>
    /// Base values shared by typed email-template parameter objects.
    /// </summary>
    public class SendEmailParameter
    {
        public string ToEmail { get; set; }
        public string UserName { get; set; }
        public string UserNameUrlEncode { get { return UserName.UrlEncode(); } }
        //public string Year { get { return DateTime.Now.Year.ToString(); } }
    }

    /// <summary>
    /// Supplies values for a test-email template.
    /// </summary>
    public class SendEmailParameter_Test : SendEmailParameter
    {
        public string TestType { get; set; }
        public string Domain { get; set; }
        public string EmailType { get; set; }
        /// <summary>
        /// Initializes test-email template values.
        /// </summary>
        /// <param name="toEmail">The recipient email address.</param>
        /// <param name="userName">The account user name.</param>
        /// <param name="testType">The test scenario.</param>
        /// <param name="domain">The test domain.</param>
        /// <param name="emailType">The email type label.</param>
        public SendEmailParameter_Test(string toEmail, string userName, string testType, string domain, string emailType)
        {
            ToEmail = toEmail; UserName = userName; TestType = testType; Domain = domain; EmailType = emailType;
        }
    }

    /// <summary>
    /// Supplies title and content for a custom email.
    /// </summary>
    public class SendEmailParameter_CustomEmail : SendEmailParameter
    {
        public string Title { get; set; }
        public string Content { get; set; }
        /// <summary>
        /// Initializes a custom email subject and content.
        /// </summary>
        /// <param name="toEmail">The recipient email address.</param>
        /// <param name="userName">The account user name.</param>
        /// <param name="title">The custom email title.</param>
        /// <param name="content">The custom email content.</param>
        public SendEmailParameter_CustomEmail(string toEmail, string userName, string title, string content)
        {
            ToEmail = toEmail; UserName = userName;
            Title = title; Content = content;
        }
    }
}

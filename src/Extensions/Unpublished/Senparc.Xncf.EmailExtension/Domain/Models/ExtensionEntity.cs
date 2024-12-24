using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.EmailExtension.Domain.Models
{
    #region Email

    /// <summary>
    /// 自动发送
    /// </summary>
    public class AutoSendEmail
    {
        public int Id { get; set; }

        public string Address { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public string UserName { get; set; }

        public DateTime LastSendTime { get; set; }

        public int SendCount { get; set; }
    }

    /// <summary>
    /// 自动发送完成
    /// </summary>
    public class AutoSendEmailBak
    {
        public int Id { get; set; }

        public string Address { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public string UserName { get; set; }

        public DateTime SendTime { get; set; }
    }

    public class EmailUser
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string EmailAddress { get; set; }

        public string Password { get; set; }

        public string SmtpHost { get; set; }

        public int SmtpPort { get; set; }

        public bool NeedCredentials { get; set; }

        public string Note { get; set; }
    }

    #endregion


    #region XML Config格式
    /// <summary>
    /// Email
    /// </summary>
    public class XmlConfig_Email
    {
        public string ToUse { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }

        public string Holders { get; set; }

        public DateTime UpdateTime { get; set; }
    }

    #endregion
}

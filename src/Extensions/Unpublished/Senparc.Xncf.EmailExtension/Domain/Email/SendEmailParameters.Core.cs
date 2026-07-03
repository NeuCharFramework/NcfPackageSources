/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SendEmailParameters.Core.cs
    文件功能描述：SendEmailParameters.Core 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
    /// 测试邮件发送
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
    /// 自定义邮件
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

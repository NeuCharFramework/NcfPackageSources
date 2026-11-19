/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SendEmail.cs
    File description: SendEmail related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;

namespace Senparc.Ncf.Core.Email
{
    using Senparc.CO2NET.Extensions;
    using Senparc.Ncf.Core.Enums;
    using Senparc.Ncf.Core.Utility;
    using Senparc.Ncf.Log;
    using Senparc.Xncf.EmailExtension.Domain.Cache;
    using Senparc.Xncf.EmailExtension.Domain.Models;
    using Senparc.Xncf.EmailExtension.Domain.Xml;
    using System.IO;

    interface ISendEmail
    {
        bool DoSendEmail(string to, string subject, string body, string username, bool useEmailTemplate, EmailAccountType emailUserType, bool fromCache);
        bool Send<T>(T sendEmailParameter, bool lineInCache) where T : SendEmailParameter;
        string ReplaceHolderValue<T>(T sendEmailParameter, string content) where T : SendEmailParameter;
        string GetFilledSubject<T>(T sendEmailParameter) where T : SendEmailParameter;
        string GetFilledBody<T>(T sendEmailParameter) where T : SendEmailParameter;
    }

    public class SendEmail : ISendEmail
    {
        private XmlConfig_Email ConfigNode { get; set; }
        private string Subject { get; set; }
        private string Body { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string SentSubject { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string SentBody { get; set; }


        public bool UseEmailTemplate { get; set; }

        public SendEmail(SendEmailType? toUse)
        {
            if (toUse != null)
            {
                ConfigNode = EmailXmlConfig.GetEmailConfig((SendEmailType)toUse);//Legacy comment translated to English.
                Subject = ConfigNode.Subject;
                Body = ConfigNode.Body.HtmlDecode();
            }
            UseEmailTemplate = true;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        private EmailUser GetEmailUser(EmailAccountType emailUserType)
        {
            //emailUserType = emailUserType == EmailUserType.Default ? EmailUserType._163 : emailUserType;

            XmlDataContext xmlCtx = new XmlDataContext();
            var emailUserList = xmlCtx.GetXmlList<EmailUser>();
            var emailUser = emailUserList.FirstOrDefault(z => z.Name == emailUserType.ToString());
            if (emailUser == null)
            {
                LogUtility.EmailLogger.ErrorFormat("EmailUserType类型错误。传入类型：{0}", emailUserType.ToString());
            }
            return emailUser;
        }

        #region Legacy section

        public bool DoSendEmail(string to, string subject, string body, string username, bool useEmailTemplate, EmailAccountType emailUserType, bool fromCache)
        {
            bool sendSuccess = false;
            string smtpEmailAddress = null;
            //if (emailUserType == EmailUserType.Default && to.ToUpper().EndsWith("@QQ.COM"))
            //{
            //Legacy comment translated to English.
            //    emailUserType = EmailUserType._163;
            //}

            //if (emailUserType == EmailUserType.Default)
            //{
            //    emailUserType = EmailUserType.KJH8;//defaultuseKJH8send
            //}

            try
            {
                body = this.GetEmailTemplate(body, useEmailTemplate);

                EmailUser targetEmailUser = this.GetEmailUser(emailUserType);
                smtpEmailAddress = targetEmailUser.EmailAddress;

                MailMessage m_message = new MailMessage();
                m_message.From = new MailAddress(targetEmailUser.EmailAddress, targetEmailUser.DisplayName);
                m_message.To.Add(new MailAddress(to));
                m_message.Subject = subject;
                m_message.IsBodyHtml = true;//Legacy comment translated to English.
                m_message.Body = body;

                SmtpClient m_smtpClient = new SmtpClient();
                m_smtpClient.Host = targetEmailUser.SmtpHost;
                m_smtpClient.Port = targetEmailUser.SmtpPort;
                if (targetEmailUser.NeedCredentials)
                {
                    m_smtpClient.Credentials = new System.Net.NetworkCredential(targetEmailUser.EmailAddress, targetEmailUser.Password);
                }

                m_smtpClient.Send(m_message);

                sendSuccess = true;
            }
            catch (Exception e)
            {
                LogUtility.EmailLogger.ErrorFormat($"TO:{to},Subject:{subject},Username:{username},Message:{e.Message}", e);
                sendSuccess = false;
            }
            finally
            {
                string cacheUsed = fromCache ? "缓存" : "直接发送";
                if (sendSuccess)
                {
                    //Legacy comment translated to English.
                    XmlDataContext xmlCtx = new XmlDataContext();
                    AutoSendEmailBak emailBak = new AutoSendEmailBak()
                    {
                        Address = to,
                        Body = body,
                        Subject = subject,
                        SendTime = DateTime.Now,
                        UserName = username
                    };
                    xmlCtx.Insert(emailBak);

                    LogUtility.EmailLogger.Info($"发送Email成功({cacheUsed})。To:{to}，Subject:{subject}，UserName:{username}。by:{smtpEmailAddress}");
                }
                else
                {
                    LogUtility.EmailLogger.Error($"发送Email失败({cacheUsed})。To:{to}，Subject:{subject}，UserName:{username}。by:{smtpEmailAddress}");
                }
            }
            return sendSuccess;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sendEmailParameter"></param>
        /// <param name="lineInCache"></param>
        /// <returns></returns>
        public bool Send<T>(T sendEmailParameter, bool lineInCache) where T : SendEmailParameter
        {
            return this.Send(sendEmailParameter, lineInCache, true, EmailAccountType.Default);
        }

        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="sendEmailParameter"></param>
        ///// <param name="lineInCache"></param>
        ///Legacy comment translated to English.
        ///// <returns></returns>
        //public bool Send<T>(T sendEmailParameter, bool lineInCache, bool useBackupEmail) where T : SendEmailParameter
        //{
        //    return this.Send(sendEmailParameter, lineInCache, true, useBackupEmail);
        //}

        /// <summary>
        /// sendEmail
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sendEmailParameter"></param>
        /// <param name="lineInCache"></param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        public bool Send<T>(T sendEmailParameter, bool lineInCache, bool sendImmediately, EmailAccountType emailUserType) where T : SendEmailParameter
        {
            SentSubject = null;
            SentBody = null;
            try
            {
                if (sendEmailParameter.ToEmail.IsNullOrEmpty() /*|| !sendEmailParameter.ToEmail.ValidateEmailFormat()*/)
                {
                    throw new Exception("ToEmail参数为空");
                }

                //Legacy comment translated to English.
                SentSubject = ReplaceHolderValue(sendEmailParameter, Subject);
                SentBody = ReplaceHolderValue(sendEmailParameter, Body);

                bool sendImmediatelySuccess = true;
                if (!lineInCache)
                {
                    //Legacy comment translated to English.
                    sendImmediatelySuccess = DoSendEmail(sendEmailParameter.ToEmail, SentSubject, SentBody, sendEmailParameter.UserName, true, emailUserType, lineInCache);
                }

                //Legacy comment translated to English.
                if (lineInCache || !sendImmediatelySuccess)
                {
                    //Legacy comment translated to English.
                    SendEmailCache cache = new SendEmailCache();
                    AutoSendEmail email = new AutoSendEmail
                    {
                        UserName = sendEmailParameter.UserName,
                        Address = sendEmailParameter.ToEmail,
                        Subject = SentSubject,
                        Body = this.GetEmailTemplate(SentBody, this.UseEmailTemplate),//Legacy comment translated to English.
                        SendCount = sendImmediately ? 0 : (Senparc.Xncf.EmailExtension.Register.MaxSendEmailTimes + 200),//Legacy comment translated to English.
                        LastSendTime = DateTime.Now
                    };
                    bool insertSuccess = cache.InsertEmail(email);
                    if (!insertSuccess)
                    {
                        LogUtility.EmailLogger.Error($"Email加入缓存失败！Email:{sendEmailParameter.ToEmail},Subject:{SentSubject}\r\n内容：\r\n{SentBody}");
                    }
                    return insertSuccess;
                }
                else
                {
                    return sendImmediatelySuccess;
                }
            }
            catch (Exception e)
            {
                LogUtility.EmailLogger.ErrorFormat(
                    $"发送Email出错：{e.Message}，Email:{sendEmailParameter.ToEmail},Username:{sendEmailParameter.UserName}，" +
                    $"Subject:{SentSubject}\r\n内容：\r\n{SentBody}", e);
                return false;
            }
        }

        public string ReplaceHolderValue<T>(T sendEmailParameter, string content) where T : SendEmailParameter
        {
            foreach (var member in sendEmailParameter.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty))
            {
                object value = member.GetValue(sendEmailParameter, null);
                string valueString = (value ?? "").ToString();
                //switch (value.GetType().Name)
                //{
                //    case "Double":
                //        valueString = ((double)value).ToString("###,###.###");
                //        break;
                //    case "Int16":
                //    case "Int32":
                //    case "Int64":
                //Legacy comment translated to English.
                //        break;
                //    default:
                //        valueString = value.ToString();
                //        break;
                //}
                content = content.Replace("{$" + member.Name.ToLower() + "$}", valueString);
            }
            content = content.Replace("{$now$}", DateTime.Now.ToString());
            return content;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sendEmailParameter"></param>
        /// <returns></returns>
        public string GetFilledSubject<T>(T sendEmailParameter) where T : SendEmailParameter
        {
            return ReplaceHolderValue(sendEmailParameter, this.Subject);
        }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sendEmailParameter"></param>
        /// <returns></returns>
        public string GetFilledBody<T>(T sendEmailParameter) where T : SendEmailParameter
        {
            return ReplaceHolderValue(sendEmailParameter, this.Body);
        }


        #endregion


        #region Legacy section

        private string GetEmailTemplate(string body, bool usingEmailTemplate)
        {
            if (usingEmailTemplate)
            {
                //Legacy comment translated to English.
                using (var srEmailTemplate = new StreamReader(CO2NET.Utilities.ServerUtility.ContentRootMapPath("~/App_Data/Template/EmailTemplete.htm"), Encoding.UTF8))
                {
                    string emailTemplate = srEmailTemplate.ReadToEnd();
                    this.InsertTemplateValue(ref emailTemplate, "body", body);
                    this.InsertTemplateValue(ref emailTemplate, "longdate", DateTime.Now.ToLongDateString());
                    this.InsertTemplateValue(ref emailTemplate, "year", DateTime.Now.Year.ToString());
                    body = emailTemplate;
                }
            }
            return body;
        }

        private void InsertTemplateValue(ref string template, string valueName, string value)
        {
            if (template.IsNullOrEmpty())
            {
                throw new Exception("Email模板未载入！");
            }
            template = template.Replace("{$" + valueName.ToLower() + "$}", value);
        }
        #endregion


    }


    public class SendEmailFactory
    {

    }
}

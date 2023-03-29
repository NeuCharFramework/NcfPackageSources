using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Enums;
using Senparc.Xncf.EmailExtension.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Senparc.Xncf.EmailExtension.Domain.Xml
{
    public partial class EmailXmlConfig
    {
        #region Email

        private const string EMAIL_PATH = "~/App_Data/email.config";

        public static List<XmlConfig_Email> GetEmailConfigs()
        {
            XElement xml = XmlConfig.GetXElement(EMAIL_PATH);

            List<XmlConfig_Email> listConfig =
                (from x in xml.Elements("MailConfig")
                 select new XmlConfig_Email
                 {
                     ToUse = x.Element("ToUse").Value,
                     Subject = x.Element("Subject").Value,
                     Body = x.Element("Body").Value,
                     Holders = x.Element("Holders").Value,
                     UpdateTime = DateTime.Parse(x.Element("UpdateTime").Value)
                 }).ToList();
            return listConfig;
        }

        /// <summary>
        /// 获取Email格式XML的节点
        /// </summary>
        /// <param name="ToUse">用途标记，ToUse节点内容</param>
        /// <returns></returns>
        public static XmlConfig_Email GetEmailConfig(SendEmailType toUse)
        {
            string filename = XmlConfig.GetMapPath(EMAIL_PATH);
            XElement xml = XElement.Load(filename);
            XElement configNode = xml.Elements("MailConfig").First(z => z.Element("ToUse").Value == toUse.ToString());
            return new XmlConfig_Email()
            {
                ToUse = configNode.Element("ToUse").Value,
                Subject = configNode.Element("Subject").Value,
                Body = configNode.Element("Body").Value,
                Holders = configNode.Element("Holders").Value,
                UpdateTime = DateTime.Parse(configNode.Element("UpdateTime").Value)
            };
        }

        public static bool SaveEmailConfig(XmlConfig_Email config)
        {
            try
            {
                string filename = XmlConfig.GetMapPath(EMAIL_PATH);
                XElement xml = XElement.Load(filename);
                XElement oldConfigElement = xml.Elements("MailConfig").FirstOrDefault(x => x.Element("ToUse").Value == config.ToUse);
                XElement newConfigElement = new XElement("MailConfig",
                                                new XElement("ToUse", config.ToUse),
                                                new XElement("Subject", config.Subject),
                                                new XElement("Body", new XCData(config.Body)),
                                                new XElement("Holders", config.Holders),
                                                new XElement("UpdateTime", DateTime.Now.ToString())
                                                );

                if (oldConfigElement == null)
                {
                    xml.Add(newConfigElement);
                }
                else
                {
                    oldConfigElement.ReplaceNodes(newConfigElement.Elements());
                }

                xml.Save(filename);

                //备份
                string newPath = Path.Combine(Path.GetDirectoryName(filename), "email_bak");
                string newFileName = Path.GetFileName(filename) +
                                     $".bak.{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.{config.ToUse}.config";
                xml.Save(Path.Combine(newPath, newFileName));
                return true;
            }
            catch
            {
                throw;
                //return false;
            }
        }
        #endregion
    }


}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：EmailXmlDataContext.cs
    文件功能描述：EmailXmlDataContext 相关实现
    
    
    创建标识：Senparc - 20211221
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Utility;
using Senparc.Xncf.EmailExtension.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.EmailExtension.Domain
{
    public class EmailXmlDataContext
    {

        public XmlDataContext XmlDataContext { get; set; }

        public EmailXmlDataContext()
        {
            XmlDataContext = new XmlDataContext();
        }

        public List<AutoSendEmail> GetAutoSendEmailList()
        {
            return XmlDataContext.GetXmlList<AutoSendEmail>();
        }

        public List<AutoSendEmailBak> GetAutoSendEmailBakList()
        {
            return XmlDataContext.GetXmlList<AutoSendEmailBak>();
        }
    }
}

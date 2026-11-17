/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: EmailXmlDataContext.cs

    File description: EmailXmlDataContext related implementation

    

    

    Created by: Senparc - 20211221

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: EmailXmlDataContext.cs
    File description: EmailXmlDataContext related implementation

    Created by: Senparc - 20211221

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.12.16.2 Improve source documentation, headers, and module metadata consistency.

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

        /// <summary>
        /// Initializes an email XML data context backed by the shared XML storage helper.
        /// </summary>
        public EmailXmlDataContext()
        {
            XmlDataContext = new XmlDataContext();
        }

        /// <summary>
        /// Loads pending automatic email records from XML storage.
        /// </summary>
        /// <returns>The persisted automatic-email records.</returns>
        public List<AutoSendEmail> GetAutoSendEmailList()
        {
            return XmlDataContext.GetXmlList<AutoSendEmail>();
        }

        /// <summary>
        /// Loads backup automatic-email records from XML storage.
        /// </summary>
        /// <returns>The persisted backup records.</returns>
        public List<AutoSendEmailBak> GetAutoSendEmailBakList()
        {
            return XmlDataContext.GetXmlList<AutoSendEmailBak>();
        }
    }
}

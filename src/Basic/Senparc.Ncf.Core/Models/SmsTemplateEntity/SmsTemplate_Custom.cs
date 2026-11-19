/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SmsTemplate_Custom.cs
    File description: SmsTemplate_Custom related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System.ComponentModel;

namespace Senparc.Ncf.Core.Models
{
    public partial class SmsTemplate_Custom : SmsTemplate_Base
    {
        [Description("联系人姓名")]
        public virtual string PersonName { get; set; }

        [Description("联系人头衔")]
        public virtual string PersonTitle { get; set; }
    }
}
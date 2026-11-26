/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinTemplate_AuthNotice.cs
    File description: WeixinTemplate_AuthNotice related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;

namespace Senparc.Xncf.WeixinManager.WeixinTemplate
{
    /// <summary>
    /// Represents the Weixin template-message payload used to report an application audit result.
    /// </summary>
    public class WeixinTemplate_AppAuditNotice : WeixinTemplateBase
    {
        /*

        {{first.DATA}}

        Legacy comment translated to English.

        Legacy comment translated to English.

        Legacy comment translated to English.

        {{remark.DATA}}

        * 

        Legacy comment translated to English.

        Legacy comment translated to English.

        Legacy comment translated to English.

        Legacy comment translated to English.

        Legacy comment translated to English.

        */
        public TemplateDataItem first { get; set; }

        public TemplateDataItem keyword1 { get; set; }

        public TemplateDataItem keyword2 { get; set; }

        public TemplateDataItem keyword3 { get; set; }

        public TemplateDataItem remark { get; set; }

        /// <summary>
        /// Initializes the audit-notice template with its first, keyword, and remark values.
        /// </summary>
        /// <param name="_first">The primary audit subject text.</param>
        /// <param name="_keyword1">The first audit detail.</param>
        /// <param name="_keyword2">The second audit detail.</param>
        /// <param name="_keyword3">The third audit detail.</param>
        /// <param name="_remark">The final audit remark.</param>
        public WeixinTemplate_AppAuditNotice(string _first, string _keyword1, string _keyword2, string _keyword3,
            string _remark) : base("_1_gRx85XBc7OGY4dw9D-fyW4XEeu6Q5URn_ikKH2UD", "审核结果通知")
        {
            first = new TemplateDataItem(_first);
            keyword1 = new TemplateDataItem(_keyword1);
            keyword2 = new TemplateDataItem(_keyword2);
            keyword3 = new TemplateDataItem(_keyword3);
            remark = new TemplateDataItem(_remark);
        }
    }
}

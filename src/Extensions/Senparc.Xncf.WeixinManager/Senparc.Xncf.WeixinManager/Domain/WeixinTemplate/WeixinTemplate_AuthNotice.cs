using Senparc.Weixin.MP.AdvancedAPIs.TemplateMessage;

namespace Senparc.Xncf.WeixinManager.WeixinTemplate
{
    /// <summary>
    /// Notification of audit results
    /// </summary>
    public class WeixinTemplate_AppAuditNotice : WeixinTemplateBase
    {
        /*
        {{first.DATA}}
        Review matters: {{keyword1.DATA}}
        Review status: {{keyword2.DATA}}
        Review time: {{keyword3.DATA}}
        {{remark.DATA}}
        * 
        Your review request has been processed.
        Review matters: Organization application for registration
        Review status: Passed
        Review time: 18:17 on February 5, 2017
        Click to view details
        */
        public TemplateDataItem first { get; set; }

        public TemplateDataItem keyword1 { get; set; }

        public TemplateDataItem keyword2 { get; set; }

        public TemplateDataItem keyword3 { get; set; }

        public TemplateDataItem remark { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_first"></param>
        /// <param name="_keyword1">Authentication details</param>
        /// <param name="_keyword2">Authentication result</param>
        /// <param name="_remark"></param>
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
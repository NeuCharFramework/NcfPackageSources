/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinTemplateBase.cs
    File description: WeixinTemplateBase related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

namespace Senparc.Xncf.WeixinManager.WeixinTemplate
{
    public interface IWeixinTemplateBase
    {
        string TemplateId { get; set; }

        string TemplateName { get; set; }
    }


    public class WeixinTemplateBase : IWeixinTemplateBase
    {
        public string TemplateId { get; set; }

        public string TemplateName { get; set; }

        public WeixinTemplateBase(string templateId, string templateName)
        {
            TemplateId = templateId;
            TemplateName = templateName;
        }
    }
}
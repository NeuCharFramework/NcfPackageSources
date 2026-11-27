/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinTemplateBase.cs
    File description: WeixinTemplateBase related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

    Modified by: Senparc - 20261127
    Change description: v0.25.1.2 Improve source documentation, headers, and module metadata consistency.

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

        /// <summary>
        /// Initializes the base values required by a Weixin template message.
        /// </summary>
        /// <param name="templateId">The Weixin template identifier.</param>
        /// <param name="templateName">The template display name used for diagnostics.</param>
        public WeixinTemplateBase(string templateId, string templateName)
        {
            TemplateId = templateId;
            TemplateName = templateName;
        }
    }
}

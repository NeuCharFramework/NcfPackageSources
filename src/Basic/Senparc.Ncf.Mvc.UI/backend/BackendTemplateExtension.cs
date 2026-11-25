/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BackendTemplateExtension.cs
    File description: BackendTemplateExtension related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.22.9-preview13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Models.VD;
using System.Text;

namespace Senparc.Ncf.Mvc.UI
{
    public static class BackendTemplateExtension
    {
        /// <summary>
        /// Writes a content box with the default tab container enabled.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper whose view writer receives the opening markup.</param>
        /// <param name="title">The content-box heading.</param>
        /// <param name="tabs">The tab markup or labels to render in the header.</param>
        /// <returns>A disposable content-box writer that closes the emitted markup.</returns>
        public static ContentBox ContentBox(this HtmlHelper htmlHelper, string title, params object[] tabs)
        {
            return ContentBox(htmlHelper, title, true, tabs);
        }

        /// <summary>
        /// Writes a content box and optionally emits its default tab container.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper whose view writer receives the opening markup.</param>
        /// <param name="title">The content-box heading.</param>
        /// <param name="showDefaultTabContainer">Whether to emit the default tab container.</param>
        /// <param name="tabs">The tab markup or labels to render in the header.</param>
        /// <returns>A disposable content-box writer that closes the emitted markup.</returns>
        public static ContentBox ContentBox(this HtmlHelper htmlHelper, string title, bool showDefaultTabContainer, params object[] tabs)
        {
            StringBuilder tabsCollection = new StringBuilder();
            foreach (var tab in tabs)
            {
                tabsCollection.AppendFormat("<li>{0}</li>", tab);
            }

            var header = ($@"
<div class=""content-box"">
    <!-- Start Content Box -->
    <div class=""content-box-header"">
        <h3>
            {title}</h3>
<ul class=""content-box-tabs {tabsCollection}"">
            {(tabsCollection.Length == 0 ? "hide" : "")}
        </ul>
        <div class=""clear"">
        </div>
    </div>
    <!-- End .content-box-header -->
<div class=""content-box-content"">" +
(showDefaultTabContainer ?
@"
        <div class=""tab-content default-tab"" id=""tab1"">
            <!-- This is the target div. id must match the href of this div's tab -->"
: ""));
            htmlHelper.ViewContext.Writer.Write(header);

            ContentBox contentBox = new ContentBox(htmlHelper.ViewContext, showDefaultTabContainer);
            return contentBox;
        }

        /// <summary>
        /// Converts model messages into client-side alert calls.
        /// </summary>
        /// <param name="helper">The HTML helper providing the current view model.</param>
        /// <returns>Rendered script markup, or <see langword="null"/> when the model is not a UI view model.</returns>
        public static HtmlString RenderMessageBox(this IHtmlHelper helper)
        {
            if (!(helper.ViewData.Model is IBaseUiVD model))
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();


            if (model.MessagerList != null && model.MessagerList.Count > 0)
            {
                sb.Append("<script>");
                sb.Append("$(function(){");
                foreach (var item in model.MessagerList)
                {
                    sb.Append($"base.alert('{item.MessageType}','{item.MessageText}');");
                }
                sb.Append("});");
                sb.Append("</script>");
            }
            return new HtmlString(sb.ToString());
        }

        /// <summary>
        /// Converts developer messages into diagnostic alert markup.
        /// </summary>
        /// <param name="helper">The HTML helper providing the current view model.</param>
        /// <returns>Rendered HTML, or <see langword="null"/> when the model is not a UI view model.</returns>
        public static HtmlString RenderDeveloperMessageBox(this HtmlHelper helper)
        {
            if (!(helper.ViewData.Model is IBaseUiVD model))
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            if (model.MessagerList != null)
            {
                foreach (var item in model.MessagerList)
                {
                    sb.Append(helper.DeveloperMessageBox(item.MessageType, item.MessageText));
                }
            }
            return new HtmlString(sb.ToString());
        }

        /// <summary>
        /// Renders one developer message using an alert style derived from its message type.
        /// </summary>
        /// <param name="helper">The HTML helper used to preserve extension-method call compatibility.</param>
        /// <param name="messageType">The message category controlling the icon and CSS class.</param>
        /// <param name="messageText">The message text to place in the alert markup.</param>
        /// <returns>The generated alert HTML.</returns>
        public static HtmlString DeveloperMessageBox(this IHtmlHelper helper, MessageType messageType, string messageText)
        {
            string icon = messageType == MessageType.success ? "exclamation" : "remove";
            string format = $@"
<div class=""alert alert-{messageType.ToString()}"">
<i class=""icon-{icon}-sign""></i>
{messageText}
</div>";
            return new HtmlString(format);
        }
    }
}

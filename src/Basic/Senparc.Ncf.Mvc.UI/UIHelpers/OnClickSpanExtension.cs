/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: OnClickSpanExtension.cs
    File description: OnClickSpanExtension related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.22.9-preview13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System.Text;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace System.Web.Mvc
{
    /// <summary>
    /// Provides helpers for rendering anchor elements that invoke caller-supplied
    /// JavaScript handlers.
    /// </summary>
    public static class OnClickSpanExtension
    {
        /// <summary>
        /// Renders an anchor with no additional HTML attributes and the default CSS class.
        /// </summary>
        /// <param name="helper">The MVC HTML helper associated with the current view.</param>
        /// <param name="text">The visible anchor text.</param>
        /// <param name="onclickMethod">The JavaScript expression placed in the <c>onclick</c> attribute.</param>
        /// <returns>The generated anchor markup.</returns>
        public static string OnClickSpan(this HtmlHelper helper, string text, string onclickMethod)
        {
            return OnClickSpan(helper, text, onclickMethod, null, null);
        }

        /// <summary>
        /// Renders an anchor with caller-supplied HTML attributes and the default CSS class.
        /// </summary>
        /// <param name="helper">The MVC HTML helper associated with the current view.</param>
        /// <param name="text">The visible anchor text.</param>
        /// <param name="onclickMethod">The JavaScript expression placed in the <c>onclick</c> attribute.</param>
        /// <param name="htmlAttributes">An object whose public properties become HTML attributes.</param>
        /// <returns>The generated anchor markup.</returns>
        public static string OnClickSpan(this HtmlHelper helper, string text, string onclickMethod, object htmlAttributes)
        {
            return OnClickSpan(helper, text, onclickMethod, null, htmlAttributes);
        }

        /// <summary>
        /// Renders an anchor whose click handler, CSS class, and HTML attributes are
        /// supplied by the caller.
        /// </summary>
        /// <param name="helper">The MVC HTML helper associated with the current view.</param>
        /// <param name="text">The visible anchor text.</param>
        /// <param name="onclickMethod">The JavaScript expression placed in the <c>onclick</c> attribute.</param>
        /// <param name="cssClass">The CSS class value; <c>onclick</c> is used when null.</param>
        /// <param name="htmlAttributes">An object whose public properties become HTML attributes.</param>
        /// <returns>The generated anchor markup.</returns>
        /// <remarks>
        /// This helper only creates markup. It does not validate or execute
        /// <paramref name="onclickMethod"/>.
        /// </remarks>
        public static string OnClickSpan(this HtmlHelper helper, string text, string onclickMethod, string cssClass, object htmlAttributes)
        {
            cssClass = cssClass ?? "onclick";
            //property
            string setHash = htmlAttributes.ToAttributeList();
            string attributeList = string.Empty;
            if (setHash != null)
                attributeList = setHash;

            StringBuilder html = new StringBuilder();
            html.AppendFormat("<a href=\"javascript:void(0);\"  onclick=\"{0}\" {1}>{2}</a>", onclickMethod.Replace("\"","\\\'"), attributeList, text);

            return html.ToString();
        }
    }
}

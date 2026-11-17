/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: OnClickSpanExtension.cs
    File description: OnClickSpanExtension related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System.Text;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace System.Web.Mvc
{

    public static class OnClickSpanExtension
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="helper"></param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static string OnClickSpan(this HtmlHelper helper, string text, string onclickMethod)
        {
            return OnClickSpan(helper, text, onclickMethod, null, null);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="helper"></param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static string OnClickSpan(this HtmlHelper helper, string text, string onclickMethod, object htmlAttributes)
        {
            return OnClickSpan(helper, text, onclickMethod, null, htmlAttributes);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="helper"></param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static string OnClickSpan(this HtmlHelper helper, string text, string onclickMethod, string cssClass, object htmlAttributes)
        {
            //Legacy comment translated to English.
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

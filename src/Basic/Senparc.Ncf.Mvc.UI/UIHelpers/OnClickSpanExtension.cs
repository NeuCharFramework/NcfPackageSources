using System.Text;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace System.Web.Mvc
{

    public static class OnClickSpanExtension
    {
        /// <summary>
        /// span for Onclick operation (the style defaults to "onclick")
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="onclickMethod">Event name (everything in onclick)</param>
        /// <param name="text">Text</param>
        /// <returns></returns>
        public static string OnClickSpan(this HtmlHelper helper, string text, string onclickMethod)
        {
            return OnClickSpan(helper, text, onclickMethod, null, null);
        }

        /// <summary>
        /// span for Onclick operation (the style defaults to "onclick")
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="onclickMethod">Event name (everything in onclick)</param>
        /// <param name="text">Text</param>
        /// <returns></returns>
        public static string OnClickSpan(this HtmlHelper helper, string text, string onclickMethod, object htmlAttributes)
        {
            return OnClickSpan(helper, text, onclickMethod, null, htmlAttributes);
        }

        /// <summary>
        /// span for Onclick operation
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="cssClass">class style, if empty, use "onclick"</param>
        /// <param name="onclickMethod">Event name (everything in onclick)</param>
        /// <param name="text">Text</param>
        /// <returns></returns>
        public static string OnClickSpan(this HtmlHelper helper, string text, string onclickMethod, string cssClass, object htmlAttributes)
        {
            //style
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

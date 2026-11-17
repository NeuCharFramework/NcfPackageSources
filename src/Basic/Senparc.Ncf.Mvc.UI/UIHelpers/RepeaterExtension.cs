/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RepeaterExtension.cs
    File description: RepeaterExtension related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace System.Web.Mvc
{
    public static class RepeaterExtension
    {
        public class SingleRepeater : IDisposable
        {
            protected HttpContext _context;

            //bool wroteStartTag = false, wroteEndTag = false;

            string startTag, endTag;//Legacy comment translated to English.
            public string itemStartTag, itemEndTag;//Legacy comment translated to English.
            public StringBuilder body = new StringBuilder();

            public SingleRepeater(HttpContext context, RepeaterMode repeaterMode, object htmlAttributes)
            {
                this._context = context;//Legacy comment translated to English.
                switch (repeaterMode)
                {
                    case RepeaterMode.None:
                        startTag = string.Empty;
                        itemStartTag = string.Empty;
                        itemEndTag = string.Empty;
                        endTag = string.Empty;
                        break;
                    case RepeaterMode.Table:
                        startTag = "<table {0}><tbody>";
                        itemStartTag = "<td>";
                        itemEndTag = "</td>";
                        endTag = "</tobdy></table>";
                        break;
                    case RepeaterMode.Div:
                        startTag = "<div {0}>";
                        itemStartTag = "<div>";
                        itemEndTag = "</div>";
                        endTag = "</div>";
                        break;
                    case RepeaterMode.Ul:
                        startTag = "<ul {0}>";
                        itemStartTag = "<li>";
                        itemEndTag = "</li>";
                        endTag = "</ul>";
                        break;
                }

                //property
                var setHash = htmlAttributes.ToAttributeList();

                string attributeList = string.Empty;
                if (setHash != null)
                    attributeList = setHash;

                startTag = string.Format(startTag, attributeList);

                WriteStartTag();
            }

            public void WriteStartTag()
            {
                body.Append(startTag);
            }

            public void WriteEndTag()
            {
                body.Append(endTag);
            }

            public override string ToString()
            {
                return body.ToString();
            }

            #region Legacy section

            public void Dispose()
            {
                WriteEndTag();
            }

            #endregion
        }

        /// <summary>
        /// Repeater
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="helper"></param>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static string Repeater<T>(this HtmlHelper helper, IEnumerable<T> dataSource,
            string header, Expression<Func<T, int, string>> itemTempletes, Expression<Func<T, string>> separatorTemplate, string footer,
            RepeaterMode repeaterMode, int colCount, object htmlAttributes)
        {
            if (dataSource == null)
                return "";

            HttpContext context = helper.ViewContext.HttpContext;

            //Dictionary<object, object> data = MvcControlDataBinder.SourceToDictionary(dataSource, "", "");
            SingleRepeater repeater = new SingleRepeater(context, repeaterMode, htmlAttributes);
            using (repeater)
            {
                StringBuilder body = repeater.body;
                Func<T, int, string> itemResult = itemTempletes.Compile();
                Func<T, string> separatorResult = separatorTemplate.Compile();


                //header
                body.Append(header);

                int tableRow = 0;

                /** Legacy comment translated to English.*/

                int dataCount = dataSource.Count();
                int i = 0;
                foreach (var item in dataSource)
                {

                    /* Legacy comment translated to English. */
                    if (repeaterMode == RepeaterMode.Table)
                    {
                        if (tableRow % colCount == 0)
                            body.Append("<tr>");//Legacy comment translated to English.
                    }

                    body.Append(repeater.itemStartTag);//Legacy comment translated to English.

                    switch (repeaterMode)
                    {
                        case RepeaterMode.None:
                        case RepeaterMode.Div:
                        case RepeaterMode.Ul:
                        case RepeaterMode.Table:
                            body.Append(itemResult(item, i).ToString());//Legacy comment translated to English.
                            break;
                    }

                    body.Append(repeater.itemEndTag);//Legacy comment translated to English.

                    /* Legacy comment translated to English. */
                    if (repeaterMode == RepeaterMode.Table)
                    {
                        tableRow++;
                        if (tableRow % colCount == 0)
                            body.Append("</tr>");//Legacy comment translated to English.
                    }

                    //Legacy comment translated to English.
                    if (i < dataCount - 1)
                    {
                        body.Append(separatorResult(item).ToString());
                    }

                    i++;
                }
                /** Legacy comment translated to English.*/


                //Legacy comment translated to English.
                if (repeaterMode == RepeaterMode.Table && tableRow % colCount != 0)
                {
                    do
                    {
                        body.Append(repeater.itemStartTag).Append(repeater.itemEndTag);//Legacy comment translated to English.
                        tableRow++;
                    } while (tableRow % colCount != 0);
                    body.Append("</tr>");
                }
                //context.Response.Write(body.ToString());

                //footer
                body.Append(footer);

            }
            return repeater.ToString();
        }

        //public static string Repeater<T>(this HtmlHelper helper, IEnumerable<T> dataSource,
        //    string header, Expression<Func<T, string>> itemTempletes, Expression<Func<T, string>> separatorTemplate, string footer,
        //    RepeaterMode repeaterMode, int colCount, object htmlAttributes)
        //{

        //}

        public enum RepeaterMode
        {
            /// <summary>
            ///Legacy comment translated to English.
            /// </summary>
            None = 0,
            Table,
            Div,
            Ul
        }

        //public class MVCRepeater : Repeater
        //{ 

        //    public Repeater Repeater(this HtmlHelper html,object datasource, string header,string footer,string bodyType)
        //    {
        //        //StringBuilder sb = new StringBuilder();


        //        //return sb.ToString();
        //    }
        //}
    }
}
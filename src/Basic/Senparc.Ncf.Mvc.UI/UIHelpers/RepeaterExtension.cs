/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RepeaterExtension.cs
    File description: RepeaterExtension related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.22.9-preview13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace System.Web.Mvc
{
    /// <summary>
    /// Provides simple HTML repeater rendering helpers for table, div, and list layouts.
    /// </summary>
    public static class RepeaterExtension
    {
        /// <summary>
        /// Accumulates the opening tag, item markup, and closing tag for one repeater.
        /// </summary>
        public class SingleRepeater : IDisposable
        {
            protected HttpContext _context;

            //bool wroteStartTag = false, wroteEndTag = false;

            string startTag, endTag;
            /// <summary>Gets or sets the markup placed before each item.</summary>
            public string itemStartTag, itemEndTag;
            /// <summary>Gets the mutable buffer containing the generated markup.</summary>
            public StringBuilder body = new StringBuilder();

            /// <summary>
            /// Initializes a repeater writer and emits its opening markup.
            /// </summary>
            /// <param name="context">The HTTP context used by the repeater.</param>
            /// <param name="repeaterMode">The HTML container mode.</param>
            /// <param name="htmlAttributes">The attributes applied to the container.</param>
            public SingleRepeater(HttpContext context, RepeaterMode repeaterMode, object htmlAttributes)
            {
                this._context = context;
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

            /// <summary>
            /// Appends the configured container opening tag to the repeater body.
            /// </summary>
            public void WriteStartTag()
            {
                body.Append(startTag);
            }

            /// <summary>
            /// Appends the configured container closing tag to the repeater body.
            /// </summary>
            public void WriteEndTag()
            {
                body.Append(endTag);
            }

            /// <summary>
            /// Returns the complete repeater markup accumulated so far, including any
            /// opening and closing tags emitted by the repeater scope.
            /// </summary>
            /// <returns>The generated HTML markup.</returns>
            public override string ToString()
            {
                return body.ToString();
            }

            #region Legacy section

            /// <summary>
            /// Appends the closing tag when the repeater scope is disposed.
            /// </summary>
            public void Dispose()
            {
                WriteEndTag();
            }

            #endregion
        }

        /// <summary>
        /// Renders a sequence using the selected repeater container and item templates.
        /// </summary>
        /// <typeparam name="T">The source item type.</typeparam>
        /// <param name="helper">The MVC HTML helper supplying the current HTTP context.</param>
        /// <param name="dataSource">The items to render; a null source produces an empty string.</param>
        /// <param name="header">Markup emitted before the first item.</param>
        /// <param name="itemTempletes">The item renderer receiving an item and zero-based index.</param>
        /// <param name="separatorTemplate">The separator renderer used between items.</param>
        /// <param name="footer">Markup emitted after the final item.</param>
        /// <param name="repeaterMode">The container and item tag layout.</param>
        /// <param name="colCount">The number of columns per table row in table mode.</param>
        /// <param name="htmlAttributes">Attributes applied to the outer container.</param>
        /// <returns>The generated repeater markup.</returns>
        /// <exception cref="DivideByZeroException">
        /// Thrown when table mode is selected with a zero
        /// <paramref name="colCount"/>.
        /// </exception>
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
            /// Emits no container or item tags.
            /// </summary>
            None = 0,
            /// <summary>
            /// Emits a table body with table-cell item wrappers.
            /// </summary>
            Table,
            /// <summary>
            /// Emits a div container with div item wrappers.
            /// </summary>
            Div,
            /// <summary>
            /// Emits an unordered list with list-item wrappers.
            /// </summary>
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

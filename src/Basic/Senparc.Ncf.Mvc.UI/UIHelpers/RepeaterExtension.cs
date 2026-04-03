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

            string startTag, endTag;//start and end tags
            public string itemStartTag, itemEndTag;//Individual start and end tags
            public StringBuilder body = new StringBuilder();

            public SingleRepeater(HttpContext context, RepeaterMode repeaterMode, object htmlAttributes)
            {
                this._context = context;//It is not used currently. It is required if you use context.Response.Write to output directly here.
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

            #region IDisposable 成员

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
        /// <param name="dataSource">Data source</param>
        /// <param name="header">Content before the start of the first single item loop</param>
        /// <param name="itemTempletes">Single item content</param>
        /// <param name="separatorTemplate">Separate content in each loop (use Table with caution)</param>
        /// <param name="footer">Content after the end of the last single item loop</param>
        /// <param name="repeaterMode">repeater mode</param>
        /// <param name="colCount">Count of each row (only valid when repeaterMode is Table)</param>
        /// <param name="htmlAttributes">Tag attributes</param>
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

                /** Single cycle starts **/

                int dataCount = dataSource.Count();
                int i = 0;
                foreach (var item in dataSource)
                {

                    /* Applies only to Table */
                    if (repeaterMode == RepeaterMode.Table)
                    {
                        if (tableRow % colCount == 0)
                            body.Append("<tr>");//Add line start tag
                    }

                    body.Append(repeater.itemStartTag);//Beginning with a single item

                    switch (repeaterMode)
                    {
                        case RepeaterMode.None:
                        case RepeaterMode.Div:
                        case RepeaterMode.Ul:
                        case RepeaterMode.Table:
                            body.Append(itemResult(item, i).ToString());//Main content
                            break;
                    }

                    body.Append(repeater.itemEndTag);//single ending

                    /* Applies only to Table */
                    if (repeaterMode == RepeaterMode.Table)
                    {
                        tableRow++;
                        if (tableRow % colCount == 0)
                            body.Append("</tr>");//Add end of line tag
                    }

                    //Add separator
                    if (i < dataCount - 1)
                    {
                        body.Append(separatorResult(item).ToString());
                    }

                    i++;
                }
                /** End of single cycle **/


                //Add closing tag to Table
                if (repeaterMode == RepeaterMode.Table && tableRow % colCount != 0)
                {
                    do
                    {
                        body.Append(repeater.itemStartTag).Append(repeater.itemEndTag);//blank cell
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
            /// Does not automatically add any more than tags, equivalent to foreach. But header and footer are still valid
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
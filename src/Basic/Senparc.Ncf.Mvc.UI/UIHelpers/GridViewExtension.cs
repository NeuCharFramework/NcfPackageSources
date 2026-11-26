/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: GridViewExtension.cs
    File description: GridViewExtension related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.22.9-preview13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Html;

namespace System.Web.Mvc
{
    public class GridViewItemTemplateModel<T>
    {
        public string Header { get; set; }
        public Func<T, int, object> ItemTemplate { get; set; }
        public Func<IEnumerable<T>, string> Footer { get; set; }
        public object HtmlAttributes { get; set; }
        //public Action<T> EditTemplate { get; set; }


        /// <summary>
        /// Initializes an empty grid item-template model.
        /// </summary>
        public GridViewItemTemplateModel() { }

        /// <summary>
        /// Initializes a complete grid item-template model.
        /// </summary>
        /// <param name="header">The column header text.</param>
        /// <param name="itemTemplate">The per-item renderer.</param>
        /// <param name="footer">The optional footer renderer.</param>
        /// <param name="htmlAttributes">The HTML attributes for the column.</param>
        public GridViewItemTemplateModel(string header, Func<T, int, object> itemTemplate, Func<IEnumerable<T>, string> footer, object htmlAttributes)
        {
            this.Header = header;
            this.ItemTemplate = itemTemplate;
            this.Footer = footer;
            this.HtmlAttributes = htmlAttributes.ToAttributeList();
        }

        /// <summary>
        /// Initializes a grid item-template model without a footer renderer.
        /// </summary>
        /// <param name="header">The column header text.</param>
        /// <param name="itemTemplate">The per-item renderer.</param>
        /// <param name="htmlAttributes">The HTML attributes for the column.</param>
        public GridViewItemTemplateModel(string header, Func<T, int, object> itemTemplate, object htmlAttributes = null)
            : this(header, itemTemplate, null, htmlAttributes)
        { }

        /// <summary>
        /// Initializes a grid item-template model without a header.
        /// </summary>
        /// <param name="itemTemplate">The per-item renderer.</param>
        /// <param name="htmlAttributes">The HTML attributes for the column.</param>
        public GridViewItemTemplateModel(Func<T, int, object> itemTemplate, object htmlAttributes = null)
            : this(null, itemTemplate, htmlAttributes)
        { }

        //public GridViewItemTemplateModel(Func<T, int, string> itemTemplate, Action<T> editTemplate)
        //{
        //    this.ItemTemplate = itemTemplate;
        //    this.EditTemplate = editTemplate;
        //}
    }

    public static class GridViewExtension
    {
        /// <summary>
        /// Renders a data-bound HTML table from caller-supplied column templates.
        /// </summary>
        /// <typeparam name="T">The row type in <paramref name="dataSource"/>.</typeparam>
        /// <param name="helper">The MVC HTML helper associated with the current view.</param>
        /// <param name="dataSource">The rows to render; a <see langword="null"/> value is treated as empty.</param>
        /// <param name="htmlAttributes">An object whose public properties become table attributes.</param>
        /// <param name="emptyTemplete">The markup rendered when there are no rows.</param>
        /// <param name="htmlCodeFormat">
        /// When <see langword="true"/>, inserts line breaks between generated tags.
        /// </param>
        /// <param name="itemTempletes">
        /// Column definitions that provide headers, row renderers, optional footers,
        /// and cell attributes.
        /// </param>
        /// <returns>An <see cref="HtmlString"/> containing the rendered table or empty-state markup.</returns>
        /// <exception cref="NullReferenceException">
        /// Thrown when a column template, row renderer, or property placeholder refers
        /// to a missing value.
        /// </exception>
        public static HtmlString GridView<T>(this HtmlHelper helper, IEnumerable<T> dataSource,
            object htmlAttributes, string emptyTemplete, bool htmlCodeFormat, params GridViewItemTemplateModel<T>[] itemTempletes) where T : class
        {
            string tableFormat = "<table{0}>{1}</table>";

            string headFormat = "<thead>{0}</thead>";
            string headUnitFormat = "<th>{0}</th>";

            string bodyFormat = "<tbody>{0}</tbody>";

            string rowFormat = "<tr>{0}</tr>";
            string UnitFormat = "<td{0}>{1}</td>";//property，content

            string footFormat = "<tfoot>{0}</tfoot>";

            string emptyFormat = "{0}";

            StringBuilder table = new StringBuilder();//Legacy comment translated to English.

            //thead
            StringBuilder thead = new StringBuilder();//thead
            StringBuilder theadUints = new StringBuilder();//Legacy comment translated to English.

            //body
            StringBuilder tbody = new StringBuilder();//tbody
            StringBuilder tbodyRows = new StringBuilder();//Legacy comment translated to English.

            //tfoot
            StringBuilder tfoot = new StringBuilder();//tfoot
            StringBuilder tfootUints = new StringBuilder();//Legacy comment translated to English.

            StringBuilder empty = new StringBuilder();


            /*******************/
            /** buildTablestart **/
            /*******************/

            //thead
            foreach (var itemTemplete in itemTempletes)
            {
                theadUints.AppendFormat(headUnitFormat, itemTemplete.Header == null ? "" : itemTemplete.Header);
            }
            thead.AppendFormat(headFormat, string.Format(rowFormat, theadUints.ToString()));//Legacy comment translated to English.


            /* tbody start */
            string[] colAttributes = itemTempletes.Select(z => z.HtmlAttributes.ToAttributeList()).ToArray();

            dataSource = dataSource ?? new List<T>();//Legacy comment translated to English.
            int dataSourceCount = dataSource.Count();
            if (dataSourceCount > 0)
            {
                int rowIndex = 0;
                foreach (var data in dataSource)
                {
                    StringBuilder units = new StringBuilder();//Legacy comment translated to English.
                    foreach (var itemTemplete in itemTempletes)
                    {
                        string itemResult = itemTemplete.ItemTemplate(data, rowIndex) == null
                            ? ""
                            : itemTemplete.ItemTemplate(data, rowIndex).ToString();//Legacy comment translated to English.

                        //Legacy comment translated to English.
                        if (itemResult.StartsWith("$"))
                        {
                            //Legacy comment translated to English.
                            string bindDataName = itemResult.Replace("$", "");
                            var pro = data.GetType().GetProperty(bindDataName);
                            itemResult = data.GetType().GetProperty(bindDataName).GetValue(data, null).ToString();
                        }

                        //Legacy comment translated to English.

                        units.AppendFormat(UnitFormat, itemTemplete.HtmlAttributes != null ? itemTemplete.HtmlAttributes.ToString() : "", itemResult);//Legacy comment translated to English.
                    }
                    tbodyRows.AppendFormat(rowFormat, units.ToString());//Legacy comment translated to English.
                    rowIndex++;
                }
                tbody.AppendFormat(bodyFormat, tbodyRows.ToString()); //Legacy comment translated to English.
                /* tbody end */


                //tfoot
                foreach (var itemTemplete in itemTempletes)
                {
                    string result = "";

                    if (itemTemplete.Footer != null)
                        result = itemTemplete.Footer(dataSource);

                    tfootUints.AppendFormat(UnitFormat, "", result);//Legacy comment translated to English.
                }
                tfoot.AppendFormat(footFormat, string.Format(rowFormat, tfootUints.ToString()));//Legacy comment translated to English.
            }
            else
            {
                empty.AppendFormat(emptyFormat, emptyTemplete);//empty data
            }


            //Legacy comment translated to English.
            StringBuilder tableData = new StringBuilder();
            tableData.Append(thead.ToString()).Append(tbody.ToString()).Append(tfoot.ToString());


            //property
            string setHash = htmlAttributes.ToAttributeList();
            string attributeList = string.Empty;
            if (setHash != null)
                attributeList = setHash;

            table.AppendFormat(tableFormat, attributeList, tableData.ToString());//Legacy comment translated to English.
            table.Append(empty.ToString());//empty data


            /*******************/
            /** buildTableend **/
            /*******************/

            string tableHtml = string.Empty;

            if (htmlCodeFormat)
                return new HtmlString(table.Replace("><", ">\r\n<").ToString());
            else
                return new HtmlString(table.ToString());

        }


        /// <summary>
        /// Renders a simple HTML table whose columns are the value-type properties of
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The row type in <paramref name="dataSource"/>.</typeparam>
        /// <param name="helper">The MVC HTML helper associated with the current view.</param>
        /// <param name="dataSource">The rows to render.</param>
        /// <param name="htmlAttributes">An object whose public properties become table attributes.</param>
        /// <returns>The generated table markup as a string.</returns>
        public static string GridView<T>(this HtmlHelper helper, IEnumerable<T> dataSource, object htmlAttributes)
        {
            List<Expression<Func<T, int, string>>> itemTempletes = new List<Expression<Func<T, int, string>>>();
            PropertyInfo[] propertys = typeof(T).GetProperties().Where(z => z.PropertyType.IsValueType).ToArray();//Legacy comment translated to English.
            string[] header = propertys.Select(z => z.Name).ToArray();


            //property
            var setHash = htmlAttributes.ToAttributeList();

            string attributeList = string.Empty;
            if (setHash != null)
                attributeList = setHash;

            StringBuilder table = new StringBuilder();
            table.AppendFormat("<table {0}><thead><tr>", attributeList);
            foreach (var item in header)
            {
                table.AppendFormat("<th>{0}</th>", item);
            }
            table.Append("</tr></thead><tbody>");

            foreach (var item in dataSource)
            {
                var members = item.GetType().GetMembers();
                table.Append("<tr>");
                foreach (var pro in propertys)
                {
                    table.AppendFormat("<td>{0}</td>", pro.GetValue(item, null));
                }
                table.Append("</tr>");
            }
            table.Append("</tbody></table>");


            return table.ToString();
            //return GridView<T>(helper, dataSource, header, null, htmlAttributes, itemTempletes.ToArray());
        }

        //public static string GridView<T>(this HtmlHelper helper, IEnumerable<T> dataSource,
        //   string[] header, string[] footer,
        //   object htmlAttributes, params Expression<Func<T, string>>[] itemTempletes)
        //{
        //    List<Expression<Func<T, int, string>>> items = new List<Expression<Func<T, int, string>>>();

        //    //foreach (var item in itemTempletes)
        //    //{
        //    //    items.Add();
        //    //}

        //    //return GridView<T>(helper, dataSource, header, footer, htmlAttributes, itemTempletes.Select(
        //    //    z => z.Compile().);

        //    return GridView<T>(helper, dataSource, header, footer, htmlAttributes, items.ToArray());

        //}
    }
}

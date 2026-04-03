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


        public GridViewItemTemplateModel() { }

        public GridViewItemTemplateModel(string header, Func<T, int, object> itemTemplate, Func<IEnumerable<T>, string> footer, object htmlAttributes)
        {
            this.Header = header;
            this.ItemTemplate = itemTemplate;
            this.Footer = footer;
            this.HtmlAttributes = htmlAttributes.ToAttributeList();
        }

        public GridViewItemTemplateModel(string header, Func<T, int, object> itemTemplate, object htmlAttributes = null)
            : this(header, itemTemplate, null, htmlAttributes)
        { }

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
        /// GridView
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="helper"></param>
        /// <param name="dataSource">Data source</param>
        /// <param name="htmlAttributes">Table attributes</param>
        /// <param name="emptyTemplete">Displayed when there is no data. When there is no data, Footer does not display</param>
        /// <param name="htmlCodeFormat">HTML code line wrapping (for use in debugging state). If false, automatically generated code will not wrap.</param>
        /// <param name="itemTempletes">Template data</param>
        /// <returns></returns>
        public static HtmlString GridView<T>(this HtmlHelper helper, IEnumerable<T> dataSource,
            object htmlAttributes, string emptyTemplete, bool htmlCodeFormat, params GridViewItemTemplateModel<T>[] itemTempletes) where T : class
        {
            string tableFormat = "<table{0}>{1}</table>";

            string headFormat = "<thead>{0}</thead>";
            string headUnitFormat = "<th>{0}</th>";

            string bodyFormat = "<tbody>{0}</tbody>";

            string rowFormat = "<tr>{0}</tr>";
            string UnitFormat = "<td{0}>{1}</td>";//properties, content

            string footFormat = "<tfoot>{0}</tfoot>";

            string emptyFormat = "{0}";

            StringBuilder table = new StringBuilder();//Entire Table

            //thead
            StringBuilder thead = new StringBuilder();//thead
            StringBuilder theadUints = new StringBuilder();//cells within thead

            //body
            StringBuilder tbody = new StringBuilder();//tbody
            StringBuilder tbodyRows = new StringBuilder();//OK

            //tfoot
            StringBuilder tfoot = new StringBuilder();//tfoot
            StringBuilder tfootUints = new StringBuilder();//cells within tfoot

            StringBuilder empty = new StringBuilder();


            /*******************/
            /** Start constructing Table **/
            /*******************/

            //thead
            foreach (var itemTemplete in itemTempletes)
            {
                theadUints.AppendFormat(headUnitFormat, itemTemplete.Header == null ? "" : itemTemplete.Header);
            }
            thead.AppendFormat(headFormat, string.Format(rowFormat, theadUints.ToString()));//head line


            /* tbody start */
            string[] colAttributes = itemTempletes.Select(z => z.HtmlAttributes.ToAttributeList()).ToArray();

            dataSource = dataSource ?? new List<T>();//If the data is empty, create an empty record for getting Count
            int dataSourceCount = dataSource.Count();
            if (dataSourceCount > 0)
            {
                int rowIndex = 0;
                foreach (var data in dataSource)
                {
                    StringBuilder units = new StringBuilder();//cells within a row of tbody
                    foreach (var itemTemplete in itemTempletes)
                    {
                        string itemResult = itemTemplete.ItemTemplate(data, rowIndex) == null
                            ? ""
                            : itemTemplete.ItemTemplate(data, rowIndex).ToString();//Template data of the Bank

                        //Determine whether the body cell is automatically bound
                        if (itemResult.StartsWith("$"))
                        {
                            //Automatic binding
                            string bindDataName = itemResult.Replace("$", "");
                            var pro = data.GetType().GetProperty(bindDataName);
                            itemResult = data.GetType().GetProperty(bindDataName).GetValue(data, null).ToString();
                        }

                        //Cell properties

                        units.AppendFormat(UnitFormat, itemTemplete.HtmlAttributes != null ? itemTemplete.HtmlAttributes.ToString() : "", itemResult);//cell
                    }
                    tbodyRows.AppendFormat(rowFormat, units.ToString());//OK
                    rowIndex++;
                }
                tbody.AppendFormat(bodyFormat, tbodyRows.ToString()); //the whole tbody
                /* tbody end */


                //tfoot
                foreach (var itemTemplete in itemTempletes)
                {
                    string result = "";

                    if (itemTemplete.Footer != null)
                        result = itemTemplete.Footer(dataSource);

                    tfootUints.AppendFormat(UnitFormat, "", result);//There is no <fr> in footer?
                }
                tfoot.AppendFormat(footFormat, string.Format(rowFormat, tfootUints.ToString()));//foot row
            }
            else
            {
                empty.AppendFormat(emptyFormat, emptyTemplete);//empty data
            }


            //Integrate head, body, foot
            StringBuilder tableData = new StringBuilder();
            tableData.Append(thead.ToString()).Append(tbody.ToString()).Append(tfoot.ToString());


            //property
            string setHash = htmlAttributes.ToAttributeList();
            string attributeList = string.Empty;
            if (setHash != null)
                attributeList = setHash;

            table.AppendFormat(tableFormat, attributeList, tableData.ToString());//Integrate the entire Table, including attributes
            table.Append(empty.ToString());//empty data


            /*******************/
            /** End of constructing Table **/
            /*******************/

            string tableHtml = string.Empty;

            if (htmlCodeFormat)
                return new HtmlString(table.Replace("><", ">\r\n<").ToString());
            else
                return new HtmlString(table.ToString());

        }


        /// <summary>
        /// Automatically bind GridView
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="helper"></param>
        /// <param name="dataSource"></param>
        /// <param name="htmlAttributes"></param>
        /// <returns></returns>
        public static string GridView<T>(this HtmlHelper helper, IEnumerable<T> dataSource, object htmlAttributes)
        {
            List<Expression<Func<T, int, string>>> itemTempletes = new List<Expression<Func<T, int, string>>>();
            PropertyInfo[] propertys = typeof(T).GetProperties().Where(z => z.PropertyType.IsValueType).ToArray();//Filter to value type
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

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace System.Web.Mvc
{
    public class GridViewActionItemTemplateModel<T>
    {
        public string Header { get; set; }
        public Action<T, int> ItemTemplate { get; set; }
        public Action<T> Footer { get; set; }
        public object HtmlAttributes { get; set; }
        //public Action<T> EditTemplate { get; set; }


        public GridViewActionItemTemplateModel() { }

        public GridViewActionItemTemplateModel(string header, Action<T, int> itemTemplate, Action<T> footer, object htmlAttributes)
        {
            this.Header = header;
            this.ItemTemplate = itemTemplate;
            this.Footer = footer;
            this.HtmlAttributes = htmlAttributes.ToAttributeList();
        }

        public GridViewActionItemTemplateModel(string header, Action<T, int> itemTemplate, object htmlAttributes)
            : this(header, itemTemplate, null, htmlAttributes)
        { }

        public GridViewActionItemTemplateModel(Action<T, int> itemTemplate)
            : this(null, itemTemplate, null)
        { }

        //public GridViewItemTemplateModel(Func<T, int, string> itemTemplate, Action<T> editTemplate)
        //{
        //    this.ItemTemplate = itemTemplate;
        //    this.EditTemplate = editTemplate;
        //}
    }

    public static class GridViewActionExtension
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
        public static string GridViewAction<T>(this HtmlHelper helper, IEnumerable<T> dataSource,
            object htmlAttributes, string emptyTemplete, bool htmlCodeFormat, params GridViewActionItemTemplateModel<T>[] itemTempletes)
        {
            foreach (var item in dataSource)
            {
                foreach (var temp in itemTempletes)
                {
                    temp.ItemTemplate(item, 0);
                }
            }
            return "ddd";
        }
    }
}

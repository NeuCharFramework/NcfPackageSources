/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: GridViewActionExtension.cs
    File description: GridViewActionExtension related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.22.9-preview13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

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
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
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

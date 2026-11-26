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
    /// <summary>
    /// Describes action callbacks associated with one generated grid column.
    /// </summary>
    public class GridViewActionItemTemplateModel<T>
    {
        /// <summary>Gets or sets the column header text.</summary>
        public string Header { get; set; }
        /// <summary>Gets or sets the callback invoked for each row.</summary>
        public Action<T, int> ItemTemplate { get; set; }
        /// <summary>Gets or sets the optional footer callback.</summary>
        public Action<T> Footer { get; set; }
        /// <summary>Gets or sets the attributes associated with the column.</summary>
        public object HtmlAttributes { get; set; }
        //public Action<T> EditTemplate { get; set; }


        /// <summary>
        /// Initializes an empty action-template model.
        /// </summary>
        public GridViewActionItemTemplateModel() { }

        /// <summary>
        /// Initializes a complete action-template model.
        /// </summary>
        /// <param name="header">The column header text.</param>
        /// <param name="itemTemplate">The per-item action callback.</param>
        /// <param name="footer">The optional footer callback.</param>
        /// <param name="htmlAttributes">The HTML attributes for the column.</param>
        public GridViewActionItemTemplateModel(string header, Action<T, int> itemTemplate, Action<T> footer, object htmlAttributes)
        {
            this.Header = header;
            this.ItemTemplate = itemTemplate;
            this.Footer = footer;
            this.HtmlAttributes = htmlAttributes.ToAttributeList();
        }

        /// <summary>
        /// Initializes an action-template model without a footer callback.
        /// </summary>
        /// <param name="header">The column header text.</param>
        /// <param name="itemTemplate">The per-item action callback.</param>
        /// <param name="htmlAttributes">The HTML attributes for the column.</param>
        public GridViewActionItemTemplateModel(string header, Action<T, int> itemTemplate, object htmlAttributes)
            : this(header, itemTemplate, null, htmlAttributes)
        { }

        /// <summary>
        /// Initializes an action-template model without a header or footer.
        /// </summary>
        /// <param name="itemTemplate">The per-item action callback.</param>
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
        /// Invokes the configured action callbacks for each source row and column.
        /// </summary>
        /// <typeparam name="T">The source row type.</typeparam>
        /// <param name="helper">The MVC HTML helper associated with the current view.</param>
        /// <param name="dataSource">The rows whose callbacks should be invoked.</param>
        /// <param name="htmlAttributes">Reserved table-attribute input for future rendering.</param>
        /// <param name="emptyTemplete">Reserved empty-state markup input.</param>
        /// <param name="htmlCodeFormat">Reserved formatting flag.</param>
        /// <param name="itemTempletes">The action column definitions.</param>
        /// <returns>
        /// The current implementation returns the fixed marker <c>ddd</c> after invoking
        /// the row callbacks; it does not render a complete grid.
        /// </returns>
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.XncfBase.Functions
{
    /// <summary>
    ///option list
    /// </summary>
    public class SelectionList
    {
        private string[] selectedValues;

        /// <summary>
        /// option type
        /// </summary>
        public SelectionType SelectionType { get; set; }
        ///// <summary>
        ///// The value of the selected item (passed in from the client)
        ///// </summary>
        public string[] SelectedValues { get => selectedValues ?? new string[0]; set => selectedValues = value; }

        /// <summary>
        /// option parameters
        /// </summary>
        public IList<SelectionItem> Items { get; set; }

        //public SelectionList() { }


        public SelectionList(SelectionType selectionType) : this(selectionType, null)
        {
        }

        public SelectionList(SelectionType selectionType, IList<SelectionItem> items)
        {
            SelectionType = selectionType;
            Items = items ?? new List<SelectionItem>();
        }

        /// <summary>
        /// Determine whether there is a value in SelectedValues
        /// </summary>
        /// <param name="itemIndex">Get the Value corresponding to the index item in Items</param>
        /// <returns></returns>
        public bool IsSelected(int itemIndex)
        {
            if (itemIndex > Items.Count - 1)
            {
                throw new IndexOutOfRangeException($"{nameof(itemIndex)}（{itemIndex}） 超出 {nameof(Items)} 索引值范围（{Items.Count}）！");
            }

            return IsSelected(Items[itemIndex].Value);
        }

        /// <summary>
        /// Determine whether there is a value in SelectedValues
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsSelected(string value)
        {
            return SelectedValues.Contains(value);
        }
    }

    /// <summary>
    ///options
    /// </summary>
    public class SelectionItem
    {
        /// <summary>
        /// text label
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// value
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// (for display only), whether selected by default
        /// </summary>
        public bool DefaultSelected { get; set; }
        /// <summary>
        /// illustrate
        /// </summary>
        public string Note { get; set; }

        public object BindData  { get; set; }

        public SelectionItem() { }

        public SelectionItem(string value, string text, string note = "", bool defaultSelected = false)
        {
            Text = text;
            Value = value;
            Note = note;
            DefaultSelected = defaultSelected;
        }
    }

    /// <summary>
    /// option collection type
    /// </summary>
    public enum SelectionType
    {
        /// <summary>
        /// unknown parameters
        /// </summary>
        Unknown,
        /// <summary>
        /// drop-down list (single selection)
        /// </summary>
        DropDownList,
        /// <summary>
        /// Checkbox list (multiple selection)
        /// </summary>
        CheckBoxList,
        ///// <summary>
        ///// Single choice list (single choice)
        ///// </summary>
        //RadioButtonList
    }
}

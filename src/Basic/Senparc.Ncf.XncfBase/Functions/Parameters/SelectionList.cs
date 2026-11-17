/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: SelectionList.cs

    File description: SelectionList related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.XncfBase.Functions
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SelectionList
    {
        private string[] selectedValues;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public SelectionType SelectionType { get; set; }
        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        public string[] SelectedValues { get => selectedValues ?? new string[0]; set => selectedValues = value; }

        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsSelected(string value)
        {
            return SelectedValues.Contains(value);
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SelectionItem
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool DefaultSelected { get; set; }
        /// <summary>
        /// description
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
    ///Legacy comment translated to English.
    /// </summary>
    public enum SelectionType
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Unknown,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        DropDownList,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        CheckBoxList,
        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        //RadioButtonList
    }
}

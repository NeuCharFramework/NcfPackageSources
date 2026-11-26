/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SelectionList.cs
    File description: SelectionList related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.XncfBase.Functions
{
    /// <summary>
    /// Describes a rendered selection control and its available/selected values.
    /// </summary>
    public class SelectionList
    {
        private string[] selectedValues;

        /// <summary>Gets or sets the rendered selection control type.</summary>
        public SelectionType SelectionType { get; set; }
        /// <summary>Gets or sets the currently selected values.</summary>
        public string[] SelectedValues { get => selectedValues ?? new string[0]; set => selectedValues = value; }

        /// <summary>Gets or sets the available selection items.</summary>
        public IList<SelectionItem> Items { get; set; }

        //public SelectionList() { }


        /// <summary>
        /// Initializes an empty selection list of the requested control type.
        /// </summary>
        /// <param name="selectionType">The UI selection control type.</param>
        public SelectionList(SelectionType selectionType) : this(selectionType, null)
        {
        }

        /// <summary>
        /// Initializes a selection list with optional items.
        /// </summary>
        /// <param name="selectionType">The UI selection control type.</param>
        /// <param name="items">The available selection items.</param>
        public SelectionList(SelectionType selectionType, IList<SelectionItem> items)
        {
            SelectionType = selectionType;
            Items = items ?? new List<SelectionItem>();
        }

        /// <summary>
        /// Determines whether the item at an index is selected.
        /// </summary>
        /// <param name="itemIndex">The item index.</param>
        /// <returns><see langword="true"/> when the item's value is selected.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when the index exceeds the item collection.</exception>
        public bool IsSelected(int itemIndex)
        {
            if (itemIndex > Items.Count - 1)
            {
                throw new IndexOutOfRangeException($"{nameof(itemIndex)}（{itemIndex}） 超出 {nameof(Items)} 索引值范围（{Items.Count}）！");
            }

            return IsSelected(Items[itemIndex].Value);
        }

        /// <summary>
        /// Determines whether a value appears in the selected-value collection.
        /// </summary>
        /// <param name="value">The selection value to check.</param>
        /// <returns><see langword="true"/> when the value is selected.</returns>
        public bool IsSelected(string value)
        {
            return SelectedValues.Contains(value);
        }
    }

    /// <summary>
    /// Represents one selectable value in a Function parameter control.
    /// </summary>
    public class SelectionItem
    {
        /// <summary>Gets or sets the display text.</summary>
        public string Text { get; set; }
        /// <summary>Gets or sets the submitted value.</summary>
        public string Value { get; set; }
        /// <summary>Gets or sets whether the item is selected by default.</summary>
        public bool DefaultSelected { get; set; }
        /// <summary>
        /// description
        /// </summary>
        public string Note { get; set; }

        /// <summary>Gets or sets provider-specific data attached to the item.</summary>
        public object BindData  { get; set; }

        /// <summary>Initializes an empty selection item.</summary>
        public SelectionItem() { }

        /// <summary>
        /// Initializes a selection item with display text and optional default-selection state.
        /// </summary>
        /// <param name="value">The submitted value.</param>
        /// <param name="text">The display text.</param>
        /// <param name="note">Optional helper text.</param>
        /// <param name="defaultSelected">Whether the item is selected by default.</param>
        public SelectionItem(string value, string text, string note = "", bool defaultSelected = false)
        {
            Text = text;
            Value = value;
            Note = note;
            DefaultSelected = defaultSelected;
        }
    }

    /// <summary>
    /// Identifies the supported selection control types.
    /// </summary>
    public enum SelectionType
    {
        /// <summary>No specific selection control has been assigned.</summary>
        Unknown,
        /// <summary>A single-value dropdown control.</summary>
        DropDownList,
        /// <summary>A multi-value checkbox list.</summary>
        CheckBoxList,
        // RadioButtonList was removed from the supported selection controls.
        //RadioButtonList
    }
}

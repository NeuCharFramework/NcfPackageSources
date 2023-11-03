using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.XncfBase.Functions
{
    /// <summary>
    /// 选项列表
    /// </summary>
    public class SelectionList
    {
        private string[] selectedValues;

        /// <summary>
        /// 选项类型
        /// </summary>
        public SelectionType SelectionType { get; set; }
        ///// <summary>
        ///// 选中的项的值（从客户端传入）
        ///// </summary>
        public string[] SelectedValues { get => selectedValues ?? new string[0]; set => selectedValues = value; }

        /// <summary>
        /// 选项参数
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
        /// 判断 SelectedValues 中是否存在值
        /// </summary>
        /// <param name="itemIndex">获取 Items 中的索引项对应的 Value</param>
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
        /// 判断 SelectedValues 中是否存在值
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsSelected(string value)
        {
            return SelectedValues.Contains(value);
        }
    }

    /// <summary>
    /// 选项
    /// </summary>
    public class SelectionItem
    {
        /// <summary>
        /// 文字标签
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// 值
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// （仅供显示时使用），是否默认选中
        /// </summary>
        public bool DefaultSelected { get; set; }
        /// <summary>
        /// 说明
        /// </summary>
        public string Note { get; set; }

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
    /// 选项集合类型
    /// </summary>
    public enum SelectionType
    {
        /// <summary>
        /// 未知参数
        /// </summary>
        Unknown,
        /// <summary>
        /// 下拉列表（单选）
        /// </summary>
        DropDownList,
        /// <summary>
        /// 复选框列表（多选）
        /// </summary>
        CheckBoxList,
        ///// <summary>
        ///// 单选列表（单选）
        ///// </summary>
        //RadioButtonList
    }
}

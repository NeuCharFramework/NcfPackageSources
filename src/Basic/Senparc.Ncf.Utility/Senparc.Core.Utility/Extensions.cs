/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Extensions.cs
    File description: Extensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Senparc.Ncf.Core.Utility
{
    /// <summary>
    /// Provides small paging, enum, and price-formatting helpers used by NCF applications.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Calculates the number of records to skip for a one-based page index.
        /// </summary>
        /// <param name="pageIndex">The one-based page number.</param>
        /// <param name="pageCount">The number of records per page.</param>
        /// <returns>The number of records preceding the requested page.</returns>
        public static int GetSkipRecord(int pageIndex, int pageCount)
        {
            return (pageIndex - 1) * pageCount;
        }

        /// <summary>
        /// Converts an object array into a zero-based dictionary keyed by source order.
        /// </summary>
        /// <param name="strArr">The values to convert to strings.</param>
        /// <returns>A dictionary whose keys are array indexes and whose values are item strings.</returns>
        public static Dictionary<int, string> GetDictionaryFromStringArray(this object[] strArr)
        {
            Dictionary<int, string> dic = new Dictionary<int, string>();
            int i = 0;
            foreach (var item in strArr)
            {
                dic.Add(i, item.ToString());
                i++;
            }
            return dic;
        }

        /// <summary>
        /// Builds a string dictionary for an enum's numeric values, optionally using
        /// <see cref="DescriptionAttribute"/> values and a blank option.
        /// </summary>
        /// <param name="enumType">The enum type to enumerate.</param>
        /// <param name="useDescription">Whether field descriptions should replace enum names.</param>
        /// <param name="addBlankOption">Whether to add an empty-key entry.</param>
        /// <param name="blankOptionText">The display text for the optional blank entry.</param>
        /// <returns>A dictionary mapping numeric enum values to display text.</returns>
        /// <exception cref="Exception">Thrown when <paramref name="enumType"/> is not an enum type.</exception>
        public static Dictionary<string, string> GetDictionaryForEnums(this Type enumType, bool useDescription = false, bool addBlankOption = false, string blankOptionText = null)
        {
            if (!enumType.IsEnum)
            {
                throw new Exception("此对象不是Enum类型！");
            }
            Dictionary<string, string> dic = new Dictionary<string, string>();
            if (addBlankOption)
            {
                dic.Add("", blankOptionText ?? "");
            }
            foreach (int item in Enum.GetValues(enumType))
            {
                string name = Enum.GetName(enumType, item);
                if (useDescription)
                {
                    FieldInfo fi = enumType.GetField(Enum.GetName(enumType, item));
                    var dna = (DescriptionAttribute)Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));
                    if (dna != null)
                    {
                        name = dna.Description;
                    }
                }

                name = name ?? Enum.GetName(enumType, item);
                dic.Add(item.ToString(), name);
            }
            return dic;
        }

        /// <summary>
        /// Gets the description attribute text for a numeric enum value.
        /// </summary>
        /// <param name="enumType">The enum type to inspect.</param>
        /// <param name="item">The numeric enum value.</param>
        /// <returns>The description text, or <see langword="null"/> when no description exists.</returns>
        /// <exception cref="Exception">Thrown when <paramref name="enumType"/> is not an enum type.</exception>
        public static string GetDescriptionForEnum(this Type enumType, int item)
        {
            if (!enumType.IsEnum)
            {
                throw new Exception("此对象不是Enum类型！");
            }
            string name = null;
            FieldInfo fi = enumType.GetField(Enum.GetName(enumType, item));
            var dna = (DescriptionAttribute)Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));
            if (dna != null)
            {
                name = dna.Description;
            }
            return name;
        }

        /// <summary>
        /// Formats a task price as a currency string, or returns the developer-bidding label for zero.
        /// </summary>
        /// <param name="price">The task price.</param>
        /// <returns>The localized currency representation or the zero-price label.</returns>
        public static string GetTaskPrice(this decimal price)
        {
            return price == 0 ? "开发者竞价" : price.ToString("C");
        }
    }
}

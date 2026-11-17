/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Extensions.cs
    File description: Extensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Senparc.Ncf.Core.Utility
{
    public static class Extensions
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static int GetSkipRecord(int pageIndex, int pageCount)
        {
            return (pageIndex - 1) * pageCount;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="strArr"></param>
        /// <returns></returns>
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
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="enumType"></param>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static Dictionary<string, string> GetDictionaryForEnums(this Type enumType, bool useDescription = false, bool addBlankOption = false, string blankOptionText = null)
        {
            if (!enumType.IsEnum)
            {
                throw new Exception("此对象不是Enum类型！");
            }
            Dictionary<string, string> dic = new Dictionary<string, string>();
            if (addBlankOption)
            {
                dic.Add("", blankOptionText ?? "");//Legacy comment translated to English.
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

        public static string GetTaskPrice(this decimal price)
        {
            return price == 0 ? "开发者竞价" : price.ToString("C");
        }
    }
}
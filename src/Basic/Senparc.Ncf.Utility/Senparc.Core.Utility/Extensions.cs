using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Senparc.Ncf.Core.Utility
{
    public static class Extensions
    {
        /// <summary>
        /// Get the number of records skipped when turning pages
        /// </summary>
        /// <param name="pageIndex">Current page number</param>
        /// <param name="pageCount">Number of records per page</param>
        /// <returns></returns>
        public static int GetSkipRecord(int pageIndex, int pageCount)
        {
            return (pageIndex - 1) * pageCount;
        }

        /// <summary>
        /// Get the dictionary type of the array (key is index, value is array content). Usually used with enumeration types
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
        /// Get the enumeration members and convert them to Dictionary type
        /// </summary>
        /// <param name="enumType"></param>
        /// <param name="useDescription">Whether to use the description of the enumeration type</param>
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
                dic.Add("", blankOptionText ?? "");//Add blank items
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
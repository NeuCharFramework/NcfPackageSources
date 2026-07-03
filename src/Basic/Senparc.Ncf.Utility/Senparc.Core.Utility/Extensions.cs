/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Extensions.cs
    文件功能描述：Extensions 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
        /// 获取翻页时跳过的记录数
        /// </summary>
        /// <param name="pageIndex">当前页码</param>
        /// <param name="pageCount">每页记录数</param>
        /// <returns></returns>
        public static int GetSkipRecord(int pageIndex, int pageCount)
        {
            return (pageIndex - 1) * pageCount;
        }

        /// <summary>
        /// 获取数组的字典类型（key为index，value为数组内容）。通常用于配合枚举类型
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
        /// 获取枚举成员，转为Dictionary类型
        /// </summary>
        /// <param name="enumType"></param>
        /// <param name="useDescription">是否使用枚举类型的描述</param>
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
                dic.Add("", blankOptionText ?? "");//添加空白项
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
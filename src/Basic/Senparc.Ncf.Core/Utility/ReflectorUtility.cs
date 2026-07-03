/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ReflectorUtility.cs
    文件功能描述：ReflectorUtility 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Utility
{
    public static class ReflectorUtility
    {
        public static void SetPropertyValue(object obj, Type objType, string propertyName, string value)
        {
            var prop = objType.GetProperty(propertyName);
            switch (prop.PropertyType.Name)
            {
                case "DateTime":
                    prop.SetValue(obj, DateTime.Parse(value), null);
                    break;
                case "DateTimeOffset":
                    prop.SetValue(obj, DateTimeOffset.Parse(value), null);
                    break;
                case "Int32":
                    prop.SetValue(obj, int.Parse(value), null);
                    break;
                case "Int64":
                    prop.SetValue(obj, long.Parse(value), null);
                    break;
                case "Single":
                case "float":
                    prop.SetValue(obj, float.Parse(value), null);
                    break;
                case "Double":
                    prop.SetValue(obj, double.Parse(value), null);
                    break;
                case "Boolean":
                    prop.SetValue(obj, bool.Parse(value), null);
                    break;
                default:
                    prop.SetValue(obj, value, null);
                    break;
            }
        }
    }
}
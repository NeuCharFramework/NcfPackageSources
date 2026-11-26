/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ReflectorUtility.cs
    File description: ReflectorUtility related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Utility
{
    public static class ReflectorUtility
    {
        /// <summary>
        /// Converts a textual value to the target property's supported primitive type and assigns it
        /// through reflection. Unsupported property types receive the original string value.
        /// </summary>
        /// <param name="obj">The object whose property will be changed.</param>
        /// <param name="objType">The runtime type containing <paramref name="propertyName"/>.</param>
        /// <param name="propertyName">The writable property name.</param>
        /// <param name="value">The textual value to parse or assign.</param>
        /// <exception cref="NullReferenceException">Thrown when the named property does not exist.</exception>
        /// <exception cref="FormatException">Thrown when the value cannot be parsed as the property's supported primitive type.</exception>
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

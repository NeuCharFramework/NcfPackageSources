/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: HtmlExtensionUtility.cs
    File description: HtmlExtensionUtility related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.22.9-preview13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using Senparc.Ncf.Core.Extensions;
using Microsoft.AspNetCore.Routing;
using Senparc.CO2NET.Extensions;

namespace System.Web.Mvc
{
    public static class HtmlExtensionUtility
    {

        #region Utility

        /// <summary>
        /// Compares two values after converting the second value to the first value's type.
        /// </summary>
        /// <param name="o1">The first value.</param>
        /// <param name="o2">The second value.</param>
        /// <returns><see langword="true"/> when both values are null or equal after conversion.</returns>
        internal static bool AreEqual(object o1, object o2)
        {
            bool result = false;

            if (o1 == null && o2 == null)
            {
                result = true;
            }
            else
            {
                if (o1 != null)
                {
                    Type t = o1.GetType();
                    try
                    {
                        o2 = System.Convert.ChangeType(o2, t);
                        result = o1.Equals(o2);
                    }
                    catch
                    {
                        return false;
                    }
                }

            }

            return result;
        }

        /// <summary>
        /// Reads a hash setting as text, returning an empty string when it is absent.
        /// </summary>
        /// <param name="hash">The settings hash table.</param>
        /// <param name="key">The setting key.</param>
        /// <returns>The setting text or an empty string.</returns>
        internal static string EvalHashSetting(Hashtable hash, string key)
        {
            string result = string.Empty;
            if (hash.Keys.Count > 0)
            {
                if (hash.ContainsKey(key))
                {
                    return hash[key].ToString();
                }
            }
            return result;
        }


        //public static string RenderPage(System.Web.UI.Page pg)
        //{
        //    //render
        //    StringBuilder sb = new StringBuilder();
        //    StringWriter writer = new StringWriter(sb);

        //    try
        //    {
        //        HttpContext.Current.Server.Execute(pg, writer, true);
        //    }
        //    catch (Exception x)
        //    {
        //        if (x.InnerException != null)
        //            throw x.InnerException;
        //        else
        //            throw x;
        //    }
        //    string result = sb.ToString();
        //    return result;

        //}


        /// <summary>
        /// Creates a formatted string by applying <paramref name="format"/> to
        /// every item in an untyped sequence.
        /// </summary>
        /// <param name="list">The item sequence to enumerate.</param>
        /// <param name="format">A format string containing one placeholder for each item.</param>
        /// <returns>The concatenated formatted item strings.</returns>
        public static string ToFormattedList(this IEnumerable list, string format)
        {
            StringBuilder sb = new StringBuilder();
            IEnumerator en = list.GetEnumerator();

            while (en.MoveNext())
            {
                sb.AppendFormat(format, en.Current.ToString());
            }
            return sb.ToString();

        }

        /// <summary>
        /// Finds a property descriptor by case-insensitive name.
        /// </summary>
        /// <param name="propName">The property name to locate.</param>
        /// <param name="props">The property descriptors to inspect.</param>
        /// <returns>The first matching property, or <see langword="null"/>.</returns>
        internal static PropertyInfo FindProp(string propName, PropertyInfo[] props)
        {
            PropertyInfo result = null;
            foreach (PropertyInfo p in props)
            {
                if (p.Name.ToLower().Trim().Equals(propName.ToLower().Trim()))
                {
                    result = p;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Assigns matching public instance properties from a settings table.
        /// </summary>
        /// <param name="instance">The object whose writable properties are updated.</param>
        /// <param name="settings">The case-insensitive property-value map.</param>
        /// <exception cref="ArgumentException">
        /// Propagated by reflection when a supplied value cannot be assigned to a
        /// matching property.
        /// </exception>
        internal static void SetPropsFromHash(object instance, Hashtable settings)
        {
            PropertyInfo pInfo = null;
            foreach (string key in settings.Keys)
            {

                //find a matching property
                pInfo = instance.GetType().GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                if (pInfo != null)
                {
                    pInfo.SetValue(instance, settings[key], null);
                }
            }
        }

        ///// <summary>
        ///// This is for utility only - used in the InstanceUserControl method
        ///// </summary>
        //internal class CustomPage : ViewPage
        //{
        //}

        /// <summary>
        /// Creates an HTML attribute string from the public properties of an object.
        /// </summary>
        /// <param name="o">The object whose public properties become attributes.</param>
        /// <returns>A space-prefixed sequence of <c>name="value"</c> attributes.</returns>
        public static string ToAttributeList(this object o)
        {
            StringBuilder sb = new StringBuilder();
            if (o != null)
            {
                Hashtable attributeHash = GetPropertyHash(o);

                string resultFormat = " {0}=\"{1}\"";
                foreach (string attribute in attributeHash.Keys)
                {
                    sb.AppendFormat(resultFormat, attribute.Replace("_", ""), attributeHash[attribute]);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Creates JavaScript-style assignments from the public properties of an object.
        /// </summary>
        /// <param name="o">The object whose public properties become assignments.</param>
        /// <returns>A semicolon-separated assignment string.</returns>
        public static string ToJsParams(this object o)
        {
            StringBuilder sb = new StringBuilder();
            if (o != null)
            {
                Hashtable attributeHash = GetPropertyHash(o);

                string stringFormat = "{0}=\'{1}\';";
                string numberFormat = "{0}={1};";
                string jsonFormat = "{0}={1};";

                foreach (string attribute in attributeHash.Keys)
                {
                    string formatStr = null;
                    string str = null;
                    var value = attributeHash[attribute];
                    var key = attribute.Replace("_", "");
                    if (value == null)
                    {
                        formatStr = jsonFormat;
                    }
                    else
                    {
                        var type = value.GetType();
                        if (type == typeof(Int16) ||
                            type == typeof(Int32) ||
                            type == typeof(Int64) ||
                            type == typeof(decimal)
                            )
                        {
                            formatStr = numberFormat;
                        }
                        else if (type == typeof(string[]))
                        {
                            formatStr = jsonFormat;
                            value = value.ToJson().Replace("\"", "'");
                        }
                        else if (type == typeof(bool))
                        {
                            str = $"{key}={((bool)value == true ? "true" : "false")};";
                        }
                        else
                        {
                            formatStr = stringFormat;
                        }
                    }

                    if (!formatStr.IsNullOrEmpty())
                    {
                        sb.AppendFormat(formatStr, key, value == null ? "null" : value.ToString());
                    }
                    else
                    {
                        sb.Append(str);
                    }
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Creates an HTML attribute string while excluding selected property names.
        /// </summary>
        /// <param name="o">The object whose public properties become attributes.</param>
        /// <param name="ignoreList">Property names that must not be emitted.</param>
        /// <returns>A space-separated attribute string.</returns>
        public static string ToAttributeList(this object o, params object[] ignoreList)
        {
            Hashtable attributeHash = GetPropertyHash(o);

            string resultFormat = "{0}=\"{1}\" ";
            StringBuilder sb = new StringBuilder();
            foreach (string attribute in attributeHash.Keys)
            {
                if (!ignoreList.Contains(attribute))
                {
                    sb.AppendFormat(resultFormat, attribute, attributeHash[attribute]);
                }
            }
            return sb.ToString();
        }


        /// <summary>
        /// Converts an anonymous object or route-value dictionary into an HTML attribute string.
        /// </summary>
        /// <param name="value">The object or dictionary containing attribute values.</param>
        /// <returns>The normalized attribute string, or an empty string for <see langword="null"/>.</returns>
        public static string ConvertObjectToAttributeList(object value)
        {
            StringBuilder builder = new StringBuilder();
            if (value != null)
            {
                IDictionary<string, object> dictionary = value as IDictionary<string, object>;
                if (dictionary == null)
                {
                    dictionary = new RouteValueDictionary(value);
                }
                string format = "{0}=\"{1}\" ";
                foreach (string str2 in dictionary.Keys)
                {
                    object obj2 = dictionary[str2];
                    if (dictionary[str2] is bool)
                    {
                        obj2 = dictionary[str2].ToString().ToLowerInvariant();
                    }
                    builder.AppendFormat(format, str2.Replace("_", "").ToLowerInvariant(), obj2);
                }
            }
            return builder.ToString();
        }

        /// <summary>
        /// Copies public property names and values into a case-sensitive hash table.
        /// </summary>
        /// <param name="properties">The object to inspect.</param>
        /// <returns>
        /// A hash table containing the public properties, or <see langword="null"/>
        /// when <paramref name="properties"/> is <see langword="null"/>.
        /// </returns>
        public static Hashtable GetPropertyHash(object properties)
        {

            Hashtable values = null;

            if (properties != null)
            {
                values = new Hashtable();

                PropertyDescriptorCollection props = TypeDescriptor.GetProperties(properties);

                foreach (PropertyDescriptor prop in props)
                {
                    values.Add(prop.Name, prop.GetValue(properties));
                }
            }

            return values;
        }
        #endregion


    }
}

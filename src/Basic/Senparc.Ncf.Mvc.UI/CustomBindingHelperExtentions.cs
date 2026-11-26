/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CustomBindingHelperExtentions.cs
    File description: CustomBindingHelperExtentions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260731
    Change description: v0.22.0-preview4 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.22.9-preview13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System.Text;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Senparc.Ncf.Core;

namespace System.Web.Mvc
{
    public static class CustomBindingHelperExtentions
    {
        //public static void UpdateFrom(this object obj, NameValueCollection values, Expression<Func<string, string>> encoder, params string[] keys)
        //{
        //    NameValueCollection encodedValues = new NameValueCollection();
        //    Func<string, string> encodeFunc = encoder.Compile();
        //    foreach (string key in values.Keys)
        //        encodedValues.Add(key, encodeFunc(values[key]));
        //    if (keys.Length > 0)
        //        obj.UpdateFrom(encodedValues, keys);
        //    else
        //        obj.UpdateFrom(encodedValues);
        //}


        /// <summary>
        /// Determines whether a name collection contains a key using trimmed, case-insensitive comparison.
        /// </summary>
        /// <param name="coll">The collection to inspect.</param>
        /// <param name="keyToFind">The key to locate.</param>
        /// <returns><see langword="true"/> when a matching key exists.</returns>
        public static bool ContainsKey(this NameValueCollection coll, string keyToFind)
        {
            bool bResult = false;
            foreach (string key in coll)
            {
                if (key.ToLower().Trim().Equals(keyToFind.ToLower().Trim()))
                {
                    bResult = true;
                    break;
                }
            }
            return bResult;
        }

        /// <summary>
        /// Renders move-up and move-down controls for an item in an ordered list.
        /// The returned markup uses the supplied JavaScript handlers and CSS classes.
        /// </summary>
        /// <param name="helper">The MVC HTML helper associated with the current view.</param>
        /// <param name="listCount">The total number of items in the ordered list.</param>
        /// <param name="currentIndex">The one-based position of the current item.</param>
        /// <param name="upFunction">The JavaScript expression invoked when moving up is allowed.</param>
        /// <param name="upClassEnable">The CSS class used for an enabled up control.</param>
        /// <param name="upClassDisable">The CSS class used for a disabled up control.</param>
        /// <param name="downFunction">The JavaScript expression invoked when moving down is allowed.</param>
        /// <param name="downClassEnable">The CSS class used for an enabled down control.</param>
        /// <param name="downClassDisable">The CSS class used for a disabled down control.</param>
        /// <returns>
        /// HTML containing localized move-up and move-down labels and the corresponding
        /// enabled or disabled image controls.
        /// </returns>
        public static string UpAndDown(this HtmlHelper helper, int listCount, int currentIndex,
                                    string upFunction, string upClassEnable, string upClassDisable,
                                    string downFunction, string downClassEnable, string downClassDisable)
        {
            StringBuilder result = new StringBuilder();
            result.Append("<span class=\"");
            if (currentIndex == 1)
            {
                result.Append(upClassDisable + "\" >");
                result.AppendFormat("<img src=\"/Images/up_un.gif\" />");//TODO:Put the img in to the css of the up/down SPAN.    ---- BY TNT2    2008.5.30
            }
            else
            {
                result.AppendFormat("{0}\" onclick=\"{1}\" >", upClassEnable, upFunction);
                result.AppendFormat("<img src=\"/Images/up_en.gif\" />");
            }
            result.Append(NcfCoreResource.Get("Pager.MoveUp", "上移")).Append("</span>");

            result.Append(" <span class=\"");
            if (currentIndex >= listCount)
            {
                result.Append(downClassDisable + "\" >");
                result.AppendFormat("<img src=\"/Images/down_un.gif\" />");

            }
            else
            {
                result.AppendFormat("{0}\" onclick=\"{1}\" >", downClassEnable, downFunction);
                result.AppendFormat("<img src=\"/Images/down_en.gif\" />");
            }
            result.Append(NcfCoreResource.Get("Pager.MoveDown", "下移")).Append("</span>");

            return result.ToString();
        }
    }
}

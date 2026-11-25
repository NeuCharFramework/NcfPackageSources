/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CommonWebParts.cs
    File description: CommonWebParts related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Senparc.Ncf.Core.Utility
{
    /// <summary>
    /// Provides common web formatting, request-form, and legacy currency conversion helpers.
    /// </summary>
    public static class CommonWebParts
    {
        //#region convert HTML code public static string exHTML(string ntext)
        /////// <summary>
        /////// convert HTML code——TNT2
        ///Legacy comment translated to English.
        /////// </summary>
        ////public static string exHTML(string ntext)
        ////{
        ////    ntext = ntext.ToString().Replace(" ", "&nbsp;").Replace(Convert.ToString((char)13), "<br>");
        ////    return ntext;
        ////}

        ///// <summary>
        ///// convert HTML code——TNT2
        ///Legacy comment translated to English.
        ///// </summary>
        //public static string ExHTML(this string ntext)
        //{
        //    ntext = ntext.ToString().Replace(" ", "&nbsp;").Replace(Convert.ToString((char)13), "<br />");
        //    return ntext;
        //}

        //public static string ExHtml(string ntext)
        //{
        //    ntext = ntext.ToString().Replace(" ", "&nbsp;").Replace(Convert.ToString((char)13), "<br />");
        //    return ntext;
        //}
        //#endregion

        /// <summary>
        /// Removes tags matching the simple <c>&lt;...&gt;</c> pattern and removes CRLF sequences.
        /// </summary>
        /// <param name="str">The HTML-like text to process.</param>
        /// <returns>The text with matching tags and CRLF sequences removed.</returns>
        public static string DelHtml(this string str) {
            if (!string.IsNullOrEmpty(str)) {
                str = Regex.Replace(str, "<[^>]*>", "").Replace("\r\n", "");
            }
            return str;
        }


        /// <summary>
        /// Reads one value from the current request form collection.
        /// </summary>
        /// <param name="key">The form key to read.</param>
        /// <param name="context">The HTTP context containing the request form.</param>
        /// <returns>The form value as a string, or an empty string when the key is absent.</returns>
        public static string GetFormValue(string key, HttpContext context)
        {
            return context.Request.Form[key].ToString();
        }


        /// <summary>
        /// Applies a format string to a current file name and its extension.
        /// </summary>
        /// <param name="fileFormat">A composite format string receiving the file name and extension.</param>
        /// <param name="currentFileName">The current file name whose extension is extracted.</param>
        /// <returns>The formatted upload file name.</returns>
        public static string GetFormattedUpLoadFileName(string fileFormat, string currentFileName)
        {
            return string.Format(fileFormat, currentFileName, Path.GetExtension(currentFileName));
        }

        #region Legacy section
        /// <summary>
        /// Converts a decimal amount to the legacy Chinese uppercase currency representation.
        /// </summary>
        /// <param name="num">The amount to round to two decimal places and convert.</param>
        /// <returns>The uppercase currency text, or <c>溢出</c> when the scaled amount exceeds the supported length.</returns>
        public static string CmycurD(decimal num)
        {
            string str1 = "零壹贰叁肆伍陆柒捌玖"; //Legacy comment translated to English.
            string str2 = "万仟佰拾亿仟佰拾万仟佰拾元角分"; //Legacy comment translated to English.
            string str3 = ""; //Legacy comment translated to English.
            string str4 = ""; //Legacy comment translated to English.
            string str5 = ""; //Legacy comment translated to English.
            int i; //Legacy comment translated to English.
            int j; //Legacy comment translated to English.
            string ch1 = ""; //Legacy comment translated to English.
            string ch2 = ""; //Legacy comment translated to English.
            int nzero = 0; //Legacy comment translated to English.
            int temp; //Legacy comment translated to English.
            num = Math.Round(Math.Abs(num), 2); //Legacy comment translated to English.
            str4 = ((long)(num * 100)).ToString(); //Legacy comment translated to English.
            j = str4.Length; //Legacy comment translated to English.
            if (j > 15) { return "溢出"; }
            str2 = str2.Substring(15 - j); //Legacy comment translated to English.
            //Legacy comment translated to English.
            for (i = 0; i < j; i++)
            {
                str3 = str4.Substring(i, 1); //Legacy comment translated to English.
                temp = Convert.ToInt32(str3); //Legacy comment translated to English.
                if (i != (j - 3) && i != (j - 7) && i != (j - 11) && i != (j - 15))
                {
                    //Legacy comment translated to English.
                    if (str3 == "0")
                    {
                        ch1 = "";
                        ch2 = "";
                        nzero = nzero + 1;
                    }
                    else
                    {
                        if (str3 != "0" && nzero != 0)
                        {
                            ch1 = "零" + str1.Substring(temp * 1, 1);
                            ch2 = str2.Substring(i, 1);
                            nzero = 0;
                        }
                        else
                        {
                            ch1 = str1.Substring(temp * 1, 1);
                            ch2 = str2.Substring(i, 1);
                            nzero = 0;
                        }
                    }
                }
                else
                {
                    //Legacy comment translated to English.
                    if (str3 != "0" && nzero != 0)
                    {
                        ch1 = "零" + str1.Substring(temp * 1, 1);
                        ch2 = str2.Substring(i, 1);
                        nzero = 0;
                    }
                    else
                    {
                        if (str3 != "0" && nzero == 0)
                        {
                            ch1 = str1.Substring(temp * 1, 1);
                            ch2 = str2.Substring(i, 1);
                            nzero = 0;
                        }
                        else
                        {
                            if (str3 == "0" && nzero >= 3)
                            {
                                ch1 = "";
                                ch2 = "";
                                nzero = nzero + 1;
                            }
                            else
                            {
                                if (j >= 11)
                                {
                                    ch1 = "";
                                    nzero = nzero + 1;
                                }
                                else
                                {
                                    ch1 = "";
                                    ch2 = str2.Substring(i, 1);
                                    nzero = nzero + 1;
                                }
                            }
                        }
                    }
                }
                if (i == (j - 11) || i == (j - 3))
                {
                    //Legacy comment translated to English.
                    ch2 = str2.Substring(i, 1);
                }
                str5 = str5 + ch1 + ch2;
                if (i == j - 1 && str3 == "0")
                {
                    //Legacy comment translated to English.
                    str5 = str5 + '整';
                }
            }
            if (num == 0)
            {
                str5 = "零元整";
            }
            return str5;
        }

        /// <summary>
        /// Converts a numeric string to the legacy Chinese uppercase currency representation.
        /// </summary>
        /// <param name="numstr">The numeric string to parse.</param>
        /// <returns>The uppercase currency text, or the legacy non-numeric marker when parsing fails.</returns>
        public static string CmycurD(string numstr)
        {
            try
            {
                decimal num = Convert.ToDecimal(numstr);
                return CmycurD(num);
            }
            catch
            {
                return "非数字形式！";
            }
        }

        #endregion

    }
}

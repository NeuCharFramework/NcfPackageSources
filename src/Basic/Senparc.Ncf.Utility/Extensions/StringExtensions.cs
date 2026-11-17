/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: StringExtensions.cs

    File description: StringExtensions related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.AspNetCore.Html;
using Senparc.CO2NET.Extensions;

namespace Senparc.Ncf.Core.Extensions
{
    public static class StringExtensions
    {
        //public static HtmlString RenderMeta(this HtmlHelper helper)
        //{
        //    if (!(helper.ViewData.Model is IBaseVD))
        //    {
        //        return new HtmlString("");
        //    }

        //    IBaseVD model = helper.ViewData.Model as IBaseVD;
        //    MetaCollection metaCollection = model.MetaCollection as MetaCollection;
        //    string result = null;
        //    foreach (var item in metaCollection)
        //    {
        //        if (!string.IsNullOrEmpty(item.Value))
        //        {
        //            result += string.Format("<meta name=\"{0}\" content=\"{1}\" />\r\n", item.Key.ToString(),
        //Legacy comment translated to English.
        //                item.Value
        //                );
        //        }
        //    }
        //    return new HtmlString(result);
        //}

        //public static string HtmlDnc(this string InputString)
        //{
        //    string tString = String.Empty;
        //    StringBuilder str = null;
        //    if (!string.IsNullOrEmpty(InputString))
        //    {
        //        tString = InputString;
        //        str = new StringBuilder(tString);
        //        str = str.Replace("&amp;", "&");
        //        str = str.Replace("&lt;", "<");
        //        str = str.Replace("&gt;", ">");
        //        str = str.Replace("　", "  ");
        //        str = str.Replace("&nbsp;", " ");
        //        str = str.Replace("&quot;", "\"\"");
        //        str = str.Replace("<br>", "\n");
        //    }
        //    return str.ToString();
        //}

        //public static string HtmlEnc(this string InputString)
        //{
        //    string tString = String.Empty;
        //    StringBuilder str = null;
        //    if (!string.IsNullOrEmpty(InputString))
        //    {
        //        tString = InputString;
        //        str = new StringBuilder(tString);
        //        str = str.Replace(">", "&gt;");
        //        str = str.Replace("<", "&lt;");
        //        str = str.Replace(" ", " &nbsp;");
        //        str = str.Replace(" ", " &nbsp;");
        //        str = str.Replace("\"", "&quot;");
        //        str = str.Replace("\'", "&#39;");
        //        str = str.Replace("\n", "<br/> ");
        //    }
        //    return str.ToString();
        //}


        //public static T ShowWhenNullOrEmpty<T>(this T obj, T defaultConent)
        //{
        //    if (obj == null)
        //    {
        //        return defaultConent;
        //    }
        //    else if (obj is String && obj.ToString() == "")
        //    {
        //        return defaultConent;
        //    }
        //    else
        //    {
        //        return obj;
        //    }
        //}

        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        ///// <param name="data"></param>
        ///// <returns></returns>
        //public static string ToJson(this object data)
        //{
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        DataContractJsonSerializer s = new DataContractJsonSerializer(data.GetType());
        //        s.WriteObject(ms, data);
        //        ms.Seek(0, SeekOrigin.Begin);

        //        return Encoding.UTF8.GetString(ms.ToArray());
        //    }
        //}

        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        ///// <param name="data"></param>
        ///// <returns></returns>
        //public static string ToJson(this object data)
        //{
        //    return Newtonsoft.Json.JsonConvert.SerializeObject(data);
        //}

        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        //public static string ToJson(this object obj, int recursionDepth)
        //{
        //    //JavaScriptSerializer serializer = new JavaScriptSerializer();
        //    //serializer.RecursionLimit = recursionDepth;
        //    //return serializer.Serialize(obj);

        //    return Newtonsoft.Json.JsonConvert.SerializeObject(obj, new Newtonsoft.Json.JsonSerializerSettings()
        //    {
        //        //TODO：settingsrecursionDepth  COCONET 
        //    });
        //}

        #region Legacy section
        ///// <summary>
        ///// convert HTML code——TNT2
        ///Legacy comment translated to English.
        ///// </summary>
        //public static string exHTML(string ntext)
        //{
        //    ntext = ntext.ToString().Replace(" ", "&nbsp;").Replace(Convert.ToString((char)13), "<br>");
        //    return ntext;
        //}

        /// <summary>
        /// convert HTML code——TNT2
        ///Legacy comment translated to English.
        /// </summary>
        public static HtmlString ExHTML(this string ntext)
        {
            if (!string.IsNullOrEmpty(ntext))
            {
                ntext = ntext.ToString().Replace("  ", " &nbsp;").Replace("\n", "<br />");//.Replace(Convert.ToString((char)13), "<br />");
                ntext = Regex.Replace(ntext, @"(?<url>http[s]?://([\w-]+\.)+[\w-]+([/\w-.?=%&;\(\):]*))", "<a href=\"${url}\">${url}</a>", RegexOptions.IgnoreCase);
            }
            return new HtmlString(ntext ?? "");
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string DelHtml(this string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                str = Regex.Replace(str, "<[^>]*>", "").Replace("\r\n", "");
            }
            return str;
        }

        //public static string HtmlEncode(this string str)
        //{
        //    return System.Web.HttpUtility.HtmlEncode(str);
        //}

        //public static string HtmlDecode(this string str)
        //{
        //    return System.Web.HttpUtility.HtmlDecode(str);
        //}

        /// <summary>
        /// return a check mark or cross according to the Boolean value
        /// </summary>
        /// <param name="yesOrNo">true:√,false:×</param>
        /// <returns></returns>
        public static HtmlString YesOrNo(this bool yesOrNo)
        {
            #region Legacy section
            if (yesOrNo)
            {
                return new HtmlString("<span class=\"red\">√</span>");
            }
            else
            {
                return new HtmlString("<span class=\"gray\">×</span>");
            }
            #endregion
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// 
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static string SubString(this string str, int startIndex, int maxLangth)
        {
            if (str == string.Empty || str == null)
            {
                return "";
            }
            else
            {
                string substring = "";

                //Legacy comment translated to English.
                if (startIndex > str.Length - 1)//Legacy comment translated to English.
                {
                    startIndex = (str.Length - maxLangth > 0) ? str.Length - maxLangth : 0;//Legacy comment translated to English.
                }

                //Legacy comment translated to English.
                if (startIndex + maxLangth > str.Length)//Legacy comment translated to English.
                {
                    maxLangth = str.Length - startIndex;//Legacy comment translated to English.
                }
                //Legacy comment translated to English.

                //Legacy comment translated to English.
                substring += (startIndex > 0) ? ".." : "";//Legacy comment translated to English.

                //Legacy comment translated to English.
                substring += str.Substring(startIndex, maxLangth);

                //Legacy comment translated to English.
                substring += (str.Length - startIndex - maxLangth > 0) ? "..." : "";//Legacy comment translated to English.

                return substring;
            }
        }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static string HighlightKeyword(this string str, string keyword)
        {
            if (str.IsNullOrEmpty())
            {
                return str;
            }

            if (!keyword.IsNullOrEmpty())
            {
                string replaceFormat = "<span class=\"red\">{0}</span>";//Legacy comment translated to English.
                str = Regex.Replace(str, string.Format(@"({0})", keyword), string.Format(replaceFormat, "$1"), RegexOptions.IgnoreCase);
            }
            return str;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="ip"></param>
        ///Legacy comment translated to English.
        /// <returns></returns>
        public static string HideIP(this string ip, int hideNum)
        {
            string[] ipItems = ip.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < hideNum; i++)
            {
                ipItems[3 - i] = "*";
            }
            return string.Join(".", ipItems);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static string HideIP(this string ip)
        {
            return ip.Substring(0, ip.LastIndexOf(".")) + ".*";
        }

        #endregion
    }
}
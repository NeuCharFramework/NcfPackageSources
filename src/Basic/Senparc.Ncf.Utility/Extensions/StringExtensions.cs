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
        //                //helper.AttributeEncode(item.Value) //COCONET This method is invalid
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
        ///// Convert data to Json format
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
        ///// Convert data to Json format (using Newtonsoft.Json.dll)
        ///// </summary>
        ///// <param name="data"></param>
        ///// <returns></returns>
        //public static string ToJson(this object data)
        //{
        //    return Newtonsoft.Json.JsonConvert.SerializeObject(data);
        //}

        ///// <summary>
        ///// Format into Json string
        ///// </summary>
        ///// <param name="obj">Object that needs to be formatted</param>
        ///// <param name="recursionDepth">Specifies the serialization depth</param>
        ///// <returns>Json string</returns>
        //public static string ToJson(this object obj, int recursionDepth)
        //{
        //    //JavaScriptSerializer serializer = new JavaScriptSerializer();
        //    //serializer.RecursionLimit = recursionDepth;
        //    //return serializer.Serialize(obj);

        //    return Newtonsoft.Json.JsonConvert.SerializeObject(obj, new Newtonsoft.Json.JsonSerializerSettings()
        //    {
        //        //TODO: Set recursionDepth COCONET 
        //    });
        //}

        #region 转换HTML代码 public static string exHTML(string ntext)
        ///// <summary>
        /////Convert HTML code——TNT2
        ///// Implemented: carriage return, space
        ///// </summary>
        //public static string exHTML(string ntext)
        //{
        //    ntext = ntext.ToString().Replace(" ", "&nbsp;").Replace(Convert.ToString((char)13), "<br>");
        //    return ntext;
        //}

        /// <summary>
        /// Convert HTML code - TNT2
        /// Implemented: carriage return, space
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
        /// Remove all HTML tags
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
        /// Returns √ or × based on Boolean value
        /// </summary>
        /// <param name="yesOrNo">true:√,false:×</param>
        /// <returns></returns>
        public static HtmlString YesOrNo(this bool yesOrNo)
        {
            #region 根据布尔值，返回√或×
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
        /// area string is of fixed length, the rest is omitted
        /// 
        /// rule:
        /// 1. If startIndex is greater than the string length, it will automatically adjust to the last maxLangth length. If the maxLangth length is greater than the string length at this time, then startIndex returns to 0
        /// 2. If the length of maxLangth is greater than the length of the string based on startIndex, then maxLangth will automatically take the maximum possible value, that is, from startIndex to the end of the string.
        /// 3. In the result, wherever there is a cut in the string, it will be replaced with ".."
        /// </summary>
        /// <param name="str">Original string</param>
        /// <param name="maxLangth">The longest number of characters</param>
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

                //Adjust startIndex
                if (startIndex > str.Length - 1)//If startIndex is greater than the string length
                {
                    startIndex = (str.Length - maxLangth > 0) ? str.Length - maxLangth : 0;//It will automatically adjust to the last maxLangth length. If the maxLangth length is greater than the string length at this time, then startIndex returns to 0
                }

                //Adjust maxLangth
                if (startIndex + maxLangth > str.Length)//If based on startIndex, the maxLangth length is greater than the string length
                {
                    maxLangth = str.Length - startIndex;//Then maxLangth automatically takes the maximum possible value, that is, from startIndex to the end of the string
                }
                //Adjustment completed

                //Add abbreviation
                substring += (startIndex > 0) ? ".." : "";//If the beginning is cut, replace it with ".."

                //Get a fixed-length string
                substring += str.Substring(startIndex, maxLangth);

                //Add abbreviation
                substring += (str.Length - startIndex - maxLangth > 0) ? "..." : "";//If the ending is cut, replace it with ".."

                return substring;
            }
        }


        /// <summary>
        /// Highlight keywords (red)
        /// </summary>
        /// <param name="str">Original string</param>
        /// <param name="keyword">Keyword</param>
        /// <returns></returns>
        public static string HighlightKeyword(this string str, string keyword)
        {
            if (str.IsNullOrEmpty())
            {
                return str;
            }

            if (!keyword.IsNullOrEmpty())
            {
                string replaceFormat = "<span class=\"red\">{0}</span>";//Replacement format
                str = Regex.Replace(str, string.Format(@"({0})", keyword), string.Format(replaceFormat, "$1"), RegexOptions.IgnoreCase);
            }
            return str;
        }

        /// <summary>
        /// Hide IP segment
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="hideNum">Hide several sections from the back</param>
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
        /// Hide IP segment (only hide the last IP segment)
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
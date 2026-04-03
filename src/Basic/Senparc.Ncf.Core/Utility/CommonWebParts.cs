using System;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Senparc.Ncf.Core.Utility
{
    public static class CommonWebParts
    {
        //#region Convert HTML code public static string exHTML(string ntext)
        /////// <summary>
        /////// Convert HTML code -- TNT2
        /////// Implemented: newline, space
        /////// </summary>
        ////public static string exHTML(string ntext)
        ////{
        ////    ntext = ntext.ToString().Replace(" ", "&nbsp;").Replace(Convert.ToString((char)13), "<br>");
        ////    return ntext;
        ////}

        ///// <summary>
        ///// Convert HTML code -- TNT2
        ///// Implemented: newline, space
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
        /// Remove all HTML tags
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string DelHtml(this string str) {
            if (!string.IsNullOrEmpty(str)) {
                str = Regex.Replace(str, "<[^>]*>", "").Replace("\r\n", "");
            }
            return str;
        }


        /// <summary>
        /// Get Form value, for Handler usage
        /// </summary>
        /// <param name="key">Form key</param>
        /// <param name="context">HttpContext</param>
        /// <returns></returns>
        public static string GetFormValue(string key, HttpContext context)
        {
            return context.Request.Form[key].ToString();
        }


        /// <summary>
        /// Get formatted file name
        /// </summary>
        /// <param name="fileFormat">File format (in Config.UpLoadFileFormat)</param>
        /// <param name="currentFileName">Current file name (path can be included)</param>
        /// <returns></returns>
        public static string GetFormattedUpLoadFileName(string fileFormat, string currentFileName)
        {
            return string.Format(fileFormat, currentFileName, Path.GetExtension(currentFileName));
        }

        #region Currency uppercase conversion
        /// <summary>
        /// Currency uppercase conversion
        /// </summary>
        /// <param name="num">Currency amount</param>
        /// <returns></returns>
        public static string CmycurD(decimal num)
        {
            string str1 = "零壹贰叁肆伍陆柒捌玖"; //Chinese characters corresponding to 0-9
            string str2 = "万仟佰拾亿仟佰拾万仟佰拾元角分"; //Chinese characters for digit places
            string str3 = ""; //Value extracted from original num
            string str4 = ""; //String representation of number
            string str5 = ""; //Uppercase RMB amount format
            int i; //Loop variable
            int j; //Length of num*100 string
            string ch1 = ""; //Chinese reading of digit
            string ch2 = ""; //Chinese reading of digit place
            int nzero = 0; //Count consecutive zero values
            int temp; //Value extracted from original num
            num = Math.Round(Math.Abs(num), 2); //Take absolute value and round to 2 decimals
            str4 = ((long)(num * 100)).ToString(); //Multiply num by 100 and convert to string
            j = str4.Length; //Find highest digit
            if (j > 15) { return "溢出"; }
            str2 = str2.Substring(15 - j); //Extract corresponding part of str2, e.g. 200.55 -> j=5 -> str2=佰拾元角分
            //Loop through digits to convert
            for (i = 0; i < j; i++)
            {
                str3 = str4.Substring(i, 1); //Get one digit to convert
                temp = Convert.ToInt32(str3); //Convert to number
                if (i != (j - 3) && i != (j - 7) && i != (j - 11) && i != (j - 15))
                {
                    //When current digit place is not yuan/ten-thousand/hundred-million/trillion
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
                    //Current place is a key place: trillion/hundred-million/ten-thousand/yuan
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
                    //If this place is hundred-million or yuan, it must be written
                    ch2 = str2.Substring(i, 1);
                }
                str5 = str5 + ch1 + ch2;
                if (i == j - 1 && str3 == "0")
                {
                    //If the last digit (fen) is 0, append "整"
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
        /// Currency uppercase conversion
        /// </summary>
        /// <param name="numstr">Currency amount</param>
        /// <returns></returns>
        public static string CmycurD(string numstr)
        {
            try
            {
                decimal num = Convert.ToDecimal(numstr);
                return CmycurD(num);
            }
            catch
            {
                return "Non-numeric format!";
            }
        }

        #endregion

    }
}

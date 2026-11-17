/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MD5.cs
    File description: MD5 related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Security.Cryptography;
using System.Text;

namespace Senparc.Ncf.Core.Utility
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static class MD5
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// <param name="encoding">defaultfor UTF8</param>
        /// <returns></returns>
        public static string GetMD5Code(string str, string salt, Encoding encoding = null)
        {
            return Senparc.CO2NET.Helpers.EncryptHelper.GetMD5(str + salt, encoding ?? Encoding.UTF8);
        }
    }
}

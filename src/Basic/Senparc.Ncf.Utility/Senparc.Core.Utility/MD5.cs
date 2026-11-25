/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MD5.cs
    File description: MD5 related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Security.Cryptography;
using System.Text;

namespace Senparc.Ncf.Core.Utility
{
    /// <summary>
    /// Provides MD5 hashing helpers used for cache keys and legacy identifiers.
    /// </summary>
    public static class MD5
    {
        /// <summary>
        /// Computes the MD5 digest of the concatenation of <paramref name="str"/> and <paramref name="salt"/>.
        /// </summary>
        /// <param name="str">The input text.</param>
        /// <param name="salt">The suffix appended before hashing.</param>
        /// <param name="encoding">The text encoding; UTF-8 is used when omitted.</param>
        /// <returns>The hexadecimal MD5 representation returned by the shared encryption helper.</returns>
        public static string GetMD5Code(string str, string salt, Encoding encoding = null)
        {
            return Senparc.CO2NET.Helpers.EncryptHelper.GetMD5(str + salt, encoding ?? Encoding.UTF8);
        }
    }
}

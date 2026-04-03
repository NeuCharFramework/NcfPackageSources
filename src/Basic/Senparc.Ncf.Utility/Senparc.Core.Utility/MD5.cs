using System;
using System.Security.Cryptography;
using System.Text;

namespace Senparc.Ncf.Core.Utility
{
    /// <summary>
    ///MD5 encryption
    /// </summary>
    public static class MD5
    {
        /// <summary>
        /// Obtain globally consistent salted MD5 encryption results within the NCF system
        /// </summary>
        /// <param name="str">Original password</param>
        /// <param name="salt">Salt</param>
        /// <param name="encoding">Default is UTF8</param>
        /// <returns></returns>
        public static string GetMD5Code(string str, string salt, Encoding encoding = null)
        {
            return Senparc.CO2NET.Helpers.EncryptHelper.GetMD5(str + salt, encoding ?? Encoding.UTF8);
        }
    }
}

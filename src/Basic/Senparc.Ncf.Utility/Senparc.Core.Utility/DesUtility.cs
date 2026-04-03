using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Senparc.Ncf.Core.Utility
{
    public class DesUtility
    {
        //Default key vector
        private static byte[] Keys = { 0xA2, 0x24, 0x26, 0x77, 0x99, 0xAB, 0xEF, 0x88 };

        /// <summary>
        ///DES encrypted string
        /// </summary>
        /// <param name="encryptString">String to be encrypted</param>
        /// <param name="encryptKey">Encryption key, required to be 8 bits</param>
        /// <returns>Encryption successfully returns the encrypted string, failure returns the source string</returns>
        public static string EncryptDES(string encryptString, string encryptKey)
        {
            try
            {
                byte[] rgbKey = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));
                byte[] rgbIV = Keys;
                byte[] inputByteArray = Encoding.UTF8.GetBytes(encryptString);
                DESCryptoServiceProvider dCSP = new DESCryptoServiceProvider();
                MemoryStream mStream = new MemoryStream();
                CryptoStream cStream = new CryptoStream(mStream, dCSP.CreateEncryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return Convert.ToBase64String(mStream.ToArray());
            }
            catch
            {
                return encryptString;
            }
        }

        /// <summary>
        ///DES decrypt string
        /// </summary>
        /// <param name="decryptString">String to be decrypted</param>
        /// <param name="decryptKey">The decryption key, which is required to be 8 bits, is the same as the encryption key</param>
        /// <returns>Returns the decrypted string if decryption is successful, otherwise returns the source string</returns>
        public static string DecryptDES(string decryptString, string decryptKey)
        {
            try
            {
                byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey.Substring(0, 8));
                byte[] rgbIV = Keys;
                byte[] inputByteArray = Convert.FromBase64String(decryptString);
                DESCryptoServiceProvider DCSP = new DESCryptoServiceProvider();
                MemoryStream mStream = new MemoryStream();
                CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return Encoding.UTF8.GetString(mStream.ToArray());
            }
            catch
            {
                return decryptString;
            }
        }
    }
}
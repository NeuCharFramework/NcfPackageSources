/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DesUtility.cs
    File description: DesUtility related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Senparc.Ncf.Core.Utility
{
    /// <summary>
    /// Provides legacy DES encryption helpers with a fixed initialization vector.
    /// Failures are handled by returning the original input for compatibility with
    /// existing callers.
    /// </summary>
    public class DesUtility
    {
        // Fixed legacy initialization vector retained for compatibility with existing ciphertext.
        private static byte[] Keys = { 0xA2, 0x24, 0x26, 0x77, 0x99, 0xAB, 0xEF, 0x88 };

        /// <summary>
        /// Encrypts UTF-8 text with DES using the first eight characters of the supplied key.
        /// Any encryption or key-format failure returns the original input unchanged.
        /// </summary>
        /// <param name="encryptString">The plaintext to encrypt.</param>
        /// <param name="encryptKey">The key whose first eight characters form the DES key.</param>
        /// <returns>The Base64 ciphertext, or the original plaintext when encryption fails.</returns>
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
        /// Decrypts Base64 ciphertext with DES using the first eight characters of the supplied key.
        /// Any decryption, Base64, or key-format failure returns the original input unchanged.
        /// </summary>
        /// <param name="decryptString">The Base64 ciphertext to decrypt.</param>
        /// <param name="decryptKey">The key whose first eight characters form the DES key.</param>
        /// <returns>The decrypted UTF-8 text, or the original ciphertext when decryption fails.</returns>
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

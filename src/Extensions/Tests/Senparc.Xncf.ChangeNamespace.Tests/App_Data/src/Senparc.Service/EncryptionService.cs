using Senparc.CO2NET;
using Senparc.Ncf.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Service
{
    //public partial interface IEncryptionService : IEncryptionServiceBase
    //{
    //    string CommonEncrypt(string str);
    //    string CommonDecrypt(string str);

    //    string GetEncodedUserAvatar(int accountId, string encodeKey);
    //    int GetDecodedUserAvatar(string code, string encodeKey);

    //    string GetEncodedWeixinTokenPass(int weixinApp, string token, string encodeKey);
    //    Dictionary<int, string> CheckDecodedWeixinTokenPass(string code, string encodeKey);

    //    string GetEncodedQyAppKey(int qyAppId, DateTime qyAppAddTime, string encodeKey);
    //    //QyApp CheckDecodedQyAppKey(string code, string encodeKey);

    //    string EncodeAppCode(int appId, int neuralAppId, string encodeKey);
    //    int[] DecodeAppCode(string appCode, string encodeKey);
    //}

    public partial class EncryptionService : EncryptionServiceBase//, IEncryptionService
    {
        public const string BASE_ENCRYPT_ENCODING_KEY = "3Eddo*8jp";

        /// <summary>Initializes the sample encryption service.</summary>
        public EncryptionService()//(IBaseData baseData): base(baseData)
        { }

        /// <summary>
        /// Encrypts text after appending the service's fixed encoding marker.
        /// </summary>
        /// <param name="str">The plaintext value to protect.</param>
        /// <returns>The DES-encrypted marker-bearing value.</returns>
        public string CommonEncrypt(string str)
        {
            var content = $"{str}|{BASE_ENCRYPT_ENCODING_KEY}";
            return DesUtility.EncryptDES(content, BASE_ENCRYPT_ENCODING_KEY);
        }

        /// <summary>
        /// Decrypts a value using the sample encoding key and returns its first
        /// pipe-delimited component.
        /// </summary>
        /// <param name="str">The encrypted value.</param>
        /// <returns>The first component of the decrypted value.</returns>
        public string CommonDecrypt(string str)
        {
            var content = DesUtility.EncryptDES(str, BASE_ENCRYPT_ENCODING_KEY);
            return str.Split('|')[0];
        }

        #region UserAvatar

        /// <summary>Creates a short-lived encoded avatar token for an account.</summary>
        /// <param name="accountId">The account identifier embedded in the token.</param>
        /// <param name="encodeKey">The DES key used for encoding.</param>
        /// <returns>The encoded timestamp/account token.</returns>
        public string GetEncodedUserAvatar(int accountId, string encodeKey)
        {
            var content = $"{DateTime.Now.Ticks}|{accountId}";
            return DesUtility.EncryptDES(content, encodeKey);
        }

        /// <summary>
        /// Decodes an avatar token and returns the account identifier when it is
        /// valid and no older than fifteen seconds.
        /// </summary>
        /// <param name="code">The encoded avatar token.</param>
        /// <param name="encodeKey">The DES key used for decoding.</param>
        /// <returns>The account identifier, or <c>-1</c> for invalid or expired tokens.</returns>
        public int GetDecodedUserAvatar(string code, string encodeKey)
        {
            try
            {
                var content = DesUtility.DecryptDES(code, encodeKey);
                var datas = content.Split('|');
                var dateTime = new DateTime(long.Parse(datas[0]));

                if ((DateTime.Now - dateTime).TotalSeconds > 15)
                {
                    return -1;//Legacy comment translated to English.
                }

                var accountId = int.Parse(datas[1]);
                return accountId;
            }
            catch
            {
                return -1;
            }
        }

        #endregion
    }
}

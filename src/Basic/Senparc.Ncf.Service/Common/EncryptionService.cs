/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: EncryptionService.cs
    File description: EncryptionService related implementation

    Created by: Senparc - 20211128

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.Ncf.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.Service
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

    /// <summary>
    /// Provides the legacy NCF encryption and short-lived token encoding helpers.
    /// </summary>
    public partial class EncryptionService : EncryptionServiceBase//, IEncryptionService
    {
        public const string BASE_ENCRYPT_ENCODING_KEY = "3Eddo*8jp";


        /// <summary>
        /// Initializes the encryption service without additional state.
        /// </summary>
        public EncryptionService()//(IBaseData baseData): base(baseData)
        { }

        /// <summary>
        /// Appends the shared encoding key to a value and encrypts the resulting payload
        /// with the legacy DES utility.
        /// </summary>
        /// <param name="str">The plaintext value to encrypt.</param>
        /// <returns>The DES-encrypted payload.</returns>
        public string CommonEncrypt(string str)
        {
            var content = $"{str}|{BASE_ENCRYPT_ENCODING_KEY}";
            return DesUtility.EncryptDES(content, BASE_ENCRYPT_ENCODING_KEY);
        }

        /// <summary>
        /// Processes an encrypted value through the legacy DES helper and returns the first
        /// pipe-delimited segment of the supplied input.
        /// </summary>
        /// <param name="str">The pipe-delimited value supplied by the caller.</param>
        /// <returns>The first segment of <paramref name="str"/>.</returns>
        public string CommonDecrypt(string str)
        {
            var content = DesUtility.EncryptDES(str, BASE_ENCRYPT_ENCODING_KEY);
            return str.Split('|')[0];
        }

        #region UserAvatar

        /// <summary>
        /// Encodes an account identifier together with the current timestamp for a short-lived
        /// avatar token.
        /// </summary>
        /// <param name="accountId">The account identifier embedded in the token.</param>
        /// <param name="encodeKey">The DES key used to encrypt the token.</param>
        /// <returns>An encrypted timestamp/account token.</returns>
        public string GetEncodedUserAvatar(int accountId, string encodeKey)
        {
            var content = $"{DateTime.Now.Ticks}|{accountId}";
            return DesUtility.EncryptDES(content, encodeKey);
        }

        /// <summary>
        /// Decrypts and validates a short-lived avatar token. Tokens older than fifteen seconds,
        /// malformed tokens, and tokens that cannot be decrypted return <c>-1</c>.
        /// </summary>
        /// <param name="code">The encrypted avatar token.</param>
        /// <param name="encodeKey">The DES key used to decrypt the token.</param>
        /// <returns>The account identifier, or <c>-1</c> when validation fails or the token has expired.</returns>
        public int GetDecodedUserAvatar(string code, string encodeKey)
        {
            try
            {
                var content = DesUtility.DecryptDES(code, encodeKey);
                var datas = content.Split('|');
                var dateTime = new DateTime(long.Parse(datas[0]));

                if ((DateTime.Now - dateTime).TotalSeconds > 15)
                {
                    return -1;
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

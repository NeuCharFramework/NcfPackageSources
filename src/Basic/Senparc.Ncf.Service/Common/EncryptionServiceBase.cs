/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: EncryptionServiceBase.cs
    File description: EncryptionServiceBase related implementation

    Created by: Senparc - 20211128

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Utility;

namespace Senparc.Ncf.Service
{
    public partial interface IEncryptionServiceBase //: IBaseServiceData
    {
        string GetEncodedContent(string content, string encodeKey);
        string GetDecodedContent(string content, string encodeKey);
    }

    public class EncryptionServiceBase :/* BaseServiceData,*/ IEncryptionServiceBase
    {
        /// <summary>
        /// Initializes the DES-based encryption service.
        /// </summary>
        public EncryptionServiceBase()//(IBaseData baseData): base(baseData)
        { }

        /// <summary>
        /// Encrypts content with the configured DES-compatible utility and key.
        /// </summary>
        /// <param name="content">The plaintext content to encrypt.</param>
        /// <param name="encodeKey">The key used by <see cref="DesUtility.EncryptDES(string, string)"/>.</param>
        /// <returns>The encrypted representation returned by the utility.</returns>
        public string GetEncodedContent(string content, string encodeKey)
        {
            return DesUtility.EncryptDES(content, encodeKey);
        }

        /// <summary>
        /// Decrypts content with the configured DES-compatible utility and key.
        /// </summary>
        /// <param name="content">The encrypted content to decrypt.</param>
        /// <param name="encodeKey">The key used by <see cref="DesUtility.DecryptDES(string, string)"/>.</param>
        /// <returns>The decrypted plaintext returned by the utility.</returns>
        public string GetDecodedContent(string content, string encodeKey)
        {
            return DesUtility.DecryptDES(content, encodeKey);
        }
    }
}

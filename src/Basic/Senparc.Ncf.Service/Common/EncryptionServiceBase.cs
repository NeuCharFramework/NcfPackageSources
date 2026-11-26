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
        /// <summary>
        /// Encrypts plaintext content with the implementation's configured algorithm and key.
        /// </summary>
        /// <param name="content">The plaintext content.</param>
        /// <param name="encodeKey">The encryption key.</param>
        /// <returns>The encrypted representation.</returns>
        string GetEncodedContent(string content, string encodeKey);

        /// <summary>
        /// Decrypts content produced by <see cref="GetEncodedContent"/>.
        /// </summary>
        /// <param name="content">The encrypted representation.</param>
        /// <param name="encodeKey">The decryption key.</param>
        /// <returns>The plaintext content.</returns>
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

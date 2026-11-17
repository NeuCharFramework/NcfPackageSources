/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: EncryptionServiceBase.cs
    File description: EncryptionServiceBase related implementation

    Created by: Senparc - 20211128

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
        public EncryptionServiceBase()//(IBaseData baseData): base(baseData)
        { }

        public string GetEncodedContent(string content, string encodeKey)
        {
            return DesUtility.EncryptDES(content, encodeKey);
        }

        public string GetDecodedContent(string content, string encodeKey)
        {
            return DesUtility.DecryptDES(content, encodeKey);
        }
    }
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：EncryptionServiceBase.cs
    文件功能描述：EncryptionServiceBase 相关实现
    
    
    创建标识：Senparc - 20211128
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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

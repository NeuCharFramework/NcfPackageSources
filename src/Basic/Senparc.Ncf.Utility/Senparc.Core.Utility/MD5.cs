/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MD5.cs
    文件功能描述：MD5 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Security.Cryptography;
using System.Text;

namespace Senparc.Ncf.Core.Utility
{
    /// <summary>
    /// MD5 加密
    /// </summary>
    public static class MD5
    {
        /// <summary>
        /// 获得 NCF 系统内全局一致的加盐的 MD5 加密结果
        /// </summary>
        /// <param name="str">原始密码</param>
        /// <param name="salt">盐</param>
        /// <param name="encoding">默认为 UTF8</param>
        /// <returns></returns>
        public static string GetMD5Code(string str, string salt, Encoding encoding = null)
        {
            return Senparc.CO2NET.Helpers.EncryptHelper.GetMD5(str + salt, encoding ?? Encoding.UTF8);
        }
    }
}

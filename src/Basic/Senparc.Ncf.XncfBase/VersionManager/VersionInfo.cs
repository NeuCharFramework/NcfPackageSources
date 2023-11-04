using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.VersionManager
{
    /// <summary>  
    /// 软件版本信息
    /// </summary>  
    public class VersionInfo
    {
        /// <summary>  
        /// 主版本号  
        /// </summary>  
        public int Major { get; set; }

        /// <summary>  
        /// 次版本号  
        /// </summary>  
        public int Minor { get; set; }

        /// <summary>  
        /// 修订版本号  
        /// </summary>  
        public int Patch { get; set; }

        /// <summary>  
        /// 可选的构建版本号  
        /// </summary>  
        public int? Build { get; set; }

        /// <summary>  
        /// 可选的预发布版本标签  
        /// </summary>  
        public string PreRelease { get; set; }

        /// <summary>  
        /// 可选的元数据标签  
        /// </summary>  
        public string Metadata { get; set; }


        /// <summary>  
        /// 将 VersionInfo 对象转换为版本字符串。  
        /// </summary>  
        /// <returns>表示版本信息的字符串。</returns>  
        /// <summary>  
        /// 将 VersionInfo 对象转换为版本字符串。  
        /// </summary>  
        /// <returns>表示版本信息的字符串。</returns>  
        public override string ToString()
        {
            var versionString = $"{Major}.{Minor}.{Patch}";

            // 如果存在 Build 属性，则将其添加到版本字符串中  
            if (Build.HasValue)
            {
                versionString += $".{Build.Value}";
            }

            if (!string.IsNullOrEmpty(PreRelease))
            {
                versionString += $"-{PreRelease}";
            }

            if (!string.IsNullOrEmpty(Metadata))
            {
                versionString += $"+{Metadata}";
            }

            return versionString;
        }

    }
}

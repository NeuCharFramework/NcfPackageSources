/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FileSaveUtility.cs
    文件功能描述：FileSaveUtility 相关实现
    
    
    创建标识：Senparc - 20211215
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System.IO;

namespace Senparc.Ncf.Utility
{
    public static class FileSaveUtility
    {
        public static string GetAvailableFileName(string filePath)
        {
            string fileExtension = Path.GetExtension(filePath);
            string fileNameWithoutExtension = filePath.Substring(0, filePath.Length - fileExtension.Length);
            string newFilePath = filePath;
            int i = 1;
            while (File.Exists(newFilePath))
            {
                newFilePath = $"{fileNameWithoutExtension}({i.ToString()}){fileExtension}";
                i++;
            }
            return newFilePath;
        }
    }
}
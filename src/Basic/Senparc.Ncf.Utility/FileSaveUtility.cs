/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: FileSaveUtility.cs

    File description: FileSaveUtility related implementation

    

    

    Created by: Senparc - 20211215

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
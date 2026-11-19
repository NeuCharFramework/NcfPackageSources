/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FileSaveUtility.cs
    File description: FileSaveUtility related implementation

    Created by: Senparc - 20211215

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

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
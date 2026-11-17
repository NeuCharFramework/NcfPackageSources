/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: FileUtility.cs

    File description: FileUtility related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Utility
{
    public class FileUtility
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        public static string GetRandomFileName()
        {
            return $"{DateTime.Now.Ticks}{Guid.NewGuid().ToString("n").Substring(0, 8)}";
        }
    }
}
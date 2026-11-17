/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    File name: AllowFileExtension.cs
    File description: AllowFileExtension related implementation
    
    
    Created by: Senparc - 20260213
    
    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Config
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class AllowFileExtension
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static readonly HashSet<string> ImageExtension = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp"
        };
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static readonly HashSet<string> VideoExtensiong = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".flv", ".swf", ".mkv", ".avi", ".rm", ".rmvb", ".mpeg", ".mpg",
            ".ogg", ".ogv", ".mov", ".wmv", ".mp4", ".webm", ".mp3", ".wav", ".mid"
        };
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static readonly HashSet<string> FileExtension = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp",
            ".flv", ".swf", ".mkv", ".avi", ".rm", ".rmvb", ".mpeg", ".mpg",
            ".ogg", ".ogv", ".mov", ".wmv", ".mp4", ".webm", ".mp3", ".wav", ".mid",
            ".rar", ".zip", ".tar", ".gz", ".7z", ".bz2", ".cab", ".iso",
            ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".pdf", ".txt", ".md", ".xml"
        };
    }
}

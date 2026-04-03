using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.Config
{
    /// <summary>
    /// Allow extension classes
    /// </summary>
    public class AllowFileExtension
    {
        /// <summary>
        ///image extension
        /// </summary>
        public static readonly HashSet<string> ImageExtension = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp"
        };
        /// <summary>
        /// audio and video extension
        /// </summary>
        public static readonly HashSet<string> VideoExtensiong = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".flv", ".swf", ".mkv", ".avi", ".rm", ".rmvb", ".mpeg", ".mpg",
            ".ogg", ".ogv", ".mov", ".wmv", ".mp4", ".webm", ".mp3", ".wav", ".mid"
        };
        /// <summary>
        /// file extension
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

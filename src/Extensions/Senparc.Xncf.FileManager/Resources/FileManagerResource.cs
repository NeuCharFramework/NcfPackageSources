/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FileManagerResource.cs
    File description: FileManagerResource related implementation

    Created by: Senparc - 20260717

    Modified by: Senparc - 20260717
    Change description: v0.3.0-preview2 Updated module behavior and related support.

----------------------------------------------------------------*/
#nullable enable

using Senparc.Ncf.Core.Localization;

namespace Senparc.Xncf.FileManager
{
    /// <summary>
    /// Localization catalog owned and packaged by the FileManager module.
    /// </summary>
    public sealed class FileManagerResource
    {
        public static string Get(string key, string? fallback = null)
        {
            return ResourceStringLocalizer.Get(typeof(FileManagerResource), key, fallback);
        }

        public static string Format(string key, string fallback, params object[] arguments)
        {
            return ResourceStringLocalizer.Format(typeof(FileManagerResource), key, fallback, arguments);
        }
    }
}

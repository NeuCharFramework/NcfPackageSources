/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: CloudflareProtectOptions.cs
    File description: Related implementation.

    Created by: Senparc - 20260906

    Modified by: Senparc - 20260915
    Change description: v0.39.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;

namespace Senparc.Web.Infrastructure.Security
{
    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public sealed class CloudflareProtectOptions
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public const string SectionName = "CloudflareProtect";

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int WindowSeconds { get; set; } = 60;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int MaxRequestsPerWindow { get; set; } = 120;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool AddSecurityHeaders { get; set; } = true;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public List<string> ProtectedPathPrefixes { get; set; } = new List<string>();
    }
}

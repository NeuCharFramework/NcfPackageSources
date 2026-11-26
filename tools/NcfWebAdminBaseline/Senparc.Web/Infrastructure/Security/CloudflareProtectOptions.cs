/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CloudflareProtectOptions.cs
    File description: Related implementation.

    Created by: Senparc - 20260906

    Modified by: Senparc - 20260915
    Change description: v0.39.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.39.9 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;

namespace Senparc.Web.Infrastructure.Security
{
    /// <summary>
    /// Configures Cloudflare-aware request protection and its fixed-window limits.
    /// </summary>
    public sealed class CloudflareProtectOptions
    {
        /// <summary>
        /// The configuration section name used to bind these options.
        /// </summary>
        public const string SectionName = "CloudflareProtect";

        /// <summary>
        /// Gets or sets whether Cloudflare protection is enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the rate-limit window length in seconds.
        /// </summary>
        public int WindowSeconds { get; set; } = 60;

        /// <summary>
        /// Gets or sets the maximum requests allowed per window.
        /// </summary>
        public int MaxRequestsPerWindow { get; set; } = 120;

        /// <summary>
        /// Gets or sets whether response security headers are added.
        /// </summary>
        public bool AddSecurityHeaders { get; set; } = true;

        /// <summary>
        /// Gets or sets the path prefixes to which protection applies.
        /// </summary>
        public List<string> ProtectedPathPrefixes { get; set; } = new List<string>();
    }
}

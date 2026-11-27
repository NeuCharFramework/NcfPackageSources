/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: VersionInfo.cs
    File description: VersionInfo related implementation

    Created by: Senparc - 20231104

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.29.1.2 Extend shared chat-tool contracts for admin task initiation.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.VersionManager
{
    /// <summary>Represents a parsed semantic version and optional prerelease metadata.</summary>
    public record class VersionInfo
    {
        /// <summary>Gets or sets the major version component.</summary>
        public int Major { get; set; }

        /// <summary>Gets or sets the minor version component.</summary>
        public int Minor { get; set; }

        /// <summary>Gets or sets the patch version component.</summary>
        public int Patch { get; set; }

        /// <summary>Gets or sets the optional build component.</summary>
        public int? Build { get; set; }

        /// <summary>Gets or sets the prerelease label.</summary>
        public string PreRelease { get; set; }

        /// <summary>Gets or sets the build metadata label.</summary>
        public string Metadata { get; set; }


        /// <summary>Returns the normalized semantic-version string.</summary>
        public override string ToString()
        {
            var versionString = $"{Major}.{Minor}.{Patch}";

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

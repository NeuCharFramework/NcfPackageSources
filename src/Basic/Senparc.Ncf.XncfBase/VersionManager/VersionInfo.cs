using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.VersionManager
{
    /// <summary>  
    ///Software version information
    /// </summary>  
    public record class VersionInfo
    {
        /// <summary>  
        ///major version
        /// </summary>  
        public int Major { get; set; }

        /// <summary>  
        ///minor version
        /// </summary>  
        public int Minor { get; set; }

        /// <summary>  
        ///revision
        /// </summary>  
        public int Patch { get; set; }

        /// <summary>  
        /// optional build version
        /// </summary>  
        public int? Build { get; set; }

        /// <summary>  
        /// optional pre-release tag  
        /// </summary>  
        public string PreRelease { get; set; }

        /// <summary>  
        /// Optional metadata tag  
        /// </summary>  
        public string Metadata { get; set; }


        /// <summary>  
        /// Convert a VersionInfo object to a version string.  
        /// </summary>  
        ///<returns>A string representing version information. </returns>  
        /// <summary>  
        /// Convert a VersionInfo object to a version string.  
        /// </summary>  
        ///<returns>A string representing version information. </returns>  
        public override string ToString()
        {
            var versionString = $"{Major}.{Minor}.{Patch}";

            // If the Build attribute is present, add it to the version string  
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

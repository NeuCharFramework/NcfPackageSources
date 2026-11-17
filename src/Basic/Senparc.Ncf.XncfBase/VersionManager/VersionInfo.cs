/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: VersionInfo.cs

    File description: VersionInfo related implementation

    

    

    Created by: Senparc - 20231104

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.VersionManager
{
    /// <summary>  
    ///Legacy comment translated to English.
    /// </summary>  
    public record class VersionInfo
    {
        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public int Major { get; set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public int Minor { get; set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public int Patch { get; set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public int? Build { get; set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public string PreRelease { get; set; }

        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        public string Metadata { get; set; }


        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        ///Legacy comment translated to English.
        /// <summary>  
        ///Legacy comment translated to English.
        /// </summary>  
        ///Legacy comment translated to English.
        public override string ToString()
        {
            var versionString = $"{Major}.{Minor}.{Patch}";

            //Legacy comment translated to English.
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

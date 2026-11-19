/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: JwtSettings.cs
    File description: JwtSettings related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

namespace Senparc.Areas.Admin
{
    public class JwtSettings
    {

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public const string Position_MiniPro = "MiniProJwt";

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public const string Position_Backend = "BackendJwt";

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public double Expires { get; set; }
    }
}

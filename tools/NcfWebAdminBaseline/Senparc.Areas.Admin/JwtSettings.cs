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
    /// <summary>Contains configuration values used to issue and validate backend JWTs.</summary>
    public class JwtSettings
    {

        /// <summary>
        /// Configuration section name for MiniProgram JWT settings.
        /// </summary>
        public const string Position_MiniPro = "MiniProJwt";

        /// <summary>
        /// Configuration section name for backend JWT settings.
        /// </summary>
        public const string Position_Backend = "BackendJwt";

        /// <summary>
        /// Gets or sets the token issuer.
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// Gets or sets the token audience.
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// Gets or sets the signing secret.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// Gets or sets the token lifetime value configured by the host.
        /// </summary>
        public double Expires { get; set; }
    }
}

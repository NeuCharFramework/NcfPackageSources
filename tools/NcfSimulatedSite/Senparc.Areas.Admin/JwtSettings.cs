namespace Senparc.Areas.Admin
{
    public class JwtSettings
    {

        /// <summary>
        /// front desk jwt
        /// </summary>
        public const string Position_MiniPro = "MiniProJwt";

        /// <summary>
        /// Management backend jwt
        /// </summary>
        public const string Position_Backend = "BackendJwt";

        /// <summary>
        /// Who issued the token?
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        //Which clients / token can be used by?
        /// </summary>
        public string Audience { get; set; }

        /// <summary>
        /// encrypted key
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// Expiration time, unit [hour]
        /// </summary>
        public double Expires { get; set; }
    }
}

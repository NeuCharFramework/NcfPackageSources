/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CustomSwaggerAuth.cs
    File description: CustomSwaggerAuth related implementation

    Created by: Senparc - 20210614

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.12 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Xncf.Swagger.Utils;

namespace Senparc.Xncf.Swagger.Models
{
    public class CustomSwaggerAuth
    {
        public CustomSwaggerAuth() { }
        public CustomSwaggerAuth(string userName, string userPwd)
        {
            UserName = userName;
            UserPwd = userPwd;
        }
        public string UserName { get; set; }
        public string UserPwd { get; set; }
        public string AuthStr
        {
            get
            {
                return SecurityHelper.HMACSHA256(UserName + UserPwd);
            }
        }
    }
}

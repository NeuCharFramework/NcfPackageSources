/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：CustomSwaggerAuth.cs
    文件功能描述：CustomSwaggerAuth 相关实现
    
    
    创建标识：Senparc - 20210614
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：PermissionAttribute.cs
    文件功能描述：PermissionAttribute 相关实现
    
    
    创建标识：Senparc - 20211223
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Senparc.Ncf.Core.Authorization
{
    /// <summary>
    /// 权限特性
    /// 用法[Permission("role.add,role.update")]
    /// </summary>
    public class PermissionAttribute : TypeFilterAttribute
    {
        //public string[] Codes { get; set; }

        /// <summary>
        /// code里面不能存在英文逗号 TODO
        /// </summary>
        /// <param name="codes">多个code 英文逗号分割</param>
        public PermissionAttribute(string codes)
            : base(typeof(PermissionFilterAttribute))
        {
            //Arguments = new[] { new PermissionRequirement(Codes) };
            Arguments = new[] { new PermissionRequirement(codes.Split(",")) };
        }
    }
}

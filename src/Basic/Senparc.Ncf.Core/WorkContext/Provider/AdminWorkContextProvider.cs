/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AdminWorkContextProvider.cs
    文件功能描述：AdminWorkContextProvider 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace Senparc.Ncf.Core.WorkContext.Provider
{
    public class AdminWorkContextProvider : IAdminWorkContextProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminWorkContextProvider(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        public AdminWorkContext GetAdminWorkContext()
        {
            AdminWorkContext adminWorkContext = new AdminWorkContext();

            adminWorkContext.UserName = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.Name)?.Value;
            bool isConvertSucess = int.TryParse(_httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.NameIdentifier)?.Value, out int convertId);
            if (isConvertSucess)
            {
                adminWorkContext.AdminUserId = convertId;
            }
            adminWorkContext.RoleCodes = _httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(_ => _.Type == ClaimTypes.Role)?.Value?.Split(",").ToList() ?? new List<string>();
            return adminWorkContext;
        }
    }
}

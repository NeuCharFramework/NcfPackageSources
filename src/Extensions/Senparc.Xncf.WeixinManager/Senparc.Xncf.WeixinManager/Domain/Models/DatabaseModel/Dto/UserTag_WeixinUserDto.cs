/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：UserTag_WeixinUserDto.cs
    文件功能描述：UserTag_WeixinUserDto 相关实现
    
    
    创建标识：Senparc - 20250712
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto
{
    public class UserTag_WeixinUserDto
    {
        public int UserTagId { get; private set; }
        public UserTag_CreateOrUpdateDto UserTag { get; private set; }

        public int WeixinUserId { get; private set; }
        public WeixinUserDto WeixinUser { get; private set; }

        private UserTag_WeixinUserDto() { }
    }
}

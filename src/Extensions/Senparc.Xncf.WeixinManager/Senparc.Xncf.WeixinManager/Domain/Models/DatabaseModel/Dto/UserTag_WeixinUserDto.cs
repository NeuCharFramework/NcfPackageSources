/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: UserTag_WeixinUserDto.cs
    File description: UserTag_WeixinUserDto related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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

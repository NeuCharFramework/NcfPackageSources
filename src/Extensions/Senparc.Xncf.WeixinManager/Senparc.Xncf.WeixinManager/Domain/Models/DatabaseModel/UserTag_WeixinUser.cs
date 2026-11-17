/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: UserTag_WeixinUser.cs

    File description: UserTag_WeixinUser related implementation

    

    

    Created by: Senparc - 20250712

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel
{
    //Legacy comment translated to English.

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>

    [Table(Register.DATABASE_PREFIX + nameof(UserTag_WeixinUser))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class UserTag_WeixinUser : EntityBase
    {
        public int UserTagId { get; private set; }
        public UserTag UserTag { get; private set; }

        public int WeixinUserId { get; private set; }
        public WeixinUser WeixinUser { get; private set; }

        private UserTag_WeixinUser() { }

        public UserTag_WeixinUser(int userTagId, int weixinUserId)
        {
            UserTagId = userTagId;
            WeixinUserId = weixinUserId;
        }

        protected void SetUpdateTime(DateTime? time = default)
        {
            if (AddTime == DateTime.MinValue)
            {
                AddTime = SystemTime.Now.LocalDateTime;
            }
            LastUpdateTime = time ?? SystemTime.Now.LocalDateTime;
        }

        public void UpdateTime()
        {
            SetUpdateTime(SystemTime.Now.UtcDateTime);
        }
    }
}

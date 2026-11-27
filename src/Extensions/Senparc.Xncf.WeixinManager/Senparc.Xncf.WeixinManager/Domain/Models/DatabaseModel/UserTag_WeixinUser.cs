/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: UserTag_WeixinUser.cs
    File description: UserTag_WeixinUser related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

    Modified by: Senparc - 20261127
    Change description: v0.25.1.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel
{
    /// <summary>
    /// Links a Weixin user to a persisted Weixin user tag.
    /// </summary>

    [Table(Register.DATABASE_PREFIX + nameof(UserTag_WeixinUser))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class UserTag_WeixinUser : EntityBase
    {
        /// <summary>Gets the linked user-tag identifier.</summary>
        public int UserTagId { get; private set; }
        /// <summary>Gets the linked user-tag entity.</summary>
        public UserTag UserTag { get; private set; }

        /// <summary>Gets the linked Weixin-user identifier.</summary>
        public int WeixinUserId { get; private set; }
        /// <summary>Gets the linked Weixin-user entity.</summary>
        public WeixinUser WeixinUser { get; private set; }

        /// <summary>Initializes an EF Core materialization-only instance.</summary>
        private UserTag_WeixinUser() { }

        /// <summary>Creates a user/tag link.</summary>
        /// <param name="userTagId">The tag identifier.</param>
        /// <param name="weixinUserId">The Weixin-user identifier.</param>
        public UserTag_WeixinUser(int userTagId, int weixinUserId)
        {
            UserTagId = userTagId;
            WeixinUserId = weixinUserId;
        }

        /// <summary>Sets creation and last-update timestamps.</summary>
        /// <param name="time">Optional explicit last-update time.</param>
        protected void SetUpdateTime(DateTime? time = default)
        {
            if (AddTime == DateTime.MinValue)
            {
                AddTime = SystemTime.Now.LocalDateTime;
            }
            LastUpdateTime = time ?? SystemTime.Now.LocalDateTime;
        }

        /// <summary>Updates the link's last-update timestamp using UTC time.</summary>
        public void UpdateTime()
        {
            SetUpdateTime(SystemTime.Now.UtcDateTime);
        }
    }
}

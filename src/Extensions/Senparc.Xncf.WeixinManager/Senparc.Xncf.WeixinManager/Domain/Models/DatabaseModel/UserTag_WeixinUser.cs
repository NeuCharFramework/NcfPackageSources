using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel
{
    //About EF many-to-many approach: https://www.entityframeworktutorial.net/efcore/configure-many-to-many-relationship-in-ef-core.aspx

    /// <summary>
    ///UserTag - WeixinUser many-to-many association table
    /// </summary>

    [Table(Register.DATABASE_PREFIX + nameof(UserTag_WeixinUser))]//The prefix must be added to prevent conflicts system-wide.
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

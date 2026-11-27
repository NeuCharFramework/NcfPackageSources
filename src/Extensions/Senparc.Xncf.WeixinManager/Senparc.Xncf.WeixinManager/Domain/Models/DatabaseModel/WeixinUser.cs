/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinUser.cs
    File description: WeixinUser related implementation

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
    /// Persists the Weixin public-account user profile and subscription metadata.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(WeixinUser))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class WeixinUser : EntityBase<int>
    {
        public MpAccount MpAccount { get; private set; }
        public int MpAccountId { get; private set; }

        /// <summary>
        /// Gets the subscription state reported by Weixin.
        /// </summary>
        [Required]
        public int Subscribe { get; private set; }
        /// <summary>
        /// Gets the stable Weixin open identifier.
        /// </summary>
        [Required]
        public string OpenId { get; private set; }
        /// <summary>
        /// Gets the display nickname.
        /// </summary>
        [Required]
        public string NickName { get; private set; }
        /// <summary>
        /// Gets the numeric sex value reported by Weixin.
        /// </summary>
        [Required]
        public int Sex { get; private set; }
        /// <summary>
        /// Gets the preferred language reported by Weixin.
        /// </summary>
        public string Language { get; private set; }
        /// <summary>
        /// Gets the city reported by Weixin.
        /// </summary>
        public string City { get; private set; }
        /// <summary>
        /// Gets the province reported by Weixin.
        /// </summary>
        public string Province { get; private set; }
        /// <summary>
        /// Gets the country reported by Weixin.
        /// </summary>
        public string Country { get; private set; }
        /// <summary>
        /// Gets the avatar URL reported by Weixin.
        /// </summary>
        public string HeadImgUrl { get; private set; }
        /// <summary>
        /// Gets the Weixin subscription timestamp.
        /// </summary>
        public long Subscribe_Time { get; private set; }
        /// <summary>
        /// Gets the Weixin union identifier.
        /// </summary>
        public string UnionId { get; private set; }
        /// <summary>
        /// Gets the administrator remark.
        /// </summary>
        public string Remark { get; private set; }
        /// <summary>
        /// Gets the Weixin group identifier.
        /// </summary>
        public int Groupid { get; private set; }

        /// <summary>
        /// Gets the subscription source.
        /// </summary>
        public string Subscribe_Scene { get; private set; }
        /// <summary>
        /// Gets the numeric QR-scene identifier.
        /// </summary>
        public uint Qr_Scene { get; private set; }
        /// <summary>
        /// Gets the textual QR-scene value.
        /// </summary>
        public string Qr_Scene_Str { get; private set; }

        /// <summary>
        /// Reserved for EF Core materialization.
        /// </summary>
        private WeixinUser()
        {
        }

        //public WeixinUser(int mpAccountId, int subscribe, string openId, string nickName, int sex, string language, string city, string province, string country, string headImgUrl, long subscribe_Time, string unionId, string remark, int groupid, string subscribe_Scene, uint qr_Scene, string qr_Scene_Str)
        //{
        //    MpAccountId = mpAccountId;
        //    Subscribe = subscribe;
        //    OpenId = openId;
        //    NickName = nickName;
        //    Sex = sex;
        //    Language = language;
        //    City = city;
        //    Province = province;
        //    Country = country;
        //    HeadImgUrl = headImgUrl;
        //    Subscribe_Time = subscribe_Time;
        //    UnionId = unionId;
        //    Remark = remark;
        //    Groupid = groupid;
        //    Subscribe_Scene = subscribe_Scene;
        //    Qr_Scene = qr_Scene;
        //    Qr_Scene_Str = qr_Scene_Str;
        //}

        /// <summary>
        /// Gets the user-tag links associated with this Weixin user.
        /// </summary>
        public IList<UserTag_WeixinUser> UserTags_WeixinUsers { get; private set; } = new List<UserTag_WeixinUser>();


        /// <summary>Updates the entity's last-update timestamp.</summary>
        public void UpdateTime()
        {
            SetUpdateTime();
        }
    }
}

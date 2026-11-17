/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinUser.cs
    File description: WeixinUser related implementation

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
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(WeixinUser))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class WeixinUser : EntityBase<int>
    {
        public MpAccount MpAccount { get; private set; }
        public int MpAccountId { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int Subscribe { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string OpenId { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string NickName { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int Sex { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Language { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string City { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Province { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Country { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string HeadImgUrl { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long Subscribe_Time { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string UnionId { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Remark { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int Groupid { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Subscribe_Scene { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public uint Qr_Scene { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Qr_Scene_Str { get; private set; }

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
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public IList<UserTag_WeixinUser> UserTags_WeixinUsers { get; private set; } = new List<UserTag_WeixinUser>();


        public void UpdateTime()
        {
            SetUpdateTime();
        }
    }
}

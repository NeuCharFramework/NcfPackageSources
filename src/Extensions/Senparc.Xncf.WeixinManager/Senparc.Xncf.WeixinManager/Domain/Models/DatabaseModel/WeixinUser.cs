using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel
{
    /// <summary>
    /// WeChat user
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(WeixinUser))]//The prefix must be added to prevent conflicts system-wide.
    [Serializable]
    public class WeixinUser : EntityBase<int>
    {
        public MpAccount MpAccount { get; private set; }
        public int MpAccountId { get; private set; }

        /// <summary>
        /// Whether the user subscribes to the official account. When the value is 0, it means that the user does not follow the official account and cannot obtain other information.
        /// </summary>
        [Required]
        public int Subscribe { get; private set; }
        /// <summary>
        /// User’s identification, unique to the current official account
        /// </summary>
        [Required]
        public string OpenId { get; private set; }
        /// <summary>
        /// user's nickname
        /// </summary>
        [Required]
        public string NickName { get; private set; }
        /// <summary>
        /// The gender of the user, when the value is 1, it is male, when the value is 2, it is female, when the value is 0, it is unknown
        /// </summary>
        [Required]
        public int Sex { get; private set; }
        /// <summary>
        ///User's language, simplified Chinese is zh_CN
        /// </summary>
        public string Language { get; private set; }
        /// <summary>
        ///User's city
        /// </summary>
        public string City { get; private set; }
        /// <summary>
        ///Province where the user is located
        /// </summary>
        public string Province { get; private set; }
        /// <summary>
        ///user's country
        /// </summary>
        public string Country { get; private set; }
        /// <summary>
        /// User avatar, the last value represents the size of the square avatar (there are 0, 46, 64, 96, and 132 values ​​​​available, 0 represents a 640*640 square avatar), this item is empty when the user does not have an avatar. If the user changes their avatar, the original avatar URL will be invalid.
        /// </summary>
        public string HeadImgUrl { get; private set; }
        /// <summary>
        /// The time the user paid attention to is the timestamp. If the user has followed multiple times, the last follow time will be used
        /// </summary>
        public long Subscribe_Time { get; private set; }
        /// <summary>
        /// This field will only appear after the user binds the official account to the WeChat Open Platform account.
        /// </summary>
        public string UnionId { get; private set; }
        /// <summary>
        /// Official account operators’ notes to fans. Official account operators can add notes to fans on the WeChat public platform user management interface.
        /// </summary>
        public string Remark { get; private set; }
        /// <summary>
        /// The group ID of the user (compatible with the old user group interface)
        /// </summary>
        public int Groupid { get; private set; }

        /// <summary>
        /// Return to the channel source that the user is concerned about, ADD_SCENE_SEARCH public account search, ADD_SCENE_ACCOUNT_MIGRATION public account migration, ADD_SCENE_PROFILE_CARD business card sharing, ADD_SCENE_QR_CODE scan QR code, ADD_SCENEPROFILE LINK click on the name in the image and text page, ADD_SCENE_PROFILE_ITEM Menu in the upper right corner of the graphic page, ADD_SCENE_PAID, follow after payment, ADD_SCENE_OTHERS others
        /// </summary>
        public string Subscribe_Scene { get; private set; }
        /// <summary>
        /// QR code scanning scenario (developer customized)
        /// </summary>
        public uint Qr_Scene { get; private set; }
        /// <summary>
        /// QR code scanning scene description (developer-defined)
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
        ///user tag
        /// <para>Corresponding to WeChat API tagid_list attribute</para>
        /// </summary>
        public IList<UserTag_WeixinUser> UserTags_WeixinUsers { get; private set; } = new List<UserTag_WeixinUser>();


        public void UpdateTime()
        {
            SetUpdateTime();
        }
    }
}

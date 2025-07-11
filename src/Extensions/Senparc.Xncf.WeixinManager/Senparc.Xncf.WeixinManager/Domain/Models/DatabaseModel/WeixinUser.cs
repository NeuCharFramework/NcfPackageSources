using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel
{
    /// <summary>
    /// 微信用户
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(WeixinUser))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class WeixinUser : EntityBase<int>
    {
        public MpAccount MpAccount { get; private set; }
        public int MpAccountId { get; private set; }

        /// <summary>
        /// 用户是否订阅该公众号标识，值为0时，代表此用户没有关注该公众号，拉取不到其余信息。
        /// </summary>
        [Required]
        public int Subscribe { get; private set; }
        /// <summary>
        /// 用户的标识，对当前公众号唯一
        /// </summary>
        [Required]
        public string OpenId { get; private set; }
        /// <summary>
        /// 用户的昵称
        /// </summary>
        [Required]
        public string NickName { get; private set; }
        /// <summary>
        /// 用户的性别，值为1时是男性，值为2时是女性，值为0时是未知
        /// </summary>
        [Required]
        public int Sex { get; private set; }
        /// <summary>
        ///用户的语言，简体中文为zh_CN
        /// </summary>
        public string Language { get; private set; }
        /// <summary>
        /// 用户所在城市
        /// </summary>
        public string City { get; private set; }
        /// <summary>
        /// 用户所在省份
        /// </summary>
        public string Province { get; private set; }
        /// <summary>
        /// 用户所在国家
        /// </summary>
        public string Country { get; private set; }
        /// <summary>
        /// 用户头像，最后一个数值代表正方形头像大小（有0、46、64、96、132数值可选，0代表640*640正方形头像），用户没有头像时该项为空。若用户更换头像，原有头像URL将失效。
        /// </summary>
        public string HeadImgUrl { get; private set; }
        /// <summary>
        /// 用户关注时间，为时间戳。如果用户曾多次关注，则取最后关注时间
        /// </summary>
        public long Subscribe_Time { get; private set; }
        /// <summary>
        /// 只有在用户将公众号绑定到微信开放平台帐号后，才会出现该字段。
        /// </summary>
        public string UnionId { get; private set; }
        /// <summary>
        /// 公众号运营者对粉丝的备注，公众号运营者可在微信公众平台用户管理界面对粉丝添加备注
        /// </summary>
        public string Remark { get; private set; }
        /// <summary>
        /// 用户所在的分组ID（兼容旧的用户分组接口）
        /// </summary>
        public int Groupid { get; private set; }

        /// <summary>
        /// 返回用户关注的渠道来源，ADD_SCENE_SEARCH 公众号搜索，ADD_SCENE_ACCOUNT_MIGRATION 公众号迁移，ADD_SCENE_PROFILE_CARD 名片分享，ADD_SCENE_QR_CODE 扫描二维码，ADD_SCENEPROFILE LINK 图文页内名称点击，ADD_SCENE_PROFILE_ITEM 图文页右上角菜单，ADD_SCENE_PAID 支付后关注，ADD_SCENE_OTHERS 其他
        /// </summary>
        public string Subscribe_Scene { get; private set; }
        /// <summary>
        /// 二维码扫码场景（开发者自定义）
        /// </summary>
        public uint Qr_Scene { get; private set; }
        /// <summary>
        /// 二维码扫码场景描述（开发者自定义）
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
        /// 用户标签
        /// <para>对应微信API tagid_list 属性</para>
        /// </summary>
        public IList<UserTag_WeixinUser> UserTags_WeixinUsers { get; private set; } = new List<UserTag_WeixinUser>();


        public void UpdateTime()
        {
            SetUpdateTime();
        }
    }
}

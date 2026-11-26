/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinUserDto.cs
    File description: WeixinUserDto related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using AutoMapper;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto
{
    /// <summary>Transfers Weixin public-account user profile data to application/API boundaries.</summary>
    public class WeixinUserDto : DtoBase
    {
        /// <summary>Gets or sets the persisted user identifier.</summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the subscription state reported by Weixin.
        /// </summary>
        [Required]
        public int Subscribe { get; set; }
        /// <summary>
        /// Gets or sets the stable Weixin open identifier.
        /// </summary>
        [Required]
        public string OpenId { get; set; }
        /// <summary>
        /// Gets or sets the display nickname.
        /// </summary>
        [Required]
        public string NickName { get; set; }
        /// <summary>
        /// Gets or sets the numeric sex value.
        /// </summary>
        [Required]
        public int Sex { get; set; }
        /// <summary>
        /// Gets or sets the preferred language.
        /// </summary>
        public string Language { get; set; }
        /// <summary>
        /// Gets or sets the city.
        /// </summary>
        public string City { get; set; }
        /// <summary>
        /// Gets or sets the province.
        /// </summary>
        public string Province { get; set; }
        /// <summary>
        /// Gets or sets the country.
        /// </summary>
        public string Country { get; set; }
        /// <summary>
        /// Gets or sets the avatar URL.
        /// </summary>
        public string HeadImgUrl { get; set; }
        /// <summary>
        /// Gets or sets the subscription timestamp.
        /// </summary>
        public long Subscribe_Time { get; set; }
        /// <summary>
        /// Gets or sets the Weixin union identifier.
        /// </summary>
        public string UnionId { get; set; }
        /// <summary>
        /// Gets or sets the administrator remark.
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// Gets or sets the Weixin group identifier.
        /// </summary>
        public int Groupid { get; set; }
        ///// <summary>
        /// Gets or sets the subscription source.
        ///// </summary>
        //public int[] Tagid_List { get; set; }
        /// <summary>
        /// Gets or sets the numeric QR-scene identifier.
        /// </summary>
        public string Subscribe_Scene { get; set; }
        /// <summary>
        /// Gets or sets the textual QR-scene value.
        /// </summary>
        public uint Qr_Scene { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Qr_Scene_Str { get; set; }

        /// <summary>Initializes an empty Weixin user DTO.</summary>
        public WeixinUserDto() { }

        public MpAccount MpAccount { get; set; }
        public int MpAccountId { get; set; }

        public IList<UserTag_WeixinUserDto> UserTags_WeixinUsers { get; private set; } = new List<UserTag_WeixinUserDto>();

    }


    /// <summary>Contains Weixin API fields used to update a stored user profile.</summary>
    public class WeixinUser_UpdateFromApiDto
    {
        /// <summary>
        /// Gets or sets the subscription state.
        /// </summary>
        [Required]
        public int Subscribe { get; set; }
        /// <summary>
        /// Gets or sets the open identifier.
        /// </summary>
        [Required]
        public string OpenId { get; set; }
        /// <summary>
        /// Gets or sets the nickname.
        /// </summary>
        [Required]
        public string NickName { get; set; }
        /// <summary>
        /// Gets or sets the numeric sex value.
        /// </summary>
        [Required]
        public int Sex { get; set; }
        /// <summary>
        /// Gets or sets the preferred language.
        /// </summary>
        public string Language { get; set; }
        /// <summary>
        /// Gets or sets the city.
        /// </summary>
        public string City { get; set; }
        /// <summary>
        /// Gets or sets the province.
        /// </summary>
        public string Province { get; set; }
        /// <summary>
        /// Gets or sets the country.
        /// </summary>
        public string Country { get; set; }
        /// <summary>
        /// Gets or sets the avatar URL.
        /// </summary>
        public string HeadImgUrl { get; set; }
        /// <summary>
        /// Gets or sets the subscription timestamp.
        /// </summary>
        public long Subscribe_Time { get; set; }
        /// <summary>
        /// Gets or sets the union identifier.
        /// </summary>
        public string UnionId { get; set; }
        /// <summary>
        /// Gets or sets the remark.
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// Gets or sets the group identifier.
        /// </summary>
        public int Groupid { get; set; }
        /// <summary>
        /// Gets or sets the Weixin tag identifiers.
        /// </summary>
        public int[] Tagid_List { get; set; } = new int[] { };
        /// <summary>
        /// Gets or sets the subscription source.
        /// </summary>
        public string Subscribe_Scene { get; set; }
        /// <summary>
        /// Gets or sets the QR-scene identifier.
        /// </summary>
        public uint Qr_Scene { get; set; }
        /// <summary>
        /// Gets or sets the QR-scene text.
        /// </summary>
        public string Qr_Scene_Str { get; set; }


        public WeixinUser_UpdateFromApiDto()
        {
        }

        //public MpAccount MpAccount { get; set; }

        public int MpAccountId { get; set; }
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinUserDto.cs
    File description: WeixinUserDto related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using AutoMapper;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto
{
    public class WeixinUserDto : DtoBase
    {
        public int Id { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int Subscribe { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string OpenId { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string NickName { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int Sex { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Language { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string City { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Province { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Country { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string HeadImgUrl { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long Subscribe_Time { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string UnionId { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int Groupid { get; set; }
        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        //public int[] Tagid_List { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Subscribe_Scene { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public uint Qr_Scene { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Qr_Scene_Str { get; set; }

        public WeixinUserDto() { }

        public MpAccount MpAccount { get; set; }
        public int MpAccountId { get; set; }

        public IList<UserTag_WeixinUserDto> UserTags_WeixinUsers { get; private set; } = new List<UserTag_WeixinUserDto>();

    }


    public class WeixinUser_UpdateFromApiDto
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int Subscribe { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string OpenId { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string NickName { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public int Sex { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Language { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string City { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Province { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Country { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string HeadImgUrl { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long Subscribe_Time { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string UnionId { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int Groupid { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int[] Tagid_List { get; set; } = new int[] { };
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Subscribe_Scene { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public uint Qr_Scene { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Qr_Scene_Str { get; set; }


        public WeixinUser_UpdateFromApiDto()
        {
        }

        //public MpAccount MpAccount { get; set; }

        public int MpAccountId { get; set; }
    }
}

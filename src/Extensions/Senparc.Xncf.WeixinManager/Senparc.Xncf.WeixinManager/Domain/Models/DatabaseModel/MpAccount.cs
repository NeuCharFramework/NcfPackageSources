/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: MpAccount.cs
    File description: MpAccount related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

    Modified by: Senparc - 20261127
    Change description: v0.25.1.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using AutoMapper;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel
{
    /// <summary>
    /// Stores one Weixin public-account configuration and its PromptRange binding.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(MpAccount))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class MpAccount : EntityBase<int>
    {
        /// <summary>Gets the account logo URL.</summary>
        [MaxLength(200)]
        public string Logo { get; private set; }
        /// <summary>Gets the administrator-facing account name.</summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; private set; }
        [Required]
        [MaxLength(100)]
        public string AppId { get; private set; }
        [Required]
        [MaxLength(100)]
        public string AppSecret { get; private set; }
        [Required]
        [MaxLength(500)]
        public string Token { get; private set; }
        [MaxLength(500)]
        public string EncodingAESKey { get; private set; }
        [MaxLength(100)]
        public string PromptRangeCode { get; private set; }

        /// <summary>
        /// Reserved for EF Core materialization.
        /// </summary>
        private MpAccount() { }

        /// <summary>
        /// Initializes an account from the administrative DTO.
        /// </summary>
        /// <param name="dto">The account values to persist.</param>
        public MpAccount(MpAccountDto dto)
        {
            Logo = dto.Logo;
            Name = dto.Name;
            AppId = dto.AppId;
            AppSecret = dto.AppSecret;
            Token = dto.Token;
            EncodingAESKey = dto.EncodingAESKey;
            AdminRemark = dto.AdminRemark;
            Remark = dto.Remark;
            PromptRangeCode = dto.PromptRangeCode;
            SetUpdateTime(SystemTime.Now.UtcDateTime);
        }

        /// <summary>Gets the users associated with the account.</summary>
        public IList<WeixinUser> WeixinUsers { get; private set; }
        /// <summary>Gets the tags associated with the account.</summary>
        public IList<UserTag> UserTags { get; private set; }
    }
}
        /// <summary>Gets the Weixin application ID.</summary>
        /// <summary>Gets the Weixin application secret.</summary>
        /// <summary>Gets the message verification token.</summary>
        /// <summary>Gets the message encryption key.</summary>
        /// <summary>Gets the optional PromptRange code used for AI replies.</summary>

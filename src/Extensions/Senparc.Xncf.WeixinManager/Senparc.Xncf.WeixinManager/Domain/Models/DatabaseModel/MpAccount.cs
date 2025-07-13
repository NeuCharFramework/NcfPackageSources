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
    /// 微信公众号信息
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(MpAccount))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class MpAccount : EntityBase<int>
    {
        [MaxLength(200)]
        public string Logo { get; private set; }
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

        private MpAccount() { }

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

        public IList<WeixinUser> WeixinUsers { get; private set; }
        public IList<UserTag> UserTags { get; private set; }
    }
}

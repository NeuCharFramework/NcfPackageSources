/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: UserTag.cs
    File description: UserTag related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel
{
    [Table(Register.DATABASE_PREFIX + nameof(UserTag))]//a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class UserTag : EntityBase<int>
    {
        public int MpAccountId { get; private set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int TagId { get; private set; }
        [Required]
        public string Name { get; private set; }
        public int Count { get; private set; }

        private UserTag() { }

        //public UserTag(int mpAccountId, int tagId, string name, int count)
        //{
        //    MpAccountId = mpAccountId;
        //    TagId = tagId;
        //    Name = name;
        //    Count = count;
        //}

        public MpAccount MpAccount { get; private set; }

        public IList<UserTag_WeixinUser> UserTags_WeixinUsers { get; private set; }

        public bool Update(UserTag_CreateOrUpdateDto dto)
        {
            var changed = false;
            if (dto.Name != Name)
            {
                Name = dto.Name;
                changed = true;
            }
            if (dto.Count != Count)
            {
                Count = dto.Count;
                changed = true;
            }

            if (changed)
            {
                SetUpdateTime();
            }

            return changed;
        }
    }
}

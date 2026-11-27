/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: UserTag.cs
    File description: UserTag related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

    Modified by: Senparc - 20261127
    Change description: v0.25.1.2 Improve source documentation, headers, and module metadata consistency.

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
        /// <summary>Gets the owning public-account identifier.</summary>
        public int MpAccountId { get; private set; }
        /// <summary>
        /// Gets the provider tag identifier.
        /// </summary>
        public int TagId { get; private set; }
        /// <summary>Gets the tag display name.</summary>
        [Required]
        public string Name { get; private set; }
        /// <summary>Gets the number of users assigned to the tag.</summary>
        public int Count { get; private set; }

        /// <summary>
        /// Reserved for EF Core materialization.
        /// </summary>
        private UserTag() { }

        //public UserTag(int mpAccountId, int tagId, string name, int count)
        //{
        //    MpAccountId = mpAccountId;
        //    TagId = tagId;
        //    Name = name;
        //    Count = count;
        //}

        /// <summary>Gets the owning public-account entity.</summary>
        public MpAccount MpAccount { get; private set; }

        /// <summary>Gets the user relationships associated with this tag.</summary>
        public IList<UserTag_WeixinUser> UserTags_WeixinUsers { get; private set; }

        /// <summary>
        /// Applies editable tag values and updates the entity timestamp when a value changes.
        /// </summary>
        /// <param name="dto">The replacement tag values.</param>
        /// <returns><see langword="true"/> when at least one value changed.</returns>
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

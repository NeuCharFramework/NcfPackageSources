/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FeedBack.cs
    File description: FeedBack related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.15.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.15.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.SystemManager.Domain.DatabaseModel
{
    /// <summary>
    /// Stores feedback submitted by an administrator or account.
    /// </summary>
    [Table("FeedBacks")]
    public class FeedBack : EntityBase<int>
    {
        /// Gets or sets the submitting account identifier.
        public int AccountId { get; set; }

        /// Gets or sets the feedback content.
        public string Content { get; set; }

        ///// <summary>
        ///// user
        ///// </summary>
        ///// <value></value>
        //public Account Account { get; set; }
    }
}

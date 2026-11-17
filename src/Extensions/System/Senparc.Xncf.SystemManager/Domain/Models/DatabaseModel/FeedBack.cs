/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: FeedBack.cs

    File description: FeedBack related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.SystemManager.Domain.DatabaseModel
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Table("FeedBacks")]
    public class FeedBack : EntityBase<int>
    {
        public int AccountId { get; set; }

        public string Content { get; set; }

        ///// <summary>
        ///// user
        ///// </summary>
        ///// <value></value>
        //public Account Account { get; set; }
    }
}
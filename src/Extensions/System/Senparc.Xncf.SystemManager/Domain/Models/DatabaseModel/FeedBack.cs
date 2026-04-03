using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.SystemManager.Domain.DatabaseModel
{
    /// <summary>
    ///Feedback
    /// </summary>
    [Table("FeedBacks")]
    public class FeedBack : EntityBase<int>
    {
        public int AccountId { get; set; }

        public string Content { get; set; }

        ///// <summary>
        ///// User
        ///// </summary>
        ///// <value></value>
        //public Account Account { get; set; }
    }
}
/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FeedBack.cs
    文件功能描述：FeedBack 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.SystemManager.Domain.DatabaseModel
{
    /// <summary>
    /// 意见反馈
    /// </summary>
    [Table("FeedBacks")]
    public class FeedBack : EntityBase<int>
    {
        public int AccountId { get; set; }

        public string Content { get; set; }

        ///// <summary>
        ///// 用户
        ///// </summary>
        ///// <value></value>
        //public Account Account { get; set; }
    }
}
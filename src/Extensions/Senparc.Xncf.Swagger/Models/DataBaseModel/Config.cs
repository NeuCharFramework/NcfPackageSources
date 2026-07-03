/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Config.cs
    文件功能描述：Config 相关实现
    
    
    创建标识：Senparc - 20210724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Senparc.Xncf.Swagger.Models.DataBaseModel
{
    /// <summary>
    /// 配置
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(Config))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class Config : EntityBase<int>
    {
        /// <summary>
        /// 使用目录筛选
        /// </summary>
        [DefaultValue(true)]
        public bool UseCategoryFilter { get; set; }
        /// <summary>
        /// 启用
        /// </summary>
        [DefaultValue(true)]
        public bool Enabled { get; set; }
        /// <summary>
        /// 允许访问的用户分组，留空则不做判断
        /// </summary>
        public string AllowUserRoles { get; set; }
    }
}

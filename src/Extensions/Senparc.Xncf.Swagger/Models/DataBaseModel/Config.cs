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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.Models
{
    /// <summary>
    /// 数据库结构元数据
    /// 用于存储模块-表-字段的映射关系
    /// </summary>
    public class DatabaseSchemaMetadata
    {
        /// <summary>
        /// 模块名称
        /// 例如: Senparc.Xncf.AgentsManager
        /// </summary>
        [Required]
        public string ModuleName { get; set; }

        /// <summary>
        /// 表名称
        /// </summary>
        [Required]
        public string TableName { get; set; }

        /// <summary>
        /// 表的完全限定名（C# 类型全名）
        /// 例如: Senparc.Xncf.AgentsManager.Models.DatabaseModel.AgentTemplate
        /// </summary>
        [Required]
        public string EntityFullName { get; set; }

        /// <summary>
        /// 表的中文描述
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 表的详描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 字段列表
        /// </summary>
        public List<DatabaseColumnMetadata> Columns { get; set; } = new List<DatabaseColumnMetadata>();

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否可见/可查询
        /// 用于权限控制
        /// </summary>
        public bool IsVisible { get; set; } = true;
    }

    /// <summary>
    /// 数据库字段元数据
    /// </summary>
    public class DatabaseColumnMetadata
    {
        /// <summary>
        /// 字段名称
        /// </summary>
        [Required]
        public string ColumnName { get; set; }

        /// <summary>
        /// 字段类型
        /// 例如: string, int, DateTime
        /// </summary>
        [Required]
        public string ColumnType { get; set; }

        /// <summary>
        /// 字段中文名称/描述
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 字段描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 是否为主键
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// 是否为必需字段
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 最大长度（字符串字段）
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// 是否可用于查询过滤
        /// </summary>
        public bool IsFilterable { get; set; } = true;

        /// <summary>
        /// 是否可见
        /// 用于确定是否在查询时返回此字段
        /// </summary>
        public bool IsVisible { get; set; } = true;
    }

    /// <summary>
    /// 模型-表映射信息的 DTO
    /// 用于通过 API 传输
    /// </summary>
    public class DatabaseSchemaDto
    {
        public string ModuleName { get; set; }
        public string TableName { get; set; }
        public string EntityFullName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public List<DatabaseColumnDto> Columns { get; set; } = new List<DatabaseColumnDto>();
        public bool IsVisible { get; set; }
    }

    /// <summary>
    /// 数据库字段信息 DTO
    /// </summary>
    public class DatabaseColumnDto
    {
        public string ColumnName { get; set; }
        public string ColumnType { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsRequired { get; set; }
        public int? MaxLength { get; set; }
        public bool IsFilterable { get; set; }
        public bool IsVisible { get; set; }
    }

    /// <summary>
    /// 查询条件元数据
    /// 用于指定如何查询某个表
    /// </summary>
    public class QueryConditionMetadata
    {
        /// <summary>
        /// 模块名称
        /// </summary>
        [Required]
        public string ModuleName { get; set; }

        /// <summary>
        /// 表名称
        /// </summary>
        [Required]
        public string TableName { get; set; }

        /// <summary>
        /// 过滤条件 JSON
        /// 用于构建 Where 条件
        /// 例如: { "Id": 1 } 或 { "Name": "test" }
        /// </summary>
        public string FilterJson { get; set; }

        /// <summary>
        /// 排序字段
        /// </summary>
        public string OrderByField { get; set; }

        /// <summary>
        /// 是否降序
        /// </summary>
        public bool IsDescending { get; set; } = false;

        /// <summary>
        /// 页码（从 0 开始）
        /// </summary>
        public int PageIndex { get; set; } = 0;

        /// <summary>
        /// 每页数量
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 返回的字段列表
        /// 如果为空，则返回所有可见字段
        /// </summary>
        public List<string> SelectColumns { get; set; } = new List<string>();
    }
}

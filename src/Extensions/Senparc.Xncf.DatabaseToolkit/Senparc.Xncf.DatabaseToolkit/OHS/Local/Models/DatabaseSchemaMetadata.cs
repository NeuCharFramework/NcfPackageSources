using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.Models
{
    /// <summary>
    /// Database structure metadata
    /// Used to store module-table-field mapping relationship
    /// </summary>
    public class DatabaseSchemaMetadata
    {
        /// <summary>
        /// module name
        ///Example: Senparc.Xncf.AgentsManager
        /// </summary>
        [Required]
        public string ModuleName { get; set; }

        /// <summary>
        /// table name
        /// </summary>
        [Required]
        public string TableName { get; set; }

        /// <summary>
        /// Fully qualified name of the table (full C# type name)
        /// For example: Senparc.Xncf.AgentsManager.Models.DatabaseModel.AgentTemplate
        /// </summary>
        [Required]
        public string EntityFullName { get; set; }

        /// <summary>
        /// Chinese description of the table
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Detailed description of the table
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// field list
        /// </summary>
        public List<DatabaseColumnMetadata> Columns { get; set; } = new List<DatabaseColumnMetadata>();

        /// <summary>
        ///Creation time
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Whether it is visible/queryable
        /// used for permission control
        /// </summary>
        public bool IsVisible { get; set; } = true;
    }

    /// <summary>
    /// Database field metadata
    /// </summary>
    public class DatabaseColumnMetadata
    {
        /// <summary>
        ///Field name
        /// </summary>
        [Required]
        public string ColumnName { get; set; }

        /// <summary>
        ///Field type
        ///Example: string, int, DateTime
        /// </summary>
        [Required]
        public string ColumnType { get; set; }

        /// <summary>
        ///Field Chinese name/description
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        ///Field description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Whether it is the primary key
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Is it a required field?
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Maximum length (string field)
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Whether it can be used for query filtering
        /// </summary>
        public bool IsFilterable { get; set; } = true;

        /// <summary>
        /// is visible
        /// Used to determine whether to return this field when querying
        /// </summary>
        public bool IsVisible { get; set; } = true;
    }

    /// <summary>
    /// DTO for model-table mapping information
    /// for transfer via API
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
    /// Database field information DTO
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
    /// Query condition metadata
    /// is used to specify how to query a table
    /// </summary>
    public class QueryConditionMetadata
    {
        /// <summary>
        /// module name
        /// </summary>
        [Required]
        public string ModuleName { get; set; }

        /// <summary>
        /// table name
        /// </summary>
        [Required]
        public string TableName { get; set; }

        /// <summary>
        /// filter condition JSON
        /// is used to build Where conditions
        /// For example: { "Id": 1 } or { "Name": "test" }
        /// </summary>
        public string FilterJson { get; set; }

        /// <summary>
        /// sort field
        /// </summary>
        public string OrderByField { get; set; }

        /// <summary>
        /// Whether to descend
        /// </summary>
        public bool IsDescending { get; set; } = false;

        /// <summary>
        ///Page number (starting from 0)
        /// </summary>
        public int PageIndex { get; set; } = 0;

        /// <summary>
        ///number per page
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Returned field list
        /// If empty, returns all visible fields
        /// </summary>
        public List<string> SelectColumns { get; set; } = new List<string>();
    }
}

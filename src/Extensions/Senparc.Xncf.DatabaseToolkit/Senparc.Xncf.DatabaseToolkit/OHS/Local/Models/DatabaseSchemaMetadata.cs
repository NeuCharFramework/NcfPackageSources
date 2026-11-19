/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: DatabaseSchemaMetadata.cs
    File description: DatabaseSchemaMetadata related implementation

    Created by: Senparc - 20260327

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.27.13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.DatabaseToolkit.OHS.Local.Models
{
    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public class DatabaseSchemaMetadata
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// for example: Senparc.Xncf.AgentsManager
        /// </summary>
        [Required]
        public string ModuleName { get; set; }

        /// <summary>
        /// tablename
        /// </summary>
        [Required]
        public string TableName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// for example: Senparc.Xncf.AgentsManager.Models.DatabaseModel.AgentTemplate
        /// </summary>
        [Required]
        public string EntityFullName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public List<DatabaseColumnMetadata> Columns { get; set; } = new List<DatabaseColumnMetadata>();

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsVisible { get; set; } = true;
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class DatabaseColumnMetadata
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string ColumnName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// for example: string, int, DateTime
        /// </summary>
        [Required]
        public string ColumnType { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsFilterable { get; set; } = true;

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsVisible { get; set; } = true;
    }

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
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
    ///Legacy comment translated to English.
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
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public class QueryConditionMetadata
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public string ModuleName { get; set; }

        /// <summary>
        /// tablename
        /// </summary>
        [Required]
        public string TableName { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// for example: { "Id": 1 } or { "Name": "test" }
        /// </summary>
        public string FilterJson { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string OrderByField { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsDescending { get; set; } = false;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int PageIndex { get; set; } = 0;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public List<string> SelectColumns { get; set; } = new List<string>();
    }
}

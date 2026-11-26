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
    /// Describes one database table and the metadata needed to render it in
    /// DatabaseToolkit administration and query surfaces.
    /// </summary>
    public class DatabaseSchemaMetadata
    {
        /// <summary>
        /// Gets or sets the owning XNCF module name.
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
        /// Gets or sets the database table name.
        /// for example: Senparc.Xncf.AgentsManager.Models.DatabaseModel.AgentTemplate
        /// </summary>
        [Required]
        public string EntityFullName { get; set; }

        /// <summary>
        /// Gets or sets the fully qualified entity CLR type name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the localized/display table name.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the table description.
        /// </summary>
        public List<DatabaseColumnMetadata> Columns { get; set; } = new List<DatabaseColumnMetadata>();

        /// <summary>
        /// Gets or sets the discovered column metadata.
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets a value indicating whether the table is visible to query surfaces.
        /// </summary>
        public bool IsVisible { get; set; } = true;
    }

    /// <summary>
    /// Describes one database column exposed by DatabaseToolkit.
    /// </summary>
    public class DatabaseColumnMetadata
    {
        /// <summary>
        /// Gets or sets the database column name.
        /// </summary>
        [Required]
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets or sets the CLR/provider column type name.
        /// for example: string, int, DateTime
        /// </summary>
        [Required]
        public string ColumnType { get; set; }

        /// <summary>
        /// Gets or sets the localized/display column name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the column description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets whether the column is part of the primary key.
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Gets or sets whether the column is required.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets the maximum configured string length.
        /// </summary>
        public int? MaxLength { get; set; }

        /// <summary>
        /// Gets or sets whether the column may be used as a filter.
        /// </summary>
        public bool IsFilterable { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the table should be shown in the administration UI.
        /// </summary>
        public bool IsVisible { get; set; } = true;
    }

    /// <summary>
    /// DTO representing table metadata returned by DatabaseToolkit APIs.
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
    /// DTO representing column metadata returned by DatabaseToolkit APIs.
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
    /// Describes a table query condition submitted to DatabaseToolkit.
    /// </summary>
    public class QueryConditionMetadata
    {
        /// <summary>
        /// Gets or sets the owning XNCF module name.
        /// </summary>
        [Required]
        public string ModuleName { get; set; }

        /// <summary>
        /// tablename
        /// </summary>
        [Required]
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets a JSON object containing property-value filters.
        /// for example: { "Id": 1 } or { "Name": "test" }
        /// </summary>
        public string FilterJson { get; set; }

        /// <summary>
        /// Gets or sets the property used for ordering.
        /// </summary>
        public string OrderByField { get; set; }

        /// <summary>
        /// Gets or sets whether ordering is descending.
        /// </summary>
        public bool IsDescending { get; set; } = false;

        /// <summary>
        /// Gets or sets the zero-based page index.
        /// </summary>
        public int PageIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets the requested page size.
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Gets or sets the selected columns to return.
        /// </summary>
        public List<string> SelectColumns { get; set; } = new List<string>();
    }
}

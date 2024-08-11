using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DynamicData.Domain.Models.DatabaseModel.Dto
{
    public class ColumnMetadataDto : DtoBase<ColumnMetadata,int>
    {
        /// <summary>  
        /// 关联的表格ID。
        /// </summary>  
        //[ForeignKey(nameof(TableMetadata))]
        public int TableMetadataId { get;  set; }

        /// <summary>  
        /// 列名称。  
        /// </summary>  
        [Required]
        [MaxLength(255)]
        public string ColumnName { get;  set; }

        /// <summary>  
        /// 列的数据类型。  
        /// </summary>  
        [Required]
        [MaxLength(50)]
        public string ColumnType { get;  set; }

        /// <summary>  
        /// 是否允许NULL值。  
        /// </summary>  
        [Required]
        public bool IsNullable { get;  set; }

        /// <summary>  
        /// 列的默认值。  
        /// </summary>  
        public string DefaultValue { get;  set; }

        /// <summary>  
        /// 关联的表格元数据。  
        /// </summary>  
        //[InverseProperty(nameof(TableMetadata.ColumnMetadatas))]
        public TableMetadataDto TableMetadata { get; set; }

        /// <summary>  
        /// 关联的表格元数据。  
        /// </summary>  
        //[InverseProperty(nameof(TableData.ColumnMetadata))]
        public ICollection<TableDataDto> TableDatas { get; set; }

        private ColumnMetadataDto() { }
    }
}

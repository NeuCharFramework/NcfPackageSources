using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Senparc.AI;
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AIKernel.Models;
using Senparc.AI.Interfaces;

namespace Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto
{
    public class AIVectorDto : DtoBase
    {
        /// <summary>
        /// 主键 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 代号
        /// </summary>
        public string Alias { get; set; }


        /// <summary>
        /// 模型名称（必须）
        /// </summary>
        public string VectorId { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// 向量数据库的类型（必须）, 例如：Memory, HardDisk, Redis, Mulivs, Chroma, PostgreSQL, Sqlite, SqlServer, Default
        /// </summary>
        public VectorDBType VectorDBType { get; set; }

        /// <summary>
        /// Note（可选）
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// 是否共享
        /// </summary>
        public bool IsShared { get; set; } = false;


        /// <summary>
        /// 是否展示
        /// </summary>
        public bool Show { get; set; }

        public AIVectorDto()
        {
        }

        public AIVectorDto(AIVector aIVector)
        {
            Id = aIVector.Id;
            Alias = aIVector.Alias;
            VectorId = aIVector.VectorId;
            Name = aIVector.Name;
            ConnectionString = aIVector.ConnectionString;
            VectorDBType = aIVector.VectorDBType;
            Note = aIVector.Note;
            IsShared = aIVector.IsShared;
            Show = aIVector.Show;
            AddTime = aIVector.AddTime;
            LastUpdateTime = aIVector.LastUpdateTime;
            TenantId = aIVector.TenantId;
        }
    }
}
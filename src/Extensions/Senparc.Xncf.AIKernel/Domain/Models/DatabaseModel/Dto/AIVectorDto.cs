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
        /// primary key ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// code name
        /// </summary>
        public string Alias { get; set; }


        /// <summary>
        ///Model name (required)
        /// </summary>
        public string VectorId { get; set; }

        /// <summary>
        ///name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Type of vector database (required), for example: Memory, HardDisk, Redis, Mulivs, Chroma, PostgreSQL, Sqlite, SqlServer, Default
        /// </summary>
        public VectorDBType VectorDBType { get; set; }

        /// <summary>
        ///Note (optional)
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// Whether to share
        /// </summary>
        public bool IsShared { get; set; } = false;


        /// <summary>
        /// Whether to display
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
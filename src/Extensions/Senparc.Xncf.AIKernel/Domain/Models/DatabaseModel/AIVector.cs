using Senparc.Ncf.Core.Models;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Senparc.AI;
using Senparc.Xncf.AIKernel.OHS.Local.PL;
using Senparc.Xncf.AIKernel.Domain.Models;
using Senparc.AI.Interfaces;

namespace Senparc.Xncf.AIKernel.Models
{
    /// <summary>
    ///Vector database entity
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AIVector))] //The prefix must be added to prevent conflicts system-wide.
    [Serializable]
    public class AIVector : EntityBase<int>
    {
        /// <summary>
        /// code name
        /// </summary>
        [Required, MaxLength(50)]
        public string Alias { get; private set; }


        /// <summary>
        /// Vector database name (required)
        /// </summary>
        [Required, MaxLength(100)]
        public string VectorId { get; private set; }

        /// <summary>
        ///name
        /// </summary>
        [MaxLength(150)]
        public string Name { get; private set; }

        /// <summary>
        /// connection string
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// Type of vector database (required), for example: Memory, HardDisk, Redis, Mulivs, Chroma, PostgreSQL, Sqlite, SqlServer, Default
        /// </summary>
        [Required]
        public VectorDBType VectorDBType { get; private set; }

        /// <summary>
        ///Note (optional)
        /// </summary>
        public string Note { get; private set; }

        /// <summary>
        /// Whether to share
        /// </summary>
        [Required, DefaultValue(false)]
        public bool IsShared { get; private set; } = false;


        /// <summary>
        /// Whether to display
        /// </summary>
        [Required, DefaultValue(true)]
        public bool Show { get; private set; }


        public AIVector(string name, string connectionString, VectorDBType vectorDBType, string note, string alias, string vectorId)
        {
            Name = name;
            ConnectionString = connectionString;
            VectorDBType = vectorDBType;
            Note = note;
            Alias = alias;
            VectorId = vectorId;
            Show = true;
            IsShared = false;
        }

        public AIVector(AIVector_CreateOrEditRequest orEditRequest) : this(orEditRequest.Name, orEditRequest.ConnectionString,orEditRequest.VectorDBType, orEditRequest.Note, orEditRequest.Alias, orEditRequest.VectorId)
        {
        }

        public AIVector SwitchShow(bool show)
        {
            Show = show;
            return this;
        }

        public AIVector Update(AIVector_CreateOrEditRequest request)
        {
            VectorId = request.VectorId;
            Name = request.Name;
            ConnectionString = request.ConnectionString;
            VectorDBType = request.VectorDBType;
            Note = request.Note;
            Alias = request.Alias;
            VectorId = request.VectorId;
            IsShared = request.IsShared;
            SwitchShow(request.Show);
            return this;
        }
    }
}
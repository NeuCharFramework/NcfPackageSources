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
    /// 向量数据库实体
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AIVector))] //必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class AIVector : EntityBase<int>
    {
        /// <summary>
        /// 代号
        /// </summary>
        [Required, MaxLength(50)]
        public string Alias { get; private set; }


        /// <summary>
        /// 向量数据库名称（必须）
        /// </summary>
        [Required, MaxLength(100)]
        public string VectorId { get; private set; }

        /// <summary>
        /// 名称
        /// </summary>
        [MaxLength(150)]
        public string Name { get; private set; }

        /// <summary>
        /// 连接字符串
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// 向量数据库的类型（必须）, 例如：Memory, HardDisk, Redis, Mulivs, Chroma, PostgreSQL, Sqlite, SqlServer, Default
        /// </summary>
        [Required]
        public VectorDB.VectorDBType VectorDBType { get; private set; }

        /// <summary>
        /// Note（可选）
        /// </summary>
        public string Note { get; private set; }

        /// <summary>
        /// 是否共享
        /// </summary>
        [Required, DefaultValue(false)]
        public bool IsShared { get; private set; } = false;


        /// <summary>
        /// 是否展示
        /// </summary>
        [Required, DefaultValue(true)]
        public bool Show { get; private set; }


        public AIVector(string name, string connectionString, VectorDB.VectorDBType vectorDBType, string note, string alias, string vectorId)
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
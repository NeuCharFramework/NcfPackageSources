/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIVector.cs
    File description: AIVector related implementation

    Created by: Senparc - 20250402

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

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
    ///Legacy comment translated to English.
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(AIVector))] //a prefix is required to prevent system-wide conflicts
    [Serializable]
    public class AIVector : EntityBase<int>
    {
        /// <summary>
        /// code
        /// </summary>
        [Required, MaxLength(50)]
        public string Alias { get; private set; }


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required, MaxLength(100)]
        public string VectorId { get; private set; }

        /// <summary>
        /// name
        /// </summary>
        [MaxLength(150)]
        public string Name { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        [Required]
        public VectorDBType VectorDBType { get; private set; }

        /// <summary>
        /// Note (optional)
        /// </summary>
        public string Note { get; private set; }

        /// <summary>
        /// whether shared
        /// </summary>
        [Required, DefaultValue(false)]
        public bool IsShared { get; private set; } = false;


        /// <summary>
        /// whether displayed
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
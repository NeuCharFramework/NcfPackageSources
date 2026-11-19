/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIVectorDto.cs
    File description: AIVectorDto related implementation

    Created by: Senparc - 20250402

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

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
        /// code
        /// </summary>
        public string Alias { get; set; }


        /// <summary>
        /// model name (required)
        /// </summary>
        public string VectorId { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public VectorDBType VectorDBType { get; set; }

        /// <summary>
        /// Note (optional)
        /// </summary>
        public string Note { get; set; }

        /// <summary>
        /// whether shared
        /// </summary>
        public bool IsShared { get; set; } = false;


        /// <summary>
        /// whether displayed
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
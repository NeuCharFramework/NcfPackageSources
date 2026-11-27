/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIVectorDto.cs
    File description: AIVectorDto related implementation

    Created by: Senparc - 20250402

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

    Modified by: Senparc - 20261127
    Change description: v0.17.4.2 Improve source documentation, headers, and module metadata consistency.

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
        /// Gets or sets the vector provider connection string.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the vector database provider type.
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

        /// <summary>
        /// Initializes an empty vector DTO for binding.
        /// </summary>
        public AIVectorDto()
        {
        }

        /// <summary>
        /// Projects a vector database entity into a transport DTO.
        /// </summary>
        /// <param name="aIVector">The vector entity to project.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="aIVector"/> is null.</exception>
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

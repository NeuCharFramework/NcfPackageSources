using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>
    /// Empty class that implements the IEntityTypeConfiguration interface
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class BlankEntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
         where TEntity : class
    {
        /// <summary>
        ///Set the entity of TEntity
        /// </summary>
        /// <param name="builder">Builder used to set entity type (entity type)</param>
        public void Configure(EntityTypeBuilder<TEntity> builder)
        {
            
        }
    }
}

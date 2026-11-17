/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BlankEntityTypeConfiguration.cs
    File description: BlankEntityTypeConfiguration related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models.DataBaseModel
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class BlankEntityTypeConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
         where TEntity : class
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ///Legacy comment translated to English.
        public void Configure(EntityTypeBuilder<TEntity> builder)
        {
            
        }
    }
}

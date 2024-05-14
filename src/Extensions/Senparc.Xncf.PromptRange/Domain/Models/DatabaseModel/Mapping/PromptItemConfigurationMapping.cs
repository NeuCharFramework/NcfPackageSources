using System;using System.Collections.Generic;using System.Linq;using System.Text;using System.Threading.Tasks;using Microsoft.EntityFrameworkCore;using System.Xml;using Microsoft.EntityFrameworkCore.Metadata.Builders;using Senparc.Ncf.Core.Models.DataBaseModel;namespace Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.Mapping{    internal class PromptPromptItemConfigurationMapping : ConfigurationMappingWithIdBase<PromptItem, int>    {        public override void Configure(EntityTypeBuilder<PromptItem> builder)
        {
            builder.Property(e => e.EvalAvgScore)
                   .HasConversion<decimal>()
                   .HasColumnType("NUMERIC");

            builder.Property(e => e.EvalMaxScore)
                   .HasConversion<decimal>()
                   .HasColumnType("NUMERIC");


            base.Configure(builder);
        }    }}
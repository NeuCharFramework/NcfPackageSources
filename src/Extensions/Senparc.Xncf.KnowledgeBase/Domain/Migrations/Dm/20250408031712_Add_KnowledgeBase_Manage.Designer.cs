﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Senparc.Xncf.KnowledgeBase.Models;

#nullable disable

namespace Senparc.Xncf.KnowledgeBase.Domain.Migrations.Dm
{
    [DbContext(typeof(KnowledgeBaseSenparcEntities_Dm))]
    [Migration("20250408031712_Add_KnowledgeBase_Manage")]
    partial class Add_KnowledgeBase_Manage
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Dm:ValueGenerationStrategy", DmValueGenerationStrategy.IdentityColumn)
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            modelBuilder.Entity("Senparc.Xncf.KnowledgeBase.Color", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INT")
                        .HasAnnotation("Dm:ValueGenerationStrategy", DmValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<string>("AdditionNote")
                        .HasColumnType("NVARCHAR2(32767)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<int>("Blue")
                        .HasColumnType("INT");

                    b.Property<bool>("Flag")
                        .HasColumnType("BIT");

                    b.Property<int>("Green")
                        .HasColumnType("INT");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<int>("Red")
                        .HasColumnType("INT");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<int>("TenantId")
                        .HasColumnType("INT");

                    b.HasKey("Id");

                    b.ToTable("Senparc_KnowledgeBase_Color");
                });

            modelBuilder.Entity("Senparc.Xncf.KnowledgeBase.Models.DatabaseModel.KnowledgeBases", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("NVARCHAR2(450)");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<string>("ChatModelId")
                        .HasColumnType("NVARCHAR2(32767)");

                    b.Property<string>("EmbeddingModelId")
                        .HasColumnType("NVARCHAR2(32767)");

                    b.Property<bool>("Flag")
                        .HasColumnType("BIT");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<string>("Name")
                        .HasColumnType("NVARCHAR2(32767)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<int>("TenantId")
                        .HasColumnType("INT");

                    b.Property<string>("VectorDBId")
                        .HasColumnType("NVARCHAR2(32767)");

                    b.HasKey("Id");

                    b.ToTable("Senparc_KnowledgeBase_KnowledgeBases");
                });
#pragma warning restore 612, 618
        }
    }
}

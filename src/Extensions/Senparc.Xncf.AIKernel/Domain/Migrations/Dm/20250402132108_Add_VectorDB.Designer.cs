﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Senparc.Xncf.AIKernel.Models;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Dm
{
    [DbContext(typeof(AIKernelSenparcEntities_Dm))]
    [Migration("20250402132108_Add_VectorDB")]
    partial class Add_VectorDB
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Dm:ValueGenerationStrategy", DmValueGenerationStrategy.IdentityColumn)
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            modelBuilder.Entity("Senparc.Xncf.AIKernel.Models.AIModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INT")
                        .HasAnnotation("Dm:ValueGenerationStrategy", DmValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<int>("AiPlatform")
                        .HasColumnType("INT");

                    b.Property<string>("Alias")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("NVARCHAR2(50)");

                    b.Property<string>("ApiKey")
                        .HasMaxLength(200)
                        .HasColumnType("NVARCHAR2(200)");

                    b.Property<string>("ApiVersion")
                        .HasMaxLength(100)
                        .HasColumnType("NVARCHAR2(100)");

                    b.Property<int>("ConfigModelType")
                        .HasColumnType("INT");

                    b.Property<string>("DeploymentName")
                        .HasMaxLength(150)
                        .HasColumnType("NVARCHAR2(150)");

                    b.Property<string>("Endpoint")
                        .HasMaxLength(250)
                        .HasColumnType("NVARCHAR2(250)");

                    b.Property<bool>("Flag")
                        .HasColumnType("BIT");

                    b.Property<bool>("IsShared")
                        .HasColumnType("BIT");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<int>("MaxToken")
                        .HasColumnType("INT");

                    b.Property<string>("ModelId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("NVARCHAR2(100)");

                    b.Property<string>("Note")
                        .HasColumnType("NVARCHAR2(32767)");

                    b.Property<string>("OrganizationId")
                        .HasMaxLength(200)
                        .HasColumnType("NVARCHAR2(200)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<bool>("Show")
                        .HasColumnType("BIT");

                    b.Property<int>("TenantId")
                        .HasColumnType("INT");

                    b.HasKey("Id");

                    b.ToTable("Senparc_AIKernel_AIModel");
                });

            modelBuilder.Entity("Senparc.Xncf.AIKernel.Models.AIVector", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INT")
                        .HasAnnotation("Dm:ValueGenerationStrategy", DmValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<string>("Alias")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("NVARCHAR2(50)");

                    b.Property<string>("ConnectionString")
                        .HasColumnType("NVARCHAR2(32767)");

                    b.Property<bool>("Flag")
                        .HasColumnType("BIT");

                    b.Property<bool>("IsShared")
                        .HasColumnType("BIT");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<string>("Name")
                        .HasMaxLength(150)
                        .HasColumnType("NVARCHAR2(150)");

                    b.Property<string>("Note")
                        .HasColumnType("NVARCHAR2(32767)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<bool>("Show")
                        .HasColumnType("BIT");

                    b.Property<int>("TenantId")
                        .HasColumnType("INT");

                    b.Property<int>("VectorDBType")
                        .HasColumnType("INT");

                    b.Property<string>("VectorId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("NVARCHAR2(100)");

                    b.HasKey("Id");

                    b.ToTable("Senparc_AIKernel_AIVector");
                });
#pragma warning restore 612, 618
        }
    }
}

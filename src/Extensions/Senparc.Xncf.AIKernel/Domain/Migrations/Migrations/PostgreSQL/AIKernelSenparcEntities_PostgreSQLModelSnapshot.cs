﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Senparc.Xncf.AIKernel.Models;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Migrations.PostgreSQL
{
    [DbContext(typeof(AIKernelSenparcEntities_PostgreSQL))]
    partial class AIKernelSenparcEntities_PostgreSQLModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Senparc.Xncf.AIKernel.Models.AIModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)");

                    b.Property<int>("AiPlatform")
                        .HasColumnType("integer");

                    b.Property<string>("Alias")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("ApiKey")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("ApiVersion")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<int>("ConfigModelType")
                        .HasColumnType("integer");

                    b.Property<string>("DeploymentName")
                        .HasMaxLength(150)
                        .HasColumnType("character varying(150)");

                    b.Property<string>("Endpoint")
                        .HasMaxLength(250)
                        .HasColumnType("character varying(250)");

                    b.Property<bool>("Flag")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsShared")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("MaxToken")
                        .HasColumnType("integer");

                    b.Property<string>("ModelId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("Note")
                        .HasColumnType("text");

                    b.Property<string>("OrganizationId")
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)");

                    b.Property<bool>("Show")
                        .HasColumnType("boolean");

                    b.Property<int>("TenantId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("Senparc_AIKernel_AIModel");
                });

            modelBuilder.Entity("Senparc.Xncf.AIKernel.Models.AIVector", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)");

                    b.Property<string>("Alias")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)");

                    b.Property<string>("ConnectionString")
                        .HasColumnType("text");

                    b.Property<bool>("Flag")
                        .HasColumnType("boolean");

                    b.Property<bool>("IsShared")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Name")
                        .HasMaxLength(150)
                        .HasColumnType("character varying(150)");

                    b.Property<string>("Note")
                        .HasColumnType("text");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("character varying(300)");

                    b.Property<bool>("Show")
                        .HasColumnType("boolean");

                    b.Property<int>("TenantId")
                        .HasColumnType("integer");

                    b.Property<int>("VectorDBType")
                        .HasColumnType("integer");

                    b.Property<string>("VectorId")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.HasKey("Id");

                    b.ToTable("Senparc_AIKernel_AIVector");
                });
#pragma warning restore 612, 618
        }
    }
}

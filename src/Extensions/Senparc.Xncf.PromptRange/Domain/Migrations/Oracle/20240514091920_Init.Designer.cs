﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oracle.EntityFrameworkCore.Metadata;
using Senparc.Xncf.PromptRange.Models;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Oracle
{
    [DbContext(typeof(PromptRangeSenparcEntities_Oracle))]
    [Migration("20240514091920_Init")]
    partial class Init
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            OracleModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.LlModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("NUMBER(10)");

                    OraclePropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TIMESTAMP(7)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

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

                    b.Property<string>("DeploymentName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("NVARCHAR2(100)");

                    b.Property<string>("Endpoint")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("NVARCHAR2(250)");

                    b.Property<bool>("Flag")
                        .HasColumnType("NUMBER(1)");

                    b.Property<bool>("IsShared")
                        .HasColumnType("NUMBER(1)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TIMESTAMP(7)");

                    b.Property<int>("MaxToken")
                        .HasColumnType("NUMBER(10)");

                    b.Property<int>("ModelType")
                        .HasMaxLength(20)
                        .HasColumnType("NUMBER(10)");

                    b.Property<string>("Note")
                        .HasMaxLength(1000)
                        .HasColumnType("NVARCHAR2(1000)");

                    b.Property<string>("OrganizationId")
                        .HasMaxLength(200)
                        .HasColumnType("NVARCHAR2(200)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<bool>("Show")
                        .HasColumnType("NUMBER(1)");

                    b.Property<int>("TenantId")
                        .HasColumnType("NUMBER(10)");

                    b.HasKey("Id");

                    b.ToTable("Senparc_PromptRange_LlModel");
                });

            modelBuilder.Entity("Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.PromptItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("NUMBER(10)");

                    OraclePropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TIMESTAMP(7)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<int>("Aiming")
                        .HasMaxLength(5)
                        .HasColumnType("NUMBER(10)");

                    b.Property<string>("Content")
                        .HasColumnType("NVARCHAR2(2000)");

                    b.Property<decimal>("EvalAvgScore")
                        .HasMaxLength(3)
                        .HasColumnType("DECIMAL(18, 2)");

                    b.Property<decimal>("EvalMaxScore")
                        .HasMaxLength(3)
                        .HasColumnType("DECIMAL(18, 2)");

                    b.Property<string>("ExpectedResultsJson")
                        .HasColumnType("NVARCHAR2(2000)");

                    b.Property<bool>("Flag")
                        .HasColumnType("NUMBER(1)");

                    b.Property<float>("FrequencyPenalty")
                        .HasColumnType("BINARY_FLOAT");

                    b.Property<string>("FullVersion")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("NVARCHAR2(50)");

                    b.Property<bool>("IsDraft")
                        .HasColumnType("NUMBER(1)");

                    b.Property<bool>("IsShare")
                        .HasColumnType("NUMBER(1)");

                    b.Property<DateTime>("LastRunTime")
                        .HasColumnType("TIMESTAMP(7)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TIMESTAMP(7)");

                    b.Property<int>("MaxToken")
                        .HasColumnType("NUMBER(10)");

                    b.Property<int>("ModelId")
                        .HasColumnType("NUMBER(10)");

                    b.Property<string>("NickName")
                        .HasMaxLength(50)
                        .HasColumnType("NVARCHAR2(50)");

                    b.Property<string>("Note")
                        .HasMaxLength(20)
                        .HasColumnType("NVARCHAR2(20)");

                    b.Property<string>("ParentTac")
                        .IsRequired()
                        .HasColumnType("NVARCHAR2(2000)");

                    b.Property<string>("Prefix")
                        .HasMaxLength(10)
                        .HasColumnType("NVARCHAR2(10)");

                    b.Property<float>("PresencePenalty")
                        .HasColumnType("BINARY_FLOAT");

                    b.Property<int>("RangeId")
                        .HasColumnType("NUMBER(10)");

                    b.Property<string>("RangeName")
                        .HasMaxLength(20)
                        .HasColumnType("NVARCHAR2(20)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<string>("StopSequences")
                        .HasColumnType("NVARCHAR2(2000)");

                    b.Property<string>("Suffix")
                        .HasMaxLength(10)
                        .HasColumnType("NVARCHAR2(10)");

                    b.Property<string>("Tactic")
                        .HasColumnType("NVARCHAR2(2000)");

                    b.Property<float>("Temperature")
                        .HasColumnType("BINARY_FLOAT");

                    b.Property<int>("TenantId")
                        .HasColumnType("NUMBER(10)");

                    b.Property<float>("TopP")
                        .HasColumnType("BINARY_FLOAT");

                    b.Property<string>("VariableDictJson")
                        .HasColumnType("NVARCHAR2(2000)");

                    b.HasKey("Id");

                    b.ToTable("Senparc_PromptRange_PromptItem");
                });

            modelBuilder.Entity("Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.PromptRange", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("NUMBER(10)");

                    OraclePropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TIMESTAMP(7)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<string>("Alias")
                        .HasMaxLength(50)
                        .HasColumnType("NVARCHAR2(50)");

                    b.Property<bool>("Flag")
                        .HasColumnType("NUMBER(1)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TIMESTAMP(7)");

                    b.Property<string>("RangeName")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("NVARCHAR2(20)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<int>("TenantId")
                        .HasColumnType("NUMBER(10)");

                    b.HasKey("Id");

                    b.ToTable("Senparc_PromptRange_PromptRange");
                });

            modelBuilder.Entity("Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel.PromptResult", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("NUMBER(10)");

                    OraclePropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TIMESTAMP(7)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<double>("CostTime")
                        .HasColumnType("BINARY_DOUBLE");

                    b.Property<decimal>("FinalScore")
                        .HasColumnType("DECIMAL(18, 2)");

                    b.Property<bool>("Flag")
                        .HasColumnType("NUMBER(1)");

                    b.Property<decimal>("HumanScore")
                        .HasColumnType("DECIMAL(18, 2)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TIMESTAMP(7)");

                    b.Property<int>("LlmModelId")
                        .HasColumnType("NUMBER(10)");

                    b.Property<int>("PromptCostToken")
                        .HasColumnType("NUMBER(10)");

                    b.Property<int>("PromptItemId")
                        .HasColumnType("NUMBER(10)");

                    b.Property<string>("PromptItemVersion")
                        .HasMaxLength(50)
                        .HasColumnType("NVARCHAR2(50)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<int>("ResultCostToken")
                        .HasColumnType("NUMBER(10)");

                    b.Property<string>("ResultString")
                        .HasColumnType("NVARCHAR2(2000)");

                    b.Property<decimal>("RobotScore")
                        .HasColumnType("DECIMAL(18, 2)");

                    b.Property<int>("TenantId")
                        .HasColumnType("NUMBER(10)");

                    b.Property<int>("TestType")
                        .HasColumnType("NUMBER(10)");

                    b.Property<int>("TotalCostToken")
                        .HasColumnType("NUMBER(10)");

                    b.HasKey("Id");

                    b.ToTable("Senparc_PromptRange_PromptResult");
                });
#pragma warning restore 612, 618
        }
    }
}
﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Senparc.Xncf.PromptRange.Models;

#nullable disable

namespace Senparc.Xncf.PromptRange.Domain.Migrations.Migrations.SqlServer
{
    [DbContext(typeof(PromptRangeSenparcEntities_SqlServer))]
    partial class PromptRangeSenparcEntities_SqlServerModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.3")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Senparc.Xncf.PromptRange.Models.LlmModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("ApiKey")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("ApiVersion")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Endpoint")
                        .IsRequired()
                        .HasMaxLength(250)
                        .HasColumnType("nvarchar(250)");

                    b.Property<bool>("Flag")
                        .HasColumnType("bit");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime2");

                    b.Property<int>("MaxToken")
                        .HasColumnType("int");

                    b.Property<string>("ModelType")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Note")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("OrganizationId")
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<string>("OtherModelName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<bool>("Show")
                        .HasColumnType("bit");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.Property<string>("TextCompletionModelName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("TextEmbeddingModelName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

                    b.ToTable("Senparc_PromptRange_LlmModel");
                });

            modelBuilder.Entity("Senparc.Xncf.PromptRange.Models.PromptResult", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<double>("CostTime")
                        .HasColumnType("float");

                    b.Property<bool>("Flag")
                        .HasColumnType("bit");

                    b.Property<int>("HumanScore")
                        .HasColumnType("int");

                    b.Property<bool>("IsRobotTestExactlyEquat")
                        .HasColumnType("bit");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime2");

                    b.Property<int>("LlmModelId")
                        .HasColumnType("int");

                    b.Property<int>("PromptCostToken")
                        .HasColumnType("int");

                    b.Property<int>("PromptGroupId")
                        .HasColumnType("int");

                    b.Property<int>("PromptItemId")
                        .HasColumnType("int");

                    b.Property<string>("PromptItemVersion")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<int>("ResultCostToken")
                        .HasColumnType("int");

                    b.Property<string>("ResultString")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("RobotScore")
                        .HasColumnType("bigint");

                    b.Property<string>("RobotTestExceptedResult")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.Property<int>("TestType")
                        .HasColumnType("int");

                    b.Property<int>("TotalCostToken")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("LlmModelId");

                    b.ToTable("Senparc_PromptRange_PromptResult");
                });

            modelBuilder.Entity("Senparc.Xncf.PromptRange.PromptItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("ChatSystemPrompt")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Content")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("EvaluationScore")
                        .HasColumnType("int");

                    b.Property<bool>("Flag")
                        .HasColumnType("bit");

                    b.Property<float>("FrequencyPenalty")
                        .HasColumnType("real");

                    b.Property<DateTime>("LastRunTime")
                        .HasColumnType("datetime2");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("datetime2");

                    b.Property<float>("MaxToken")
                        .HasColumnType("real");

                    b.Property<int>("ModelId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("NumsOfResults")
                        .HasColumnType("int");

                    b.Property<float>("PresencePenalty")
                        .HasColumnType("real");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<bool>("Show")
                        .HasColumnType("bit");

                    b.Property<string>("StopSequences")
                        .HasColumnType("nvarchar(max)");

                    b.Property<float>("Temperature")
                        .HasColumnType("real");

                    b.Property<int>("TenantId")
                        .HasColumnType("int");

                    b.Property<string>("TokenSelectionBiases")
                        .HasColumnType("nvarchar(max)");

                    b.Property<float>("TopP")
                        .HasColumnType("real");

                    b.Property<string>("Version")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Senparc_PromptRange_PromptItem");
                });

            modelBuilder.Entity("Senparc.Xncf.PromptRange.Models.PromptResult", b =>
                {
                    b.HasOne("Senparc.Xncf.PromptRange.Models.LlmModel", "LlmModel")
                        .WithMany()
                        .HasForeignKey("LlmModelId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("LlmModel");
                });
#pragma warning restore 612, 618
        }
    }
}

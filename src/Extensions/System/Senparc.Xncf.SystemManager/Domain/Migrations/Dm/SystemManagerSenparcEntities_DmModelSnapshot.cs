﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Senparc.Xncf.SystemManager.Domain.DatabaseModel;

#nullable disable

namespace Senparc.Xncf.SystemManager.Domain.Migrations.Dm
{
    [DbContext(typeof(SystemManagerSenparcEntities_Dm))]
    partial class SystemManagerSenparcEntities_DmModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Dm:ValueGenerationStrategy", DmValueGenerationStrategy.IdentityColumn)
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            modelBuilder.Entity("Senparc.Ncf.Core.Models.SystemConfig", b =>
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

                    b.Property<bool>("Flag")
                        .HasColumnType("BIT");

                    b.Property<bool?>("HideModuleManager")
                        .HasColumnType("BIT");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<string>("MchId")
                        .HasMaxLength(100)
                        .HasColumnType("NVARCHAR2(100)");

                    b.Property<string>("MchKey")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<string>("NeuCharAppKey")
                        .HasMaxLength(100)
                        .HasColumnType("NVARCHAR2(100)");

                    b.Property<string>("NeuCharAppSecret")
                        .HasMaxLength(100)
                        .HasColumnType("NVARCHAR2(100)");

                    b.Property<int>("NeuCharDeveloperId")
                        .HasColumnType("INT");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<string>("SystemName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("NVARCHAR2(100)");

                    b.Property<string>("TenPayAppId")
                        .HasMaxLength(100)
                        .HasColumnType("NVARCHAR2(100)");

                    b.Property<int>("TenantId")
                        .HasColumnType("INT");

                    b.HasKey("Id");

                    b.ToTable("SystemConfigs");
                });

            modelBuilder.Entity("Senparc.Xncf.SystemManager.Domain.DatabaseModel.FeedBack", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INT")
                        .HasAnnotation("Dm:ValueGenerationStrategy", DmValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("AccountId")
                        .HasColumnType("INT");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<string>("Content")
                        .HasColumnType("NVARCHAR2(32767)");

                    b.Property<bool>("Flag")
                        .HasColumnType("BIT");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<int>("TenantId")
                        .HasColumnType("INT");

                    b.HasKey("Id");

                    b.ToTable("FeedBacks");
                });
#pragma warning restore 612, 618
        }
    }
}

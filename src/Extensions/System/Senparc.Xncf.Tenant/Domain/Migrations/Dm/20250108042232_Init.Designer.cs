﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Senparc.Xncf.Tanent.Domain.DatabaseModel;

#nullable disable

namespace Senparc.Xncf.Tenant.Domain.Migrations.Dm
{
    [DbContext(typeof(TenantSenparcEntities_Dm))]
    [Migration("20250108042232_Init")]
    partial class Init
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Dm:ValueGenerationStrategy", DmValueGenerationStrategy.IdentityColumn)
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            modelBuilder.Entity("Senparc.Xncf.Tenant.Domain.DataBaseModel.TenantInfo", b =>
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

                    b.Property<bool>("Enable")
                        .HasColumnType("BIT");

                    b.Property<bool>("Flag")
                        .HasColumnType("BIT");

                    b.Property<Guid>("Guid")
                        .HasColumnType("CHAR(36)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("NVARCHAR2(32767)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<string>("TenantKey")
                        .IsRequired()
                        .HasColumnType("NVARCHAR2(32767)");

                    b.HasKey("Id");

                    b.ToTable("TenantInfos");
                });
#pragma warning restore 612, 618
        }
    }
}
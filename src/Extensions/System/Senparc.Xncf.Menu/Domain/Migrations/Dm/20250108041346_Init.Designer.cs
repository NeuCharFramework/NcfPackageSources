﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Senparc.Xncf.Menu.Models;

#nullable disable

namespace Senparc.Xncf.Menu.Domain.Migrations.Dm
{
    [DbContext(typeof(MenuSenparcEntities_Dm))]
    [Migration("20250108041346_Init")]
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

            modelBuilder.Entity("Senparc.Ncf.Core.Models.DataBaseModel.SysButton", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("NVARCHAR2(450)");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<string>("ButtonName")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("NVARCHAR2(150)");

                    b.Property<bool>("Flag")
                        .HasColumnType("BIT");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<string>("MenuId")
                        .HasMaxLength(150)
                        .HasColumnType("NVARCHAR2(150)");

                    b.Property<string>("OpearMark")
                        .HasMaxLength(150)
                        .HasColumnType("NVARCHAR2(150)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<int>("TenantId")
                        .HasColumnType("INT");

                    b.Property<string>("Url")
                        .HasMaxLength(500)
                        .HasColumnType("NVARCHAR2(500)");

                    b.HasKey("Id");

                    b.ToTable("SysButtons");
                });

            modelBuilder.Entity("Senparc.Ncf.Core.Models.DataBaseModel.SysMenu", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(150)
                        .HasColumnType("NVARCHAR2(150)");

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<bool>("Flag")
                        .HasColumnType("BIT");

                    b.Property<string>("Icon")
                        .HasMaxLength(150)
                        .HasColumnType("NVARCHAR2(150)");

                    b.Property<bool>("IsLocked")
                        .HasColumnType("BIT");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TIMESTAMP");

                    b.Property<string>("MenuName")
                        .IsRequired()
                        .HasMaxLength(150)
                        .HasColumnType("NVARCHAR2(150)");

                    b.Property<int>("MenuType")
                        .HasColumnType("INT");

                    b.Property<string>("ParentId")
                        .HasMaxLength(150)
                        .HasColumnType("NVARCHAR2(150)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<string>("ResourceCode")
                        .HasMaxLength(150)
                        .HasColumnType("NVARCHAR2(150)");

                    b.Property<int>("Sort")
                        .HasColumnType("INT");

                    b.Property<int>("TenantId")
                        .HasColumnType("INT");

                    b.Property<string>("Url")
                        .HasMaxLength(500)
                        .HasColumnType("NVARCHAR2(500)");

                    b.Property<bool>("Visible")
                        .HasColumnType("BIT");

                    b.HasKey("Id");

                    b.ToTable("SysMenus");
                });
#pragma warning restore 612, 618
        }
    }
}

﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Oracle.EntityFrameworkCore.Metadata;
using Senparc.Xncf.MCP.Models;
using System;

#nullable disable

namespace Senparc.Xncf.MCP.Domain.Migrations.Oracle
{
    [DbContext(typeof(MCPSenparcEntities_Oracle))]
    partial class MCPSenparcEntities_OracleModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            OracleModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Senparc.Xncf.MCP.Color", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("NUMBER(10)");

                    OraclePropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("AddTime")
                        .HasColumnType("TIMESTAMP(7)");

                    b.Property<string>("AdditionNote")
                        .HasColumnType("NVARCHAR2(2000)");

                    b.Property<string>("AdminRemark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<int>("Blue")
                        .HasColumnType("NUMBER(10)");

                    b.Property<bool>("Flag")
                        .HasColumnType("NUMBER(1)");

                    b.Property<int>("Green")
                        .HasColumnType("NUMBER(10)");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TIMESTAMP(7)");

                    b.Property<int>("Red")
                        .HasColumnType("NUMBER(10)");

                    b.Property<string>("Remark")
                        .HasMaxLength(300)
                        .HasColumnType("NVARCHAR2(300)");

                    b.Property<int>("TenantId")
                        .HasColumnType("NUMBER(10)");

                    b.HasKey("Id");

                    b.ToTable("Senparc_MCP_Color");
                });
#pragma warning restore 612, 618
        }
    }
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20261010172300_AddFineTuningControlPlane.cs
    文件功能描述：EF Core 数据库迁移
    
    
    创建标识：Senparc - 20261014
    
    修改标识：Senparc - 20261021
    修改描述：v0.17.0 增加微调控制面数据库迁移

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class AddFineTuningControlPlane : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Senparc_AIKernel_AIFineTuningArtifact",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    WorkerAlias = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    JobExternalId = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    ExternalId = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(255)", maxLength: 255, nullable: false),
                    Bytes = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AIKernel_AIFineTuningArtifact", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_AIKernel_AIFineTuningDataset",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    WorkerAlias = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    ExternalId = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Rows = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    Sha256 = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AIKernel_AIFineTuningDataset", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_AIKernel_AIFineTuningEvent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    WorkerAlias = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    JobExternalId = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    Sequence = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    Kind = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    MetricsJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AIKernel_AIFineTuningEvent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_AIKernel_AIFineTuningJob",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    WorkerAlias = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    ExternalId = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    FinishedUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    RequestJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    Error = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    LatestMetricsJson = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    LastEventSequence = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AIKernel_AIFineTuningJob", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Senparc_AIKernel_AIFineTuningMigration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    WorkerAlias = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    StoreId = table.Column<string>(type: "NVARCHAR2(128)", maxLength: 128, nullable: false),
                    MigrationKey = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "NVARCHAR2(50)", maxLength: 50, nullable: false),
                    SourceVersion = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    TargetVersion = table.Column<string>(type: "NVARCHAR2(100)", maxLength: 100, nullable: true),
                    StartedUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    Error = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    Flag = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    AddTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    LastUpdateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    TenantId = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    AdminRemark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true),
                    Remark = table.Column<string>(type: "NVARCHAR2(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Senparc_AIKernel_AIFineTuningMigration", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Senparc_AIKernel_AIFineTuningArtifact");

            migrationBuilder.DropTable(
                name: "Senparc_AIKernel_AIFineTuningDataset");

            migrationBuilder.DropTable(
                name: "Senparc_AIKernel_AIFineTuningEvent");

            migrationBuilder.DropTable(
                name: "Senparc_AIKernel_AIFineTuningJob");

            migrationBuilder.DropTable(
                name: "Senparc_AIKernel_AIFineTuningMigration");
        }
    }
}

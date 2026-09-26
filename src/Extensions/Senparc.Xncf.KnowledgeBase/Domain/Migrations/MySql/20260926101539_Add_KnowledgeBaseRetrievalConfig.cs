/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：20260926101539_Add_KnowledgeBaseRetrievalConfig.cs
    文件功能描述：实现 Entity Framework 数据库迁移。


    创建标识：Senparc - 20260926

----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.KnowledgeBase.Domain.Migrations.MySql
{
    /// <inheritdoc />
    public partial class Add_KnowledgeBaseRetrievalConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RetrievalConfigJson",
                table: "Senparc_KnowledgeBase_KnowledgeBase",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetrievalConfigJson",
                table: "Senparc_KnowledgeBase_KnowledgeBase");
        }
    }
}

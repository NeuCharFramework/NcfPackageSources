/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：20260703153508_Add_IsArchived.cs
    文件功能描述：数据库迁移定义
    
    
    创建标识：Senparc - 20250816
    
    修改标识：Senparc - 20260704
    修改描述：v0.11.0-preview2 新增 ChatTask 归档能力并完善多数据库迁移支持

----------------------------------------------------------------*/
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class Add_IsArchived : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Senparc_AgentsManager_ChatTask",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Senparc_AgentsManager_ChatTask");
        }
    }
}

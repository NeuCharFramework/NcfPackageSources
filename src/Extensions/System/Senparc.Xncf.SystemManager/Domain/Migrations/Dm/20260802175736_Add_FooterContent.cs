/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20260802175736_Add_FooterContent.cs
    File description: Entity Framework database migration implementation.

    Created by: Senparc - 20250120

    Modified by: Senparc - 20260804
    Change description: v0.15.3-preview6 Updated module behavior and related support.

----------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.SystemManager.Domain.Migrations.Dm
{
    /// <inheritdoc />
    public partial class Add_FooterContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FooterContent",
                table: "SystemConfigs",
                type: "NVARCHAR2(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "© 2026 Senparc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FooterContent",
                table: "SystemConfigs");
        }
    }
}

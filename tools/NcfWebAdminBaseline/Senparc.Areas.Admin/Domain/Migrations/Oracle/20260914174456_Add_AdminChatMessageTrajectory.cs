/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: 20260914174456_Add_AdminChatMessageTrajectory.cs
    File description: Related implementation.

    Created by: Senparc - 20260915

    Modified by: Senparc - 20260915
    Change description: v0.8.0 Updated module behavior and related support.

----------------------------------------------------------------*/
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class Add_AdminChatMessageTrajectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrajectoryId",
                table: "ADMIN_AdminChatMessage",
                type: "NUMBER(10)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrajectorySequence",
                table: "ADMIN_AdminChatMessage",
                type: "NUMBER(10)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrajectoryId",
                table: "ADMIN_AdminChatMessage");

            migrationBuilder.DropColumn(
                name: "TrajectorySequence",
                table: "ADMIN_AdminChatMessage");
        }
    }
}

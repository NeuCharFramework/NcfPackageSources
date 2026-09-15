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

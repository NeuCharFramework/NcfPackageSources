using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Areas.Admin.Domain.Migrations.Sqlite
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
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrajectorySequence",
                table: "ADMIN_AdminChatMessage",
                type: "INTEGER",
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

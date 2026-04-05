using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AgentsManager.Domain.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class Add_ChatTask_ScheduleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsScheduled",
                table: "Senparc_AgentsManager_ChatTask",
                type: "NUMBER(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleIntervalMinutes",
                table: "Senparc_AgentsManager_ChatTask",
                type: "NUMBER(10)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleType",
                table: "Senparc_AgentsManager_ChatTask",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsScheduled",
                table: "Senparc_AgentsManager_ChatTask");

            migrationBuilder.DropColumn(
                name: "ScheduleIntervalMinutes",
                table: "Senparc_AgentsManager_ChatTask");

            migrationBuilder.DropColumn(
                name: "ScheduleType",
                table: "Senparc_AgentsManager_ChatTask");
        }
    }
}

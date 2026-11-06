using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Migrations.PostgreSQL
{
    /// <inheritdoc />
    public partial class AddFineTuningWorkerLaunchProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LaunchProfileJson",
                table: "Senparc_AIKernel_AIFineTuningWorker",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LaunchProfileJson",
                table: "Senparc_AIKernel_AIFineTuningWorker");
        }
    }
}

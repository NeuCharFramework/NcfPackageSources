using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class AddAlias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Senparc_AIKernel_AIModel",
                newName: "DeploymentName");

            migrationBuilder.AddColumn<string>(
                name: "Alias",
                table: "Senparc_AIKernel_AIModel",
                type: "NVARCHAR2(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsShared",
                table: "Senparc_AIKernel_AIModel",
                type: "NUMBER(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Alias",
                table: "Senparc_AIKernel_AIModel");

            migrationBuilder.DropColumn(
                name: "IsShared",
                table: "Senparc_AIKernel_AIModel");

            migrationBuilder.RenameColumn(
                name: "DeploymentName",
                table: "Senparc_AIKernel_AIModel",
                newName: "Name");
        }
    }
}

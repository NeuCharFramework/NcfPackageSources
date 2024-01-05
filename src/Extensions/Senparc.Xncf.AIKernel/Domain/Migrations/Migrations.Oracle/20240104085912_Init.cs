using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Senparc.Xncf.AIKernel.Domain.Migrations.Migrations.Oracle
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Endpoint",
                table: "Senparc_AIKernel_AIModel",
                type: "NVARCHAR2(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(250)",
                oldMaxLength: 250);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Endpoint",
                table: "Senparc_AIKernel_AIModel",
                type: "NVARCHAR2(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "NVARCHAR2(250)",
                oldMaxLength: 250,
                oldNullable: true);
        }
    }
}

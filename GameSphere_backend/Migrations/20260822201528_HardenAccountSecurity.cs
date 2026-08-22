using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameSphere_backend.Migrations
{
    /// <inheritdoc />
    public partial class HardenAccountSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResetCode",
                table: "Users",
                newName: "ResetCodeHash");

            migrationBuilder.Sql("UPDATE \"Users\" SET \"ResetCodeHash\" = NULL;");

            migrationBuilder.AddColumn<int>(
                name: "AuthVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ResetCodeAttempts",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResetCodeAttempts",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "ResetCodeHash",
                table: "Users",
                newName: "ResetCode");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameSphere_backend.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceQuizPublicationWithStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Quizzs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE \"Quizzs\" SET \"Status\" = 1 WHERE \"IsPublished\";");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "Quizzs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "Quizzs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE \"Quizzs\" SET \"IsPublished\" = TRUE WHERE \"Status\" = 1;");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Quizzs");
        }
    }
}

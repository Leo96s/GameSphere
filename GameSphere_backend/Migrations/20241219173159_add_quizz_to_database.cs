using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GameSphere_backend.Migrations
{
    /// <inheritdoc />
    public partial class add_quizz_to_database : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quizzs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    difficulty = table.Column<int>(type: "integer", nullable: false),
                    numberOfQuests = table.Column<int>(type: "integer", nullable: false),
                    registrationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    userId = table.Column<int>(type: "integer", nullable: false),
                    Quizzid = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quizzs", x => x.id);
                    table.ForeignKey(
                        name: "FK_Quizzs_Quizzs_Quizzid",
                        column: x => x.Quizzid,
                        principalTable: "Quizzs",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Quizzs_Users_userId",
                        column: x => x.userId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quizzs_Quizzid",
                table: "Quizzs",
                column: "Quizzid");

            migrationBuilder.CreateIndex(
                name: "IX_Quizzs_userId",
                table: "Quizzs",
                column: "userId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Quizzs");
        }
    }
}

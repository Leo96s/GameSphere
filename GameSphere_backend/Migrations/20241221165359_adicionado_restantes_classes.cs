using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GameSphere_backend.Migrations
{
    /// <inheritdoc />
    public partial class adicionado_restantes_classes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzs_Quizzs_Quizzid",
                table: "Quizzs");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzs_Users_userId",
                table: "Quizzs");

            migrationBuilder.DropIndex(
                name: "IX_Quizzs_Quizzid",
                table: "Quizzs");

            migrationBuilder.DropColumn(
                name: "Quizzid",
                table: "Quizzs");

            migrationBuilder.RenameColumn(
                name: "totalPoints",
                table: "Users",
                newName: "TotalPoints");

            migrationBuilder.RenameColumn(
                name: "registrationDate",
                table: "Users",
                newName: "RegistrationDate");

            migrationBuilder.RenameColumn(
                name: "level",
                table: "Users",
                newName: "Level");

            migrationBuilder.RenameColumn(
                name: "lastName",
                table: "Users",
                newName: "LastName");

            migrationBuilder.RenameColumn(
                name: "image",
                table: "Users",
                newName: "Image");

            migrationBuilder.RenameColumn(
                name: "hashedPassword",
                table: "Users",
                newName: "HashedPassword");

            migrationBuilder.RenameColumn(
                name: "gender",
                table: "Users",
                newName: "Gender");

            migrationBuilder.RenameColumn(
                name: "firstName",
                table: "Users",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "externalId",
                table: "Users",
                newName: "ExternalId");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "userId",
                table: "Quizzs",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "Quizzs",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "registrationDate",
                table: "Quizzs",
                newName: "RegistrationDate");

            migrationBuilder.RenameColumn(
                name: "numberOfQuests",
                table: "Quizzs",
                newName: "NumberOfQuests");

            migrationBuilder.RenameColumn(
                name: "difficulty",
                table: "Quizzs",
                newName: "Difficulty");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Quizzs",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Quizzs_userId",
                table: "Quizzs",
                newName: "IX_Quizzs_UserId");

            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NameOfAchievement = table.Column<string>(type: "text", nullable: false),
                    RequirementsForUnlock = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Achievements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Game",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TypoOfGame = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Game", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Game_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Question",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TypeOfAnswer = table.Column<int>(type: "integer", nullable: false),
                    Answers = table.Column<string[]>(type: "text[]", nullable: false),
                    CorrectAnswer = table.Column<string>(type: "text", nullable: false),
                    QuizzId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Question", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Question_Quizzs_QuizzId",
                        column: x => x.QuizzId,
                        principalTable: "Quizzs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_UserId",
                table: "Achievements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Game_UserId",
                table: "Game",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Question_QuizzId",
                table: "Question",
                column: "QuizzId");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzs_Users_UserId",
                table: "Quizzs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Quizzs_Users_UserId",
                table: "Quizzs");

            migrationBuilder.DropTable(
                name: "Achievements");

            migrationBuilder.DropTable(
                name: "Game");

            migrationBuilder.DropTable(
                name: "Question");

            migrationBuilder.RenameColumn(
                name: "TotalPoints",
                table: "Users",
                newName: "totalPoints");

            migrationBuilder.RenameColumn(
                name: "RegistrationDate",
                table: "Users",
                newName: "registrationDate");

            migrationBuilder.RenameColumn(
                name: "Level",
                table: "Users",
                newName: "level");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Users",
                newName: "lastName");

            migrationBuilder.RenameColumn(
                name: "Image",
                table: "Users",
                newName: "image");

            migrationBuilder.RenameColumn(
                name: "HashedPassword",
                table: "Users",
                newName: "hashedPassword");

            migrationBuilder.RenameColumn(
                name: "Gender",
                table: "Users",
                newName: "gender");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "Users",
                newName: "firstName");

            migrationBuilder.RenameColumn(
                name: "ExternalId",
                table: "Users",
                newName: "externalId");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Users",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Users",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Quizzs",
                newName: "userId");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Quizzs",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "RegistrationDate",
                table: "Quizzs",
                newName: "registrationDate");

            migrationBuilder.RenameColumn(
                name: "NumberOfQuests",
                table: "Quizzs",
                newName: "numberOfQuests");

            migrationBuilder.RenameColumn(
                name: "Difficulty",
                table: "Quizzs",
                newName: "difficulty");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Quizzs",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_Quizzs_UserId",
                table: "Quizzs",
                newName: "IX_Quizzs_userId");

            migrationBuilder.AddColumn<int>(
                name: "Quizzid",
                table: "Quizzs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzs_Quizzid",
                table: "Quizzs",
                column: "Quizzid");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzs_Quizzs_Quizzid",
                table: "Quizzs",
                column: "Quizzid",
                principalTable: "Quizzs",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzs_Users_userId",
                table: "Quizzs",
                column: "userId",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

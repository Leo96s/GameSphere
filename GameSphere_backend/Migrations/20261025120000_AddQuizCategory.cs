using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameSphere_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Quizzs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ContentKey",
                table: "Quizzs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzs_ContentKey",
                table: "Quizzs",
                column: "ContentKey",
                unique: true,
                filter: "\"ContentKey\" IS NOT NULL");

            // Titles of the form "<Label> — <rest>" carried the category; derive it and use the trimmed rest as the title.
            // Rows whose rest is empty after trimming stay untouched (category 0).
            migrationBuilder.Sql(@"
UPDATE ""Quizzs"" AS q SET
    ""Category"" = p.category,
    ""Title"" = btrim(substr(q.""Title"", p.prefix_length + 1))
FROM (SELECT v.prefix, v.category, char_length(v.prefix) AS prefix_length FROM (VALUES
    ('Cultura geral — ', 0),
    ('Geografia — ', 1),
    ('História — ', 2),
    ('Ciência — ', 3),
    ('Desporto — ', 4),
    ('Entretenimento — ', 5),
    ('Tecnologia — ', 6),
    ('Videojogos — ', 7)
) AS v(prefix, category)) AS p
WHERE left(q.""Title"", p.prefix_length) = p.prefix
  AND btrim(substr(q.""Title"", p.prefix_length + 1)) <> '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The Down is lossy by design and does not restore everything the Up changed:
            //  - the "Cultura geral — " prefix is not restored (category 0 is the default, so it cannot be told apart from rows that never had it);
            //  - titles that would exceed the 100 character limit with the prefix keep the stripped title;
            //  - the ContentKey column is dropped together with its unique index, so curated quizzes lose their stable identity.
            // Restore the prefix only when the result still fits in the 100 character title limit.
            migrationBuilder.Sql(@"
UPDATE ""Quizzs"" SET ""Title"" = CASE ""Category""
                WHEN 1 THEN 'Geografia — '
                WHEN 2 THEN 'História — '
                WHEN 3 THEN 'Ciência — '
                WHEN 4 THEN 'Desporto — '
                WHEN 5 THEN 'Entretenimento — '
                WHEN 6 THEN 'Tecnologia — '
                WHEN 7 THEN 'Videojogos — '
    END || ""Title""
WHERE ""Category"" BETWEEN 1 AND 7
  AND char_length(CASE ""Category""
                WHEN 1 THEN 'Geografia — '
                WHEN 2 THEN 'História — '
                WHEN 3 THEN 'Ciência — '
                WHEN 4 THEN 'Desporto — '
                WHEN 5 THEN 'Entretenimento — '
                WHEN 6 THEN 'Tecnologia — '
                WHEN 7 THEN 'Videojogos — '
    END || ""Title"") <= 100;");

            migrationBuilder.DropIndex(
                name: "IX_Quizzs_ContentKey",
                table: "Quizzs");

            migrationBuilder.DropColumn(
                name: "ContentKey",
                table: "Quizzs");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Quizzs");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameSphere_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCredentialsManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasLocalPassword",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PendingEmail",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PendingEmailCodeAttempts",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PendingEmailCodeExpiration",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingEmailCodeHash",
                table: "Users",
                type: "text",
                nullable: true);

            // Backfill: local accounts (no linked Firebase UID) already know a real
            // password from registration or a password reset, unlike social-only
            // accounts whose HashedPassword is an unknown, randomly generated value.
            migrationBuilder.Sql("UPDATE \"Users\" SET \"HasLocalPassword\" = true WHERE \"UID\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasLocalPassword",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PendingEmail",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PendingEmailCodeAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PendingEmailCodeExpiration",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PendingEmailCodeHash",
                table: "Users");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameSphere_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountDeactivationRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymized",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RecoveryCodeAttempts",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecoveryCodeExpiration",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryCodeHash",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsAnonymized",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RecoveryCodeAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RecoveryCodeExpiration",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RecoveryCodeHash",
                table: "Users");
        }
    }
}

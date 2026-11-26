using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SevenPos.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPinLockout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "failed_pin_attempts",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "locked_until_utc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "failed_pin_attempts",
                table: "users");

            migrationBuilder.DropColumn(
                name: "locked_until_utc",
                table: "users");
        }
    }
}

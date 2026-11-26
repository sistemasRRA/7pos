using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SevenPos.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestMarketingConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "guests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<bool>(
                name: "marketing_consent",
                table: "guests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "marketing_consent_at_utc",
                table: "guests",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "marketing_consent",
                table: "guests");

            migrationBuilder.DropColumn(
                name: "marketing_consent_at_utc",
                table: "guests");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SevenPos.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "list_price",
                table: "garments",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "branches",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            // Existing branches would all get "" and break the unique index below, so each
            // gets a placeholder code (S001, S002…) that an admin can replace later.
            migrationBuilder.Sql("""
                UPDATE branches AS b
                SET code = 'S' || lpad(n.row::text, 3, '0')
                FROM (SELECT id, row_number() OVER (ORDER BY name, id) AS row FROM branches) AS n
                WHERE b.id = n.id;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_branches_code",
                table: "branches",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_branches_code",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "list_price",
                table: "garments");

            migrationBuilder.DropColumn(
                name: "code",
                table: "branches");
        }
    }
}

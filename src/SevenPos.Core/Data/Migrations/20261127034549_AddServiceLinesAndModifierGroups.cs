using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SevenPos.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceLinesAndModifierGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "service_line_id",
                table: "products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "modifier_group_id",
                table: "modifiers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "service_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    last_modified_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sync_status = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_lines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "modifier_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    last_modified_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sync_status = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_modifier_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_modifier_groups_service_lines_service_line_id",
                        column: x => x.service_line_id,
                        principalTable: "service_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Catalogs created before service lines existed (development sample data) move to a
            // "General" line, and each old modifier becomes the only option of a group with its
            // name (the group reuses the modifier's id). Without this the foreign keys below fail.
            migrationBuilder.Sql("""
                INSERT INTO service_lines (id, name, sort_order, last_modified_utc, sync_status, is_active)
                SELECT gen_random_uuid(), 'General', 0, now(), 'Pending', true
                WHERE EXISTS (SELECT 1 FROM products) OR EXISTS (SELECT 1 FROM modifiers);

                UPDATE products SET service_line_id = (SELECT id FROM service_lines WHERE name = 'General')
                WHERE EXISTS (SELECT 1 FROM service_lines WHERE name = 'General');

                INSERT INTO modifier_groups (id, service_line_id, name, sort_order, last_modified_utc, sync_status, is_active)
                SELECT m.id, l.id, m.name, m.sort_order, now(), 'Pending', m.is_active
                FROM modifiers m CROSS JOIN service_lines l
                WHERE l.name = 'General';

                UPDATE modifiers SET modifier_group_id = id;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_products_service_line_id_name",
                table: "products",
                columns: new[] { "service_line_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_modifiers_modifier_group_id_name",
                table: "modifiers",
                columns: new[] { "modifier_group_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_modifier_groups_service_line_id_name",
                table: "modifier_groups",
                columns: new[] { "service_line_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_lines_name",
                table: "service_lines",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_modifiers_modifier_groups_modifier_group_id",
                table: "modifiers",
                column: "modifier_group_id",
                principalTable: "modifier_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_products_service_lines_service_line_id",
                table: "products",
                column: "service_line_id",
                principalTable: "service_lines",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_modifiers_modifier_groups_modifier_group_id",
                table: "modifiers");

            migrationBuilder.DropForeignKey(
                name: "fk_products_service_lines_service_line_id",
                table: "products");

            migrationBuilder.DropTable(
                name: "modifier_groups");

            migrationBuilder.DropTable(
                name: "service_lines");

            migrationBuilder.DropIndex(
                name: "ix_products_service_line_id_name",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_modifiers_modifier_group_id_name",
                table: "modifiers");

            migrationBuilder.DropColumn(
                name: "service_line_id",
                table: "products");

            migrationBuilder.DropColumn(
                name: "modifier_group_id",
                table: "modifiers");
        }
    }
}

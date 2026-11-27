using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SevenPos.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "price_list_id",
                table: "branches",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "price_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    last_modified_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sync_status = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_lists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "price_list_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    effective_from_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    set_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_modified_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sync_status = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_price_list_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_price_list_items_price_lists_price_list_id",
                        column: x => x.price_list_id,
                        principalTable: "price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_price_list_items_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_price_list_items_users_set_by_user_id",
                        column: x => x.set_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_branches_price_list_id",
                table: "branches",
                column: "price_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_list_items_price_list_id_product_id_effective_from_utc",
                table: "price_list_items",
                columns: new[] { "price_list_id", "product_id", "effective_from_utc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_price_list_items_product_id",
                table: "price_list_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_list_items_set_by_user_id",
                table: "price_list_items",
                column: "set_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_price_lists_name",
                table: "price_lists",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_branches_price_lists_price_list_id",
                table: "branches",
                column: "price_list_id",
                principalTable: "price_lists",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_branches_price_lists_price_list_id",
                table: "branches");

            migrationBuilder.DropTable(
                name: "price_list_items");

            migrationBuilder.DropTable(
                name: "price_lists");

            migrationBuilder.DropIndex(
                name: "ix_branches_price_list_id",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "price_list_id",
                table: "branches");
        }
    }
}

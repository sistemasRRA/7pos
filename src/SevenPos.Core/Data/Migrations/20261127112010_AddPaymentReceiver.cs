using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SevenPos.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentReceiver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Payments recorded before this column existed: the ticket's cashier received them.
            migrationBuilder.Sql("""
                UPDATE payments p SET user_id = t.cashier_id
                FROM tickets t WHERE t.id = p.ticket_id;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_payments_user_id",
                table: "payments",
                column: "user_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_payments_amount_positive",
                table: "payments",
                sql: "amount > 0");

            migrationBuilder.AddForeignKey(
                name: "fk_payments_users_user_id",
                table: "payments",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payments_users_user_id",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "ix_payments_user_id",
                table: "payments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payments_amount_positive",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "payments");
        }
    }
}

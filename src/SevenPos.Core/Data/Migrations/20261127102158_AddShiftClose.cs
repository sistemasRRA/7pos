using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SevenPos.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftClose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "closed_by_user_id",
                table: "shifts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "cash_withdrawals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Withdrawals recorded before this column existed: the cashier who opened the shift took them.
            migrationBuilder.Sql("""
                UPDATE cash_withdrawals w SET user_id = s.user_id
                FROM shifts s WHERE s.id = w.shift_id;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_shifts_closed_by_user_id",
                table: "shifts",
                column: "closed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_shifts_open_per_branch",
                table: "shifts",
                column: "branch_id",
                unique: true,
                filter: "closed_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cash_withdrawals_user_id",
                table: "cash_withdrawals",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_cash_withdrawals_users_user_id",
                table: "cash_withdrawals",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_shifts_users_closed_by_user_id",
                table: "shifts",
                column: "closed_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_cash_withdrawals_users_user_id",
                table: "cash_withdrawals");

            migrationBuilder.DropForeignKey(
                name: "fk_shifts_users_closed_by_user_id",
                table: "shifts");

            migrationBuilder.DropIndex(
                name: "ix_shifts_closed_by_user_id",
                table: "shifts");

            migrationBuilder.DropIndex(
                name: "ix_shifts_open_per_branch",
                table: "shifts");

            migrationBuilder.DropIndex(
                name: "ix_cash_withdrawals_user_id",
                table: "cash_withdrawals");

            migrationBuilder.DropColumn(
                name: "closed_by_user_id",
                table: "shifts");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "cash_withdrawals");
        }
    }
}

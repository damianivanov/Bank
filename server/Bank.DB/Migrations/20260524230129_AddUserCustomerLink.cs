using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Bank.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCustomerLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CreditTypeConditions",
                keyColumn: "Id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "CreditTypeConditions",
                keyColumn: "Id",
                keyValue: 2L);

            migrationBuilder.RenameColumn(
                name: "Iban",
                table: "BankAccounts",
                newName: "IBAN");

            migrationBuilder.RenameIndex(
                name: "IX_BankAccounts_Iban",
                table: "BankAccounts",
                newName: "IX_BankAccounts_IBAN");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CreditTypeConditions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<long>(
                name: "CustomerId",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CustomerId",
                table: "AspNetUsers",
                column: "CustomerId",
                unique: true,
                filter: "[CustomerId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Customers_CustomerId",
                table: "AspNetUsers",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Customers_CustomerId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CustomerId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "IBAN",
                table: "BankAccounts",
                newName: "Iban");

            migrationBuilder.RenameIndex(
                name: "IX_BankAccounts_IBAN",
                table: "BankAccounts",
                newName: "IX_BankAccounts_Iban");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "CreditTypeConditions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.InsertData(
                table: "CreditTypeConditions",
                columns: new[] { "Id", "CreatedById", "CreditType", "DateCreated", "DateModified", "IsActive", "MaximumAmount", "MaximumTermMonths", "ModifiedById", "Name", "StandardAnnualInterestRate", "StandardGrantingFee", "VipAnnualInterestRate", "VipGrantingFee" },
                values: new object[,]
                {
                    { 1L, null, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, 50000m, 84, null, "Consumer", 8.50m, 120m, 7.50m, 60m },
                    { 2L, null, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, 300000m, 360, null, "Mortgage", 4.50m, 300m, 3.90m, 150m }
                });
        }
    }
}

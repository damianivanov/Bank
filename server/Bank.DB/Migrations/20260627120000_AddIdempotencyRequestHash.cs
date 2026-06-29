using Bank.DB;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank.DB.Migrations;

// Написана на ръка (без паралелен .Designer.cs), защото работеща инстанция на Bank.Web държеше заключени
// DLL-ите и "dotnet ef migrations add" не можеше да билдне startup проекта. Атрибутите [DbContext]/[Migration]
// са вградени тук (както при ранните auth миграции), а AppDbContextModelSnapshot.cs е обновен ръчно и
// консистентно. За пълна симетрия с другите миграции — регенерирай с "dotnet ef migrations add", когато
// приложението е спряно (с билд, никога --no-build).
[DbContext(typeof(AppDbContext))]
[Migration("20260627120000_AddIdempotencyRequestHash")]
public partial class AddIdempotencyRequestHash : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "RequestHash",
            table: "MoneyTransactions",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "RequestHash",
            table: "DepositRequests",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "RequestHash",
            table: "MoneyTransactions");

        migrationBuilder.DropColumn(
            name: "RequestHash",
            table: "DepositRequests");
    }
}

using System;
using Bank.DB;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank.DB.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260520091000_AddAuthTokens")]
public partial class AddAuthTokens : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                Value = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_RefreshTokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Tokens",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<long>(type: "bigint", nullable: false),
                Value = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_Tokens_AspNetUsers_UserId",
                    column: x => x.UserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId",
            table: "RefreshTokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_Value",
            table: "RefreshTokens",
            column: "Value",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Tokens_UserId",
            table: "Tokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Tokens_Value",
            table: "Tokens",
            column: "Value",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RefreshTokens");
        migrationBuilder.DropTable(name: "Tokens");
    }
}

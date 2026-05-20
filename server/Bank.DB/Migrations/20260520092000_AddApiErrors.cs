using System;
using Bank.DB;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank.DB.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260520092000_AddApiErrors")]
public partial class AddApiErrors : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Errors",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                Details = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Errors", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Errors");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank.DB.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultPaymentType",
                table: "CreditTypeConditions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GracePeriodMonths",
                table: "CreditTypeConditions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PromoPeriodMonths",
                table: "CreditTypeConditions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardAnnualManagementFee",
                table: "CreditTypeConditions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardMonthlyManagementFee",
                table: "CreditTypeConditions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardPromoRate",
                table: "CreditTypeConditions",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VipAnnualManagementFee",
                table: "CreditTypeConditions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VipMonthlyManagementFee",
                table: "CreditTypeConditions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VipPromoRate",
                table: "CreditTypeConditions",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FeePart",
                table: "CreditInstallments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CreditTerms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreditId = table.Column<long>(type: "bigint", nullable: false),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFromPaymentNumber = table.Column<int>(type: "int", nullable: false),
                    Origin = table.Column<int>(type: "int", nullable: false),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    BaseAnnualInterestRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    PromoPeriodMonths = table.Column<int>(type: "int", nullable: false),
                    PromoAnnualInterestRate = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    GracePeriodMonths = table.Column<int>(type: "int", nullable: false),
                    Apr = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    WasVipApplied = table.Column<bool>(type: "bit", nullable: false),
                    PlannedMonthlyPaymentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedById = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditTerms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditTerms_Credits_CreditId",
                        column: x => x.CreditId,
                        principalTable: "Credits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditTermsFees",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreditTermsId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateModified = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedById = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedById = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditTermsFees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditTermsFees_CreditTerms_CreditTermsId",
                        column: x => x.CreditTermsId,
                        principalTable: "CreditTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTerms_CreditId_IsCurrent",
                table: "CreditTerms",
                columns: new[] { "CreditId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditTermsFees_CreditTermsId",
                table: "CreditTermsFees",
                column: "CreditTermsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditTermsFees");

            migrationBuilder.DropTable(
                name: "CreditTerms");

            migrationBuilder.DropColumn(
                name: "DefaultPaymentType",
                table: "CreditTypeConditions");

            migrationBuilder.DropColumn(
                name: "GracePeriodMonths",
                table: "CreditTypeConditions");

            migrationBuilder.DropColumn(
                name: "PromoPeriodMonths",
                table: "CreditTypeConditions");

            migrationBuilder.DropColumn(
                name: "StandardAnnualManagementFee",
                table: "CreditTypeConditions");

            migrationBuilder.DropColumn(
                name: "StandardMonthlyManagementFee",
                table: "CreditTypeConditions");

            migrationBuilder.DropColumn(
                name: "StandardPromoRate",
                table: "CreditTypeConditions");

            migrationBuilder.DropColumn(
                name: "VipAnnualManagementFee",
                table: "CreditTypeConditions");

            migrationBuilder.DropColumn(
                name: "VipMonthlyManagementFee",
                table: "CreditTypeConditions");

            migrationBuilder.DropColumn(
                name: "VipPromoRate",
                table: "CreditTypeConditions");

            migrationBuilder.DropColumn(
                name: "FeePart",
                table: "CreditInstallments");
        }
    }
}

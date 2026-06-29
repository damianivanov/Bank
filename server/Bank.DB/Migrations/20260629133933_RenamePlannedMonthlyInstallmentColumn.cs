using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bank.DB.Migrations
{
    /// <inheritdoc />
    public partial class RenamePlannedMonthlyInstallmentColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PlannedMonthlyInstallmentAmount",
                table: "Credits",
                newName: "PlannedMonthlyPaymentAmount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PlannedMonthlyPaymentAmount",
                table: "Credits",
                newName: "PlannedMonthlyInstallmentAmount");
        }
    }
}

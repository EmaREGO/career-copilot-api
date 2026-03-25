using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareerCopilot.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverLetterToEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "GlobalMatchPercentage",
                table: "Evaluations",
                type: "decimal(5,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverLetter",
                table: "Evaluations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverLetter",
                table: "Evaluations");

            migrationBuilder.AlterColumn<decimal>(
                name: "GlobalMatchPercentage",
                table: "Evaluations",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldNullable: true);
        }
    }
}

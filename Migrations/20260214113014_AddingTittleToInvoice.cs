using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleExampleInvoice.Migrations
{
    /// <inheritdoc />
    public partial class AddingTittleToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Titulo",
                table: "Invoices",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Titulo",
                table: "Invoices");
        }
    }
}

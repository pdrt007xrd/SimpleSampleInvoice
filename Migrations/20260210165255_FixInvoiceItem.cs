using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleExampleInvoice.Migrations
{
    /// <inheritdoc />
    public partial class FixInvoiceItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UnitPrice",
                table: "InvoiceItems",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "ProductName",
                table: "InvoiceItems",
                newName: "Description");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "InvoiceItems",
                newName: "UnitPrice");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "InvoiceItems",
                newName: "ProductName");
        }
    }
}

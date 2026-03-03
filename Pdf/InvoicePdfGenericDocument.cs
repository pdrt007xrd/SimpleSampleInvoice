using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SimpleExampleInvoice.Models;
using System.Security.Cryptography;

namespace SimpleExampleInvoice.Pdf
{
    public class InvoicePdfGenericDocument : IDocument
    {
        private readonly Invoice _invoice;

        public InvoicePdfGenericDocument(Invoice invoice)
        {
            _invoice = invoice;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        private static string GetInvoiceNumber(int id) => $"PP{id:D6}";

        private static string GetAccessKey()
        {
            var partA = RandomNumberGenerator.GetInt32(100_000_000, 999_999_999).ToString();
            var partB = RandomNumberGenerator.GetInt32(100_000_000, 999_999_999).ToString();
            return $"{partA}{partB}";
        }

        private static string Rule(int width) => new('-', width);

        private static string Money(decimal value) => $"{value:N2}";

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(80, 300, Unit.Millimetre);
                page.MarginTop(8);
                page.MarginBottom(8);
                page.MarginHorizontal(3);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Content().Column(col =>
                {
                    const int lineWidth = 54;
                    var createdAt = _invoice.CreatedAt == default ? DateTime.Now : _invoice.CreatedAt;
                    var items = _invoice.Items ?? Enumerable.Empty<InvoiceItem>();
                    var subtotal = items.Sum(i => i.Total);
                    var total = subtotal;
                    var companyName = string.IsNullOrWhiteSpace(_invoice.CompanyName)
                        ? "COMERCIAL GENERICO"
                        : _invoice.CompanyName.Trim().ToUpperInvariant();
                    var clientName = string.IsNullOrWhiteSpace(_invoice.ClientName)
                        ? "CONSUMIDOR FINAL"
                        : _invoice.ClientName.Trim().ToUpperInvariant();

                    col.Spacing(1);
                    col.Item().AlignCenter().Text(companyName).Bold();
                    col.Item().Text(Rule(lineWidth));
                    col.Item().Text($"CLIENTE: {clientName}");
                    col.Item().Text($"FACTURA No.: {GetInvoiceNumber(_invoice.Id)}");
                    col.Item().Text($"FECHA: {createdAt:dd/MM/yyyy HH:mm:ss}");
                    col.Item().Text($"CLAVE DE ACCESO: {GetAccessKey()}");
                    col.Item().Text("TIPO VENTA: CONTADO");
                    col.Item().Text(Rule(lineWidth));

                    col.Item().Row(row =>
                    {
                        row.RelativeItem(3).Text("DESCRIPCION");
                        row.RelativeItem(1).AlignRight().Text("CANT.");
                        row.RelativeItem(1).AlignRight().Text("PRECIO");
                        row.RelativeItem(1).AlignRight().Text("TOTAL");
                    });
                    col.Item().Text(Rule(lineWidth));

                    foreach (var item in items)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(3).Text((item.Description ?? string.Empty).ToUpperInvariant());
                            row.RelativeItem(1).AlignRight().Text($"{item.Quantity:0.##}");
                            row.RelativeItem(1).AlignRight().Text(Money(item.Price));
                            row.RelativeItem(1).AlignRight().Text(Money(item.Total));
                        });
                    }

                    col.Item().PaddingTop(1).Text(Rule(lineWidth));
                    col.Item().AlignRight().Text($"DSCTO 0.00%    {Money(0m)}");
                    col.Item().AlignRight().Text($"TOTAL          {Money(total)}").Bold();
                    col.Item().PaddingTop(1).Text(Rule(lineWidth));
                });
            });
        }
    }
}

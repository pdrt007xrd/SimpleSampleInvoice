using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SimpleExampleInvoice.Models;
using System.Security.Cryptography;

namespace SimpleExampleInvoice.Pdf
{
    public class InvoicePdfGenericAltDocument : IDocument
    {
        private readonly Invoice _invoice;

        public InvoicePdfGenericAltDocument(Invoice invoice)
        {
            _invoice = invoice;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        private static string GetInvoiceNumber(int id) => $"PP{id:D6}";

        private static string GetGenericNcf()
        {
            var first = RandomNumberGenerator.GetInt32(0, 1_000_000);
            var second = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return $"E{first:D6}{second:D6}";
        }

        private static string Money(decimal value) => $"RD$ {value:N2}";

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
                    var createdAt = _invoice.CreatedAt == default ? DateTime.Now : _invoice.CreatedAt;
                    var companyName = string.IsNullOrWhiteSpace(_invoice.CompanyName)
                        ? "SERVICIOS GENERALES EM"
                        : _invoice.CompanyName.Trim().ToUpperInvariant();
                    var clientName = string.IsNullOrWhiteSpace(_invoice.ClientName)
                        ? "CONSUMIDOR FINAL"
                        : _invoice.ClientName.Trim().ToUpperInvariant();
                    var items = _invoice.Items ?? Enumerable.Empty<InvoiceItem>();
                    var subtotal = items.Sum(i => i.Total);
                    var total = subtotal;

                    col.Spacing(2);

                    col.Item().Border(1).Padding(3).Column(header =>
                    {
                        header.Item().AlignCenter().Text(companyName).FontSize(10).Bold();
                        header.Item().AlignCenter().Text(" ").Bold();
                        header.Item().AlignCenter().Text($"No. {GetInvoiceNumber(_invoice.Id)}");
                        header.Item().AlignCenter().Text($"NCF: {GetGenericNcf()}");
                        header.Item().AlignCenter().Text($"{createdAt:dd/MM/yyyy hh:mm tt}");
                    });

                    col.Item().Border(1).Padding(3).Column(info =>
                    {
                        info.Item().Text($"Cliente: {clientName}");
                        info.Item().Text("Tipo: Contado");
                        info.Item().Text("Moneda: DOP");
                    });

                    col.Item().Border(1).Padding(3).Column(lines =>
                    {
                        lines.Item().Row(row =>
                        {
                            row.RelativeItem(3).Text("Producto").Bold();
                            row.RelativeItem(1).AlignRight().Text("Cant").Bold();
                            row.RelativeItem(2).AlignRight().Text("Total").Bold();
                        });

                        foreach (var item in items)
                        {
                            lines.Item().Row(row =>
                            {
                                row.RelativeItem(3).Text(item.Description);
                                row.RelativeItem(1).AlignRight().Text($"{item.Quantity:0.##}");
                                row.RelativeItem(2).AlignRight().Text(Money(item.Total));
                            });
                        }
                    });

                    col.Item().Border(1).Padding(3).Column(totals =>
                    {
                        totals.Item().AlignRight().Text($"Subtotal: {Money(subtotal)}");
                        totals.Item().AlignRight().Text(" ");
                        totals.Item().AlignRight().Text($"Total: {Money(total)}").Bold();
                    });

                    col.Item().AlignCenter().Text("Gracias por su compra").Bold();
                    col.Item().AlignCenter().Text("Conserve este comprobante");
                });
            });
        }
    }
}

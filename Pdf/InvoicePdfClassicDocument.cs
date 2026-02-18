using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SimpleExampleInvoice.Models;
using System.Security.Cryptography;

namespace SimpleExampleInvoice.Pdf
{
    public class InvoicePdfClassicDocument : IDocument
    {
        private readonly Invoice _invoice;

        public InvoicePdfClassicDocument(Invoice invoice)
        {
            _invoice = invoice;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        private static string GetInvoiceNumber(int id) => $"PP{id:D6}";

        private static string GetGenericNcfNumber()
        {
            var first = RandomNumberGenerator.GetInt32(0, 1_000_000);
            var second = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return $"E{first:D6}{second:D6}";
        }

        private static string GetGenericPhone()
        {
            var middle = $"6{RandomNumberGenerator.GetInt32(0, 100):D2}";
            var last = RandomNumberGenerator.GetInt32(0, 10000).ToString("D4");
            return $"809-{middle}-{last}";
        }

        private static string FormatRd(decimal amount) => $"RD${amount:N2}";

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(80, 300, Unit.Millimetre);
                page.MarginTop(41);
                page.MarginBottom(10);
                page.MarginHorizontal(2);
                page.DefaultTextStyle(x => x.FontSize(9).Bold());

                page.Content().Column(col =>
                {
                    var companyName = string.IsNullOrWhiteSpace(_invoice.CompanyName)
                        ? "Servicios Generales EM"
                        : _invoice.CompanyName;
                    var ncf = GetGenericNcfNumber();
                    var total = (_invoice.Items ?? Enumerable.Empty<InvoiceItem>()).Sum(x => x.Total);

                    col.Spacing(2);
                    col.Item().PaddingTop(10);
                    col.Item().AlignCenter().Text(companyName).FontSize(14).Bold();
                    col.Item().AlignCenter().Text($"Telefono: {GetGenericPhone()}");
                    col.Item().AlignCenter().Text($"Fecha: {DateTime.Now:dd/MM/yyyy hh:mm tt}");
                    col.Item().AlignCenter().Text("FACTURA DE CONSUMO");

                    if (!string.IsNullOrWhiteSpace(_invoice.ClientName))
                    {
                        col.Item().PaddingTop(6).AlignCenter().Text($"Cliente: {_invoice.ClientName}");
                    }

                    col.Item().AlignCenter().Text($"Factura No: {GetInvoiceNumber(_invoice.Id)}").FontSize(10).Bold();
                    col.Item().AlignCenter().Text($"NCF: {ncf}");
                    col.Item().PaddingTop(18).AlignCenter().Text("DETALLE DE FACTURA").Bold().Underline();

                    col.Item().PaddingTop(18).LineHorizontal(0.5f);
                    col.Item().PaddingTop(1).PaddingBottom(4).LineHorizontal(0.5f);

                    foreach (var item in _invoice.Items ?? Enumerable.Empty<InvoiceItem>())
                    {
                        col.Item().Text(item.Description).Bold();
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"{item.Quantity:0.####} x {FormatRd(item.Price)}");
                            row.ConstantItem(55).AlignRight().Text(FormatRd(item.Total));
                        });
                        col.Item().PaddingBottom(2);
                    }

                    col.Item().PaddingTop(6).LineHorizontal(0.5f);
                    col.Item().PaddingTop(1).PaddingBottom(4).LineHorizontal(0.5f);
                    col.Item().AlignRight().Text("SUBTOTAL: RD$0.00");
                    col.Item().AlignRight().Text("DESCUENTO: RD$0.00");
                    col.Item().AlignRight().Text($"TOTAL: {FormatRd(total)}").FontSize(11).Bold();
                    col.Item().PaddingTop(12).AlignCenter().Text("Gracias por su compra");
                });
            });
        }
    }
}

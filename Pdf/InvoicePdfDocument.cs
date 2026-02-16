using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SimpleExampleInvoice.Models;
using System.Security.Cryptography;

namespace SimpleExampleInvoice.Pdf
{
    public class InvoicePdfDocument : IDocument
    {
        private readonly Invoice _invoice;

        public InvoicePdfDocument(Invoice invoice)
        {
            _invoice = invoice;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        private string GetInvoiceNumber(int id)
        {
            return $"PP{id:D6}";
        }

        private string GetGenericPhone()
        {
            var middle = $"6{RandomNumberGenerator.GetInt32(0, 100):D2}";
            var last = RandomNumberGenerator.GetInt32(0, 10000).ToString("D4");
            return $"809-{middle}-{last}";
        }

        private static string FormatRd(decimal amount)
        {
            return $"RD$ {amount:N2}";
        }


        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                // 80mm térmico
                page.Size(80, 300, Unit.Millimetre);

                // márgenes tipo FACTURA 5
                page.MarginTop(28);
                page.MarginBottom(10);
                page.MarginHorizontal(6);

                page.DefaultTextStyle(x => x.FontSize(9).Bold());

                page.Content().Column(col =>
                {
                    col.Spacing(2); // compacto POS
                    col.Item().PaddingTop(10);
                    var companyName = string.IsNullOrWhiteSpace(_invoice.CompanyName)
                        ? "Servicios Generales EM"
                        : _invoice.CompanyName;

                    // ===== HEADER =====
                    col.Item().AlignCenter()
                        .Text(companyName)
                        .FontSize(14)
                        .Bold();

                    // col.Item().AlignCenter()
                    //     .Text("Av. Principal #123, Santo Domingo");

                    col.Item().AlignCenter()
                        .Text($"Tel: {GetGenericPhone()}");

                    col.Item().AlignCenter()
                        .Text($"Fecha: {DateTime.Now:dd/MM/yyyy hh:mm tt}");

                    // espacio claro antes del número de factura
                    col.Item().PaddingTop(6);
                    
                    // ===== CLIENTE =====
                    if (!string.IsNullOrWhiteSpace(_invoice.ClientName))
                    {
                        col.Item().PaddingTop(6)
                            .AlignCenter()
                            .Text($"Cliente: {_invoice.ClientName}")
                            .FontSize(9)
                            .Bold();
                    }

                    col.Item().AlignCenter()
                        .Text($"Factura No: {GetInvoiceNumber(_invoice.Id)}")
                        .FontSize(10)
                        .Bold();

                    // ===== doble separación HEADER → ITEMS =====
                    col.Item()
                        .PaddingTop(6)
                        .LineHorizontal(0.5f);

                    col.Item()
                        .PaddingTop(1)
                        .PaddingBottom(4)
                        .LineHorizontal(0.5f);

                    // ===== ITEMS =====
                    foreach (var item in _invoice.Items ?? Enumerable.Empty<InvoiceItem>())
                    {
                        col.Item().Text(item.Description).Bold();

                        col.Item().Row(row =>
                        {
                            row.RelativeItem()
                                .Text($"{item.Quantity} x {FormatRd(item.Price)}");

                            row.ConstantItem(55)
                                .AlignRight()
                                .Text(FormatRd(item.Total));
                        });

                        col.Item().PaddingBottom(2);
                    }

                    // ===== doble separación ITEMS → TOTAL =====
                    col.Item()
                        .PaddingTop(6)
                        .LineHorizontal(0.5f);

                    col.Item()
                        .PaddingTop(1)
                        .PaddingBottom(4)
                        .LineHorizontal(0.5f);

                    // ===== ITBIS + TOTAL =====
                    var subtotal = (_invoice.Items ?? Enumerable.Empty<InvoiceItem>()).Sum(x => x.Total);
                    var itbis = subtotal * 0.18m;
                    var total = subtotal + itbis;

                    col.Item().AlignRight()
                        .Text($"SUBTOTAL: {FormatRd(subtotal)}")
                        .FontSize(9);

                    col.Item().AlignRight()
                        .Text($"I.T.B.I.S (18%): {FormatRd(itbis)}")
                        .FontSize(9)
                        .Bold();

                    col.Item().AlignRight()
                        .Text($"TOTAL: {FormatRd(total)}")
                        .FontSize(11)
                        .Bold();

                    // ===== separación TOTAL → SALUDO =====
                    col.Item().PaddingTop(12);

                    col.Item().AlignCenter()
                        .Text("Gracias por su compra")
                        .FontSize(9);
                });
            });
        }
    }
}

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SimpleExampleInvoice.Models;

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



        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                // 80mm térmico
                page.Size(80, 300, Unit.Millimetre);

                // márgenes tipo FACTURA 5
                page.MarginTop(10);
                page.MarginBottom(10);
                page.MarginHorizontal(6);

                page.DefaultTextStyle(x => x.FontSize(9));

                page.Content().Column(col =>
                {
                    col.Spacing(2); // compacto POS
                    var companyName = string.IsNullOrWhiteSpace(_invoice.CompanyName)
                        ? "Servicios Generales EM"
                        : _invoice.CompanyName;

                    // ===== HEADER =====
                    col.Item().AlignCenter()
                        .Text(companyName)
                        .FontSize(14)
                        .Bold();

                    col.Item().AlignCenter()
                        .Text("Av. Principal #123, Santo Domingo");

                    col.Item().AlignCenter()
                        .Text("Tel: 809-555-1234");

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
                            .SemiBold();
                    }

                    col.Item().AlignCenter()
                        .Text($"Factura No: {GetInvoiceNumber(_invoice.Id)}")
                        .FontSize(10)
                        .Bold();

                    // ===== separación HEADER → ITEMS =====
                    col.Item()
                        .PaddingTop(8)
                        .PaddingBottom(6)
                        .LineHorizontal(0.5f);

                    // ===== ITEMS =====
                    foreach (var item in _invoice.Items)
                    {
                        col.Item().Text(item.Description).Bold();

                        col.Item().Row(row =>
                        {
                            row.RelativeItem()
                                .Text($"{item.Quantity} x {item.Price:C}");

                            row.ConstantItem(55)
                                .AlignRight()
                                .Text($"{item.Total:C}");
                        });

                        col.Item().PaddingBottom(2);
                    }

                    // ===== separación ITEMS → TOTAL =====
                    col.Item()
                        .PaddingTop(8)
                        .PaddingBottom(6)
                        .LineHorizontal(0.5f);

                    // ===== TOTAL =====
                    var total = _invoice.Items.Sum(x => x.Total);

                    col.Item().AlignRight()
                        .Text($"TOTAL: {total:C}")
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

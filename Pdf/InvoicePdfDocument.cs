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

        private string GetGenericNcfNumber()
        {
            var first = RandomNumberGenerator.GetInt32(0, 1_000_000);
            var second = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return $"E{first:D6}{second:D6}";
        }

        private string GetGenericPhone()
        {
            var middle = $"6{RandomNumberGenerator.GetInt32(0, 100):D2}";
            var last = RandomNumberGenerator.GetInt32(0, 10000).ToString("D4");
            return $"809-{middle}-{last}";
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(80, 300, Unit.Millimetre);
                page.MarginTop(41);
                page.MarginBottom(10);
                page.MarginHorizontal(2);

                page.DefaultTextStyle(x => x.FontSize(8));

                page.Content()
                    .Padding(3)
                    .Column(col =>
                    {
                        const int lineLength = 50;
                        var genericNcfNumber = GetGenericNcfNumber();
                        var now = DateTime.Now;
                        var items = _invoice.Items ?? Enumerable.Empty<InvoiceItem>();
                        var subtotal = items.Sum(x => x.Total);
                        var total = subtotal;
                        var companyName = string.IsNullOrWhiteSpace(_invoice.CompanyName)
                            ? "NOMBRE COMERCIAL"
                            : _invoice.CompanyName.Trim().ToUpperInvariant();

                        col.Spacing(2);

                        col.Item().AlignCenter().Text(companyName).Bold();
                        col.Item().AlignCenter().Text($"TELEFONO: {GetGenericPhone()}").Bold();
                        col.Item().Text("Direccion General de Impuestos Internos");
                        col.Item().Text("RNC 401506254");
                        col.Item().Text("RES DGI: 23-2009 DEL 06/ABRIL/2009");
                        col.Item().PaddingTop(2).AlignCenter().Text("COMPROBANTE AUTORIZADO POR DGII").Bold();
                        col.Item().Text($"{now:dd/MM/yyyy}    {now:HH:mm}");
                        col.Item().Text("NIF: 1234560000000002");
                        col.Item().Text($"NCF: {genericNcfNumber}");

                        col.Item().AlignCenter().Text(new string('-', lineLength));
                        col.Item().AlignCenter().Text("FACTURA DE CONSUMO").Bold();
                        col.Item().AlignCenter().Text(new string('-', lineLength));
                        col.Item().PaddingTop(2).Row(row =>
                        {
                            row.RelativeItem().Text("DETALLE DE FACTURA").Bold();
                            row.ConstantItem(55).AlignRight().Text("VALOR").Bold();
                        });
                        col.Item().AlignCenter().Text(new string('-', lineLength));

                        if (!string.IsNullOrWhiteSpace(_invoice.ClientName))
                        {
                            col.Item().Text($"CLIENTE: {_invoice.ClientName.Trim()}");
                            col.Item().PaddingBottom(3);
                        }

                        foreach (var item in items)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"{item.Quantity:0.####} x {item.Price:N2}");
                                row.ConstantItem(55).AlignRight().Text($"{item.Total:N2}");
                            });

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text(item.Description);
                                row.ConstantItem(55).AlignRight().Text(string.Empty);
                            });
                            col.Item().AlignCenter().Text(new string('-', lineLength));
                        }

                        col.Item().AlignRight().Text($"Subtotal       {subtotal:N2}");
                        col.Item().AlignRight().Text($"Total          {total:N2}").Bold();
                        col.Item().PaddingTop(2).AlignRight().Text($"Efectivo       {total:N2}");
                        col.Item().AlignRight().Text("Cambio         0.00");
                        col.Item().PaddingTop(4).AlignCenter().Text("Gracias por su compra, vuelva pronto");
                        col.Item().PaddingTop(4).AlignCenter().Text(new string('-', lineLength));
                        col.Item().Text("NIF: 1234560000000002");
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"ABCDFGH {GetInvoiceNumber(_invoice.Id)}");
                            row.ConstantItem(50).AlignRight().Text("V: 1.00 XXX");
                        });
                        col.Item().Text("//");
                    });
            });
        }
    }
}

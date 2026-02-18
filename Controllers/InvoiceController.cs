using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using SimpleExampleInvoice.Data;
using SimpleExampleInvoice.Models;
using SimpleExampleInvoice.Pdf;
using QuestPDF.Fluent;
using System.Text;
using System.Security.Cryptography;

namespace SimpleExampleInvoice.Controllers
{
    [Authorize]
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===============================
        // CREATE OR EDIT INVOICE
        // ===============================
        [HttpGet]
        public IActionResult Create()
        {
            return RedirectToAction("Edit");
        }

        public IActionResult Edit(int id = 0, int page = 1)
        {
            const int pageSize = 10;
            Invoice invoice;

            if (id == 0)
            {
                invoice = new Invoice();
            }
            else
            {
                var existingInvoice = _context.Invoices
                    .Include(i => i.Items)
                    .FirstOrDefault(i => i.Id == id);

                if (existingInvoice == null)
                    return NotFound();

                invoice = existingInvoice;
            }

            var totalInvoices = _context.Invoices.Count();

            var invoices = _context.Invoices
                .Include(i => i.Items)
                .OrderByDescending(i => i.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Invoices = invoices;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalInvoices / (double)pageSize);

            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, string companyName, string clientName)
        {
            if (id == 0)
            {
                var newInvoice = new Invoice
                {
                    CreatedAt = DateTime.Now,
                    Titulo = string.Empty,
                    CompanyName = companyName?.Trim() ?? string.Empty,
                    ClientName = clientName?.Trim() ?? string.Empty
                };

                _context.Invoices.Add(newInvoice);
                _context.SaveChanges();

                TempData["InvoiceCreatedMessage"] = $"Factura #{newInvoice.Id} creada exitosamente";
                return RedirectToAction("Edit", new { id = newInvoice.Id });
            }

            var invoice = _context.Invoices.Find(id);
            if (invoice == null)
                return NotFound();

            invoice.CompanyName = companyName?.Trim() ?? string.Empty;
            invoice.ClientName = clientName?.Trim() ?? string.Empty;
            _context.SaveChanges();

            return RedirectToAction("Edit", new { id });
        }



        // ===============================
        // ADD ITEM
        // ===============================
        [HttpPost]
        public IActionResult AddItem(int invoiceId, string description, decimal quantity, decimal price)
        {
            var invoice = _context.Invoices.Find(invoiceId);
            if (invoice == null)
                return NotFound();

            var item = new InvoiceItem
            {
                InvoiceId = invoiceId,
                Description = description,
                Quantity = quantity,
                Price = price
            };

            _context.InvoiceItems.Add(item);
            _context.SaveChanges();

            return RedirectToAction("Edit", new { id = invoiceId });
        }

        // ===============================
        // PREVIEW PDF
        // ===============================
        public IActionResult Preview(int id, string format = "fiscal")
        {
            ViewBag.InvoiceId = id;
            ViewBag.Format = string.Equals(format, "clasico", StringComparison.OrdinalIgnoreCase)
                ? "clasico"
                : "fiscal";
            return View();
        }

        // ===============================
        // PRINT / DOWNLOAD PDF
        // ===============================
        public IActionResult Print(int id, string format = "fiscal")
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var invoice = _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefault(i => i.Id == id);

            if (invoice == null)
                return NotFound();

            var normalizedFormat = string.Equals(format, "clasico", StringComparison.OrdinalIgnoreCase)
                ? "clasico"
                : "fiscal";

            IDocument document = normalizedFormat == "clasico"
                ? new InvoicePdfClassicDocument(invoice)
                : new InvoicePdfDocument(invoice);
            var pdf = document.GeneratePdf();

            Response.Headers["Content-Disposition"] =
                $"inline; filename=factura_{invoice.Id}.pdf";

            return File(pdf, "application/pdf");
        }

        public IActionResult PrintPos(int id)
        {
            var invoice = _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefault(i => i.Id == id);

            if (invoice == null)
                return NotFound();

            var ticket = BuildPosTicket(invoice);
            var bytes = Encoding.UTF8.GetBytes(ticket);

            Response.Headers["Content-Disposition"] =
                $"inline; filename=factura_{invoice.Id}_pos.txt";

            return File(bytes, "text/plain; charset=utf-8");
        }

        private static string BuildPosTicket(Invoice invoice)
        {
            const int width = 48;
            const int leftMargin = 2;
            const string escInit = "\u001B\u0040";
            const string escBoldOn = "\u001B\u0045\u0001";
            const string escBoldOff = "\u001B\u0045\u0000";
            var lines = new List<string>();
            var genericNcfNumber = GetGenericNcfNumber();
            var company = string.IsNullOrWhiteSpace(invoice.CompanyName)
                ? "Servicios Generales EM"
                : invoice.CompanyName.Trim();

            static string Money(decimal value) => $"RD$ {value:N2}";
            static string Rule(int lineWidth) => new('*', lineWidth);
            static string Center(string value, int lineWidth)
            {
                if (value.Length >= lineWidth)
                    return value;

                var left = (lineWidth - value.Length) / 2;
                return new string(' ', left) + value;
            }
            static string WithMargin(string value, int margin) => new string(' ', margin) + value;

            lines.Add(string.Empty);
            lines.Add(Center(company, width));
            lines.Add(Center($"Factura: PP{invoice.Id:D6}", width));
            lines.Add(Center($"Fecha: {DateTime.Now:dd/MM/yyyy hh:mm tt}", width));
            lines.Add(Center("FACTURA DE CONSUMO", width));
            lines.Add(Center($"NCF: {genericNcfNumber}", width));
            lines.Add(Center("DETALLE DE FACTURA", width));
            lines.Add(string.Empty);
            if (!string.IsNullOrWhiteSpace(invoice.ClientName))
            {
                lines.Add($"Cliente: {invoice.ClientName.Trim()}");
                lines.Add(string.Empty);
                lines.Add(string.Empty);
            }

            lines.Add(Rule(width));
            lines.Add(Rule(width));
            lines.Add(string.Empty);

            foreach (var item in invoice.Items ?? Enumerable.Empty<InvoiceItem>())
            {
                lines.Add(item.Description ?? string.Empty);
                lines.Add($"{item.Quantity:0.####} x {Money(item.Price)}");
                lines.Add(Money(item.Total).PadLeft(width));
                lines.Add(string.Empty);
            }

            var total = (invoice.Items ?? Enumerable.Empty<InvoiceItem>()).Sum(i => i.Total);

            lines.Add(Rule(width));
            lines.Add(Rule(width));
            lines.Add(string.Empty);
            lines.Add($"TOTAL: {Money(total)}".PadLeft(width));
            lines.Add(string.Empty);
            lines.Add(Rule(width));
            lines.Add(Center("Gracias por su compra", width));
            lines.Add(string.Empty);
            lines.Add(string.Empty);

            var content = string.Join(
                Environment.NewLine,
                lines.Select(line => WithMargin(line, leftMargin)));

            return $"{escInit}{escBoldOn}{content}{Environment.NewLine}{escBoldOff}";
        }

        private static string GetGenericNcfNumber()
        {
            var first = RandomNumberGenerator.GetInt32(0, 1_000_000);
            var second = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return $"E{first:D6}{second:D6}";
        }

    }
}

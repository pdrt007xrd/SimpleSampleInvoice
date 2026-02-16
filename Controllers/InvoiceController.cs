using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using SimpleExampleInvoice.Data;
using SimpleExampleInvoice.Models;
using SimpleExampleInvoice.Pdf;
using QuestPDF.Fluent;

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
            var newInvoice = new Invoice
            {
                CreatedAt = DateTime.Now,
                Titulo = string.Empty,
                CompanyName = string.Empty,
                ClientName = string.Empty
            };

            _context.Invoices.Add(newInvoice);
            _context.SaveChanges();
            TempData["InvoiceCreatedMessage"] = $"Factura #{newInvoice.Id} creada exitosamente";

            return RedirectToAction("Edit", new { id = newInvoice.Id });
        }

        public IActionResult Edit(int id = 0, int page = 1)
        {
            const int pageSize = 10;

            if (id == 0)
                return RedirectToAction("Index", "Dashboard");

            var invoice = _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefault(i => i.Id == id);

            if (invoice == null)
                return NotFound();

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
        public IActionResult AddItem(int invoiceId, string description, int quantity, decimal price)
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
        public IActionResult Preview(int id)
        {
            ViewBag.InvoiceId = id;
            return View();
        }

        // ===============================
        // PRINT / DOWNLOAD PDF
        // ===============================
        public IActionResult Print(int id)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var invoice = _context.Invoices
                .Include(i => i.Items)
                .FirstOrDefault(i => i.Id == id);

            if (invoice == null)
                return NotFound();

            var document = new InvoicePdfDocument(invoice);
            var pdf = document.GeneratePdf();

            Response.Headers["Content-Disposition"] =
                $"inline; filename=factura_{invoice.Id}.pdf";

            return File(pdf, "application/pdf");
        }

    }
}

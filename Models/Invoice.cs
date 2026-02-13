using System.ComponentModel.DataAnnotations;

namespace SimpleExampleInvoice.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        public string ClientName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<InvoiceItem> Items { get; set; } = new();
    }
}

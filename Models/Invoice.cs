using System.ComponentModel.DataAnnotations;

namespace SimpleExampleInvoice.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        [Required]
        public string ClientName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<InvoiceItem> Items { get; set; } = new();

       
        [StringLength(150)]
        [Required(ErrorMessage = "El título es obligatorio")]
        public string? Titulo { get; set; }

    }
}

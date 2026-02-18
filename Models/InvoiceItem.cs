namespace SimpleExampleInvoice.Models
{
    public class InvoiceItem
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal Price { get; set; }

        public decimal Total => Quantity * Price;
    }
}

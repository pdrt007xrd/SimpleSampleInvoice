using Microsoft.EntityFrameworkCore;
using SimpleExampleInvoice.Models;

namespace SimpleExampleInvoice.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<InvoiceItem>()
                .Property(x => x.Price)
                .HasPrecision(18, 2);
        }

    }

}

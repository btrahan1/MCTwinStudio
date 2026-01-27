using Microsoft.EntityFrameworkCore;

namespace SalesServer.Data
{
    public class SalesContext : DbContext
    {
        public SalesContext(DbContextOptions<SalesContext> options) : base(options) { }

        public DbSet<Sale> Sales { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
    }

    public class Sale
    {
        public int Id { get; set; }
        public string ProductSku { get; set; }
        public int Quantity { get; set; }
        public DateTime SoldAt { get; set; }
        public bool IsProcessed { get; set; }
    }

    public class InventoryItem
    {
        public int Id { get; set; }
        public int RackIndex { get; set; }
        public int ShelfLevel { get; set; }
        public int BinIndex { get; set; }
        public string ProductSku { get; set; }
        public int Quantity { get; set; }
    }
}

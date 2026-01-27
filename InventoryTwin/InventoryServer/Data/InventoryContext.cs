using Microsoft.EntityFrameworkCore;
using InventoryServer.Models;

namespace InventoryServer.Data;

public class InventoryContext : DbContext
{
    public InventoryContext(DbContextOptions<InventoryContext> options) : base(options) { }

    public DbSet<InventoryItem> InventoryItems { get; set; }
}

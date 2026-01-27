using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CashRecyclerServer.Data
{
    public class CashDbContext : DbContext
    {
        public CashDbContext(DbContextOptions<CashDbContext> options) : base(options) { }

        public DbSet<Region> Regions { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Recycler> Recyclers { get; set; }
        public DbSet<Cassette> Cassettes { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Region>().ToTable("Dim_Regions");
            modelBuilder.Entity<Region>()
                .HasMany(r => r.Locations)
                .WithOne() // Location doesn't have a Region navigation property, so this is fine
                .HasForeignKey(l => l.RegionId);

            modelBuilder.Entity<Location>().ToTable("Dim_Locations");
            modelBuilder.Entity<Location>()
                .HasMany(l => l.Recyclers)
                .WithOne(r => r.Location) // Explicitly map to the new navigation property
                .HasForeignKey(r => r.LocationId);

            modelBuilder.Entity<Recycler>().ToTable("Dim_Recyclers");
            modelBuilder.Entity<Recycler>()
                .HasMany(r => r.Cassettes)
                .WithOne()
                .HasForeignKey(c => c.RecyclerId);

            modelBuilder.Entity<Cassette>().ToTable("Fact_Cassettes")
                .HasKey(c => new { c.RecyclerId, c.CassetteIndex });
            modelBuilder.Entity<Cassette>()
                .Property(c => c.Denomination).HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Transaction>().ToTable("Fact_Transactions")
                .Property(t => t.TotalAmount).HasColumnType("decimal(18,2)");
        }
    }

    public class Region
    {
        [Key] public int RegionId { get; set; }
        public required string Name { get; set; }
        public List<Location> Locations { get; set; } = new();
    }

    public class Location
    {
        [Key] public int LocationId { get; set; }
        public int RegionId { get; set; }
        public required string Name { get; set; }
        public string? Address { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public List<Recycler> Recyclers { get; set; } = new();
    }

    public class Recycler
    {
        [Key] public int RecyclerId { get; set; }
        public int LocationId { get; set; }
        public required string Name { get; set; }
        public string? SerialNumber { get; set; }
        public string? Model { get; set; }
        public string? Status { get; set; } // Online, Offline
        public List<Cassette> Cassettes { get; set; } = new();
        [ForeignKey("LocationId")]
        public Location? Location { get; set; }
    }

    public class Cassette
    {
        public int RecyclerId { get; set; }
        public int CassetteIndex { get; set; }
        public required string Type { get; set; } // Recycle, Deposit, Reject
        public decimal Denomination { get; set; }
        public int CurrentCount { get; set; }
        public int MaxCapacity { get; set; }
        public string? Status { get; set; }
    }

    public class Transaction
    {
        [Key] public int TransactionId { get; set; }
        public int RecyclerId { get; set; }
        public DateTime Timestamp { get; set; }
        public required string Type { get; set; }
        public decimal TotalAmount { get; set; }
    }
}

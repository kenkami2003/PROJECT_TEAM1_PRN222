
using Microsoft.EntityFrameworkCore;

namespace BoardingHouseManagement.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Guardian> Guardians { get; set; }
        public DbSet<TemporaryGuest> TemporaryGuests { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<AssetCategory> AssetCategories { get; set; }
        public DbSet<RoomAsset> RoomAssets { get; set; }
        public DbSet<UtilityReading> UtilityReadings { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ContractTerm> ContractTerms { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ServiceFee> ServiceFees { get; set; }
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
        public DbSet<CompensationLog> CompensationLogs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasIndex(x => x.Username).IsUnique();
            modelBuilder.Entity<Role>().HasIndex(x => x.RoleName).IsUnique();
            modelBuilder.Entity<Room>().HasIndex(x => new { x.BuildingId, x.RoomNumber }).IsUnique();
            modelBuilder.Entity<Contract>().HasIndex(x => x.ContractCode).IsUnique();
            modelBuilder.Entity<Invoice>().HasIndex(x => x.InvoiceCode).IsUnique();
        }
    }
}

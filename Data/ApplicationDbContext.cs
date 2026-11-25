using FoodOrderingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace FoodOrderingSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<MenuItem> MenuItems { get; set; } = null!;
        public DbSet<DiningTable> DiningTables { get; set; } = null!;
        public DbSet<Reservation> Reservations { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<WishlistItem> WishlistItems { get; set; } = null!;
        public DbSet<VoucherCode> VoucherCodes { get; set; } = null!;
        public DbSet<RewardItem> RewardItems { get; set; } = null!;
        public DbSet<Redemption> Redemptions { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<MenuItemImage> MenuItemImages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // NOTE: Unique constraints (Username, Email, VoucherCode.Code, WishlistItem
            // composite, Review.OrderItemId) are declared via the [Index] data annotation
            // directly on the entity classes - not here - per the assignment's "data
            // annotations, not Fluent API" rule. Fluent API below is reserved only for
            // delete-behavior configuration, which has no data-annotation equivalent in
            // EF Core.

            // Prevent cascade-delete cycles across the order graph
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(u => u.Orders)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Table)
                .WithMany(t => t.Orders)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Customer)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.MenuItem)
                .WithMany(m => m.OrderItems)
                .OnDelete(DeleteBehavior.Restrict);

            // A customer can only wishlist the same dish once (unique index via [Index] attribute)
            modelBuilder.Entity<WishlistItem>()
                .HasOne(w => w.Customer)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WishlistItem>()
                .HasOne(w => w.MenuItem)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade);

            // Voucher codes uniqueness is enforced via the [Index] attribute on VoucherCode
            modelBuilder.Entity<Redemption>()
                .HasOne(r => r.Customer)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Redemption>()
                .HasOne(r => r.RewardItem)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // One review per ordered dish - uniqueness via [Index] attribute; the one-to-one
            // shape below still needs Fluent API since there's no annotation for WithOne()
            modelBuilder.Entity<Review>()
                .HasOne(r => r.OrderItem)
                .WithOne()
                .HasForeignKey<Review>(r => r.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.MenuItem)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Customer)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict);

            // Photos are deleted along with their menu item
            modelBuilder.Entity<MenuItemImage>()
                .HasOne(i => i.MenuItem)
                .WithMany(m => m.Images)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

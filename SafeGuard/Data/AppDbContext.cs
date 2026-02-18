using Microsoft.EntityFrameworkCore;
using SafeGuard.API.Models;
using SafeGuard.Models; 

namespace SafeGuard.Data 
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Helper> Helpers { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Helper tablosundaki UserId ilişkisi için silme kuralını kapatıyoruz
            modelBuilder.Entity<Helper>()
                .HasOne(h => h.User)
                .WithMany()
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Restrict); // <-- ÖNEMLİ OLAN BURASI

            // Helper tablosundaki HelperId ilişkisi için de kapatıyoruz
            modelBuilder.Entity<Helper>()
                .HasOne(h => h.HelperUser)
                .WithMany()
                .HasForeignKey(h => h.HelperId)
                .OnDelete(DeleteBehavior.Restrict); // <-- ÖNEMLİ OLAN BURASI
        }
    }
}
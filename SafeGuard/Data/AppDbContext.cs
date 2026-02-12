using Microsoft.EntityFrameworkCore;
using SafeGuard.API.Models;
using SafeGuard.Models; // <-- DİKKAT: SafeGuard.Models kullanıyoruz

namespace SafeGuard.Data // <-- DİKKAT: Sadece SafeGuard.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Contact> Contacts { get; set; }
    }
}
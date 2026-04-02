using Microsoft.EntityFrameworkCore;

namespace TourGuideAdmin.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // 👇 Đổi dòng này thành DbSet<POI>
        public DbSet<POI> POIs { get; set; }
        public DbSet<Tour> Tours { get; set; }
    }
}
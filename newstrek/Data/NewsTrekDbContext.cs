using newstrek.Models;
using Microsoft.EntityFrameworkCore;

namespace newstrek.Data
{
    public class NewsTrekDbContext : DbContext
    {
        public NewsTrekDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<News> News { get; set; }
    }
}

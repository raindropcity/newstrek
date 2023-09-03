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
        public DbSet<User> Users { get; set; }
        public DbSet<InterestedTopic> InterestedTopics { get; set; }
        public DbSet<Vocabulary> Vocabularies { get; set; }
    }
}

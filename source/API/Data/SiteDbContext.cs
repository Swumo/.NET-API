using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Saitynai2.Auth.Model;
using Saitynai2.Data.Entities;

namespace Saitynai2.Data
{
    public class SiteDbContext : IdentityDbContext<SiteRestUser>
    {
        public DbSet<Country> Countries { get; set; }
        public DbSet<Place> Places { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public readonly IConfiguration _configuration;

        public SiteDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_configuration.GetConnectionString("PostgreSQL"));
        }
    }
}

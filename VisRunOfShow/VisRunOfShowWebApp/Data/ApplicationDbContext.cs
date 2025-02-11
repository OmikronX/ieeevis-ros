using Microsoft.EntityFrameworkCore;
using IeeeVisRunOfShowWebApp.Models;

namespace IeeeVisRunOfShowWebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<TestTable> TestTable { get; set; }
    }
}

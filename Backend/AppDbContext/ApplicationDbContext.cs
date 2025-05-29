using Backend.Models;
using Microsoft.EntityFrameworkCore;
namespace Backend.AppDbContext
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {

        }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<ApiTest> ApiTests { get; set; }
        public DbSet<SqlTest> SqlTests { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        protected ApplicationDbContext()
        {
        }
    }
}

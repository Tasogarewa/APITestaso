using Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace Backend.AppDbContext
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {

        }
     
        public DbSet<ApiTest> ApiTests { get; set; }
        public DbSet<SqlTest> SqlTests { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        protected ApplicationDbContext()
        {
        }
    }
}

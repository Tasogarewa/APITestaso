using Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
namespace Backend.AppDbContext
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApiTest>()
                .Property(e => e.Headers)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null)
                );

            modelBuilder.Entity<ApiTest>()
                .Property(e => e.BodyJson)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<object>(v, (JsonSerializerOptions?)null)
                );
        }
        public DbSet<ApiTest> ApiTests { get; set; }
        public DbSet<SqlTest> SqlTests { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        protected ApplicationDbContext()
        {
        }
    }
}

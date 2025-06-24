using Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
            var dictionaryConverter = new ValueConverter<Dictionary<string, string>, string>(
            dict => JsonSerializer.Serialize(dict, (JsonSerializerOptions)null),
            json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions)null) ?? new Dictionary<string, string>()
        );
            modelBuilder.Entity<ApiTest>()
                .Property(e => e.Headers)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null)
                );
            modelBuilder.Entity<ApiTest>()
              .Property(e => e.QueryParameters)
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
            modelBuilder.Entity<ApiTest>(entity =>
            {
                entity.Property(e => e.Save)
                      .HasConversion(dictionaryConverter);
                entity.Property(e => e.Save)
      .HasConversion(dictionaryConverter)
      .HasColumnType("nvarchar(max)");
            });

        }
        public DbSet<ApiTest> ApiTests { get; set; }
        public DbSet<SqlTest> SqlTests { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<ApiTestScenario> ApiTestScenarios { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        protected ApplicationDbContext()
        {
        }
    }
}

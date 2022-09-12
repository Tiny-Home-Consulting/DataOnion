using Microsoft.EntityFrameworkCore;

namespace SampleApp.db
{
    public class ApplicationContext : DbContext
    {
        private string _connectionString => "";
        public ApplicationContext(
            DbContextOptions options
        )
            : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
            }

            protected override void OnConfiguring(DbContextOptionsBuilder builder)
            {
                builder.UseNpgsql(_connectionString)
                    .EnableSensitiveDataLogging()
                    .UseSnakeCaseNamingConvention();
            }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Borland.EF.Tests
{
    public class TestContext : DbContext
    {
        private static readonly LoggerFactory _logger =
            new LoggerFactory(new[] { new ConsoleLoggerProvider() });

        public DbSet<Blog> Blogs { get; set; }

        public DbSet<Post> Posts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .EnableSensitiveDataLogging(true)
                .UseLoggerFactory(_logger)
                .UseLazyQueryable()
                .UseInMemoryDatabase("Test DB");
    }
}

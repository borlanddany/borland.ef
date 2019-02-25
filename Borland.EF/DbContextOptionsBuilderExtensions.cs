using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Borland.EF
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder UseLazyQueryable(this DbContextOptionsBuilder builder)
        {
            var extension = builder.Options.FindExtension<BorlandDbContextOptionsExtension>()
                            ?? new BorlandDbContextOptionsExtension();

            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(extension);

            return builder;
        }
    }
}

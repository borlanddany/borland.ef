using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Borland.EF
{
    public class BorlandDbContextOptionsExtension : IDbContextOptionsExtension
    {
        public string LogFragment => "using queryable";

        public bool ApplyServices(IServiceCollection services)
        {
            new EntityFrameworkServicesBuilder(services)
                .TryAdd<IModelSource, BorlandModelSource>();
            return false;
        }

        public long GetServiceProviderHashCode() => 545;

        public void Validate(IDbContextOptions options) { }
    }
}

using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DataOnion.Config;
using DataOnion.db;

namespace DataOnion
{
    public static class DataHelperExtensions
    {
        public static IServiceCollection ConfigureEfCoreOnion<TDbContext>(
            this IServiceCollection serviceCollection,
            EFCoreOptions? options = null
        )
            where TDbContext : DbContext
        {
            serviceCollection.AddDbContext<TDbContext>(
                options?.DataConnector?.Invoke(options.ConnectionString),
                options?.ServiceLifetime ?? ServiceLifetime.Scoped,
                options?.OptionsLifetime ?? ServiceLifetime.Scoped
            );

            serviceCollection.AddScoped<IEFCoreService<TDbContext>, EFCoreService<TDbContext>>();

            return serviceCollection;
        }

        public static IServiceCollection ConfigureDapperOnion<TDbConnection>(
            this IServiceCollection serviceCollection,
            DapperOptions<TDbConnection>? options = null
        )
            where TDbConnection : DbConnection
        {
            // If no options are passed in, assume the DBConnection has already been registered
            if (options != null)
            {
                serviceCollection.AddScoped<TDbConnection>(
                    services => options.ConnectionGetter(options.ConnectionString)
                );
            }

            serviceCollection.AddScoped<IDapperService<TDbConnection>, DapperService<TDbConnection>>();
            
            return serviceCollection;
        }
    }
}
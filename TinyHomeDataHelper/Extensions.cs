using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TinyHomeDataHelper.Config;
using TinyHomeDataHelper.db;

namespace TinyHomeDataHelper
{
    public static class DataHelperExtensions
    {
        public static IServiceCollection ConfigureEfCoreDataHelper<TDbContext>(
            this IServiceCollection serviceCollection,
            TinyHomeDataHelperOptions? options = null
        )
            where TDbContext : DbContext
        {
            serviceCollection.AddDbContext<TDbContext>(
                options?.EFCore?.DataConnector?.Invoke(options.DatabaseConnectionString),
                options?.EFCore?.ServiceLifetime ?? ServiceLifetime.Scoped,
                options?.EFCore?.OptionsLifetime ?? ServiceLifetime.Scoped
            );

            serviceCollection.AddScoped<IEFCoreService<TDbContext>, EFCoreService<TDbContext>>();

            return serviceCollection;
        }

        public static IServiceCollection ConfigureDapperDataHelper<TDbConnection>(
            this IServiceCollection serviceCollection,
            TinyHomeDataHelperOptions<TDbConnection>? options = null
        )
            where TDbConnection : DbConnection
        {
            if (options == null || String.IsNullOrWhiteSpace(options.DatabaseConnectionString))
            {
                serviceCollection.AddScoped<IDapperService<TDbConnection>, DapperService<TDbConnection>>();

                return serviceCollection;
            }

            serviceCollection.AddScoped<IDapperService<TDbConnection>, DapperService<TDbConnection>>(
                o => new DapperService<TDbConnection>(
                    // We need the ! here because the compiler is seemingly dumb.
                    options!.DapperOptions!.ConnectionGetter!(options!.DatabaseConnectionString)
                )
            );

            return serviceCollection;
        }

        public static IServiceCollection ConfigureDataHelper<TDbContext, TDbConnection>(
            this IServiceCollection serviceCollection,
            TinyHomeDataHelperOptions<TDbConnection>? options = null
        )
            where TDbContext : DbContext
            where TDbConnection : DbConnection
        {
            serviceCollection.ConfigureEfCoreDataHelper<TDbContext>(options);
            serviceCollection.ConfigureDapperDataHelper<TDbConnection>(
                options
            );


            return serviceCollection;
        }
    }
}
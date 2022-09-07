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
            this IServiceCollection serviceCollection
        )
            where TDbConnection : DbConnection
        {
            serviceCollection.AddScoped<IDapperService<TDbConnection>, DapperService<TDbConnection>>();

            return serviceCollection;
        }

        public static IServiceCollection ConfigureDataHelper<TDbContext, TDbConnection>(
            this IServiceCollection serviceCollection,
            TinyHomeDataHelperOptions? options = null
        )
            where TDbContext : DbContext
            where TDbConnection : DbConnection
        {
            serviceCollection.ConfigureEfCoreDataHelper<TDbContext>(options);
            serviceCollection.ConfigureDapperDataHelper<TDbConnection>();


            return serviceCollection;
        }
    }
}
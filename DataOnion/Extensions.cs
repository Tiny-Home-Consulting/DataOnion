using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DataOnion.db;

namespace DataOnion
{
    public interface IFluentDatabaseOnion : IFluentDapperSetup, IFluentEfCoreSetup {}

    public interface IFluentDapperSetup
    {
        IFluentEfCoreSetup ConfigureEfCore<T>(
            Func<string, Action<DbContextOptionsBuilder>>? dataConnector,
            ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
            ServiceLifetime optionsLifetime = ServiceLifetime.Scoped
        ) where T : DbContext;
    }

    public interface IFluentEfCoreSetup
    {
        IFluentDapperSetup ConfigureDapper<T>(
            Func<string, T> connectionGetter
        ) where T : DbConnection;
    }

    public class FluentDatabaseOnion : IFluentDatabaseOnion
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly string _connectionString;

        public FluentDatabaseOnion(
            IServiceCollection serviceCollection,
            string connectionString
        )
        {
            _serviceCollection = serviceCollection;
            _connectionString = connectionString;
        }

        public IFluentDapperSetup ConfigureDapper<T>(
            Func<string, T> connectionGetter
        ) where T: DbConnection
        {
            _serviceCollection.AddScoped<T>(
                services => connectionGetter(_connectionString)
            );

            return this;
        }

        public IFluentEfCoreSetup ConfigureEfCore<T>(
            Func<string, Action<DbContextOptionsBuilder>>? dataConnector,
            ServiceLifetime contextLifetime = ServiceLifetime.Scoped,
            ServiceLifetime optionsLifetime = ServiceLifetime.Scoped
        ) where T : DbContext
        {
            _serviceCollection.AddDbContext<T>(
                dataConnector?.Invoke(_connectionString),
                contextLifetime,
                optionsLifetime
            );

            _serviceCollection.AddScoped<IEFCoreService<T>, EFCoreService<T>>();

            return this;
        }
    }

    public static class DataHelperExtensions
    {
        public static IFluentDatabaseOnion? DatabaseOnion;
        public static IFluentDatabaseOnion AddDatabaseOnion(
            this IServiceCollection serviceCollection,
            string connectionString
        )
        {
            DatabaseOnion = new FluentDatabaseOnion(
                serviceCollection,
                connectionString
            );

            return DatabaseOnion;
        }
    }
}
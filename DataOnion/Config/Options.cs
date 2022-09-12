using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataOnion.Config
{
    public class DapperOptions<TDbConnection>
        where TDbConnection : DbConnection
    {
        public Func<string, TDbConnection> ConnectionGetter { get; }
        public string ConnectionString { get; }

        public DapperOptions(
            string connectionString,
            Func<string, TDbConnection> connectionGetter
        )
        {
            ConnectionGetter = connectionGetter;
            ConnectionString = connectionString;
        }
    }

    public class EFCoreOptions
    {
        public string ConnectionString { get; }
        public Func<string, Action<DbContextOptionsBuilder>> DataConnector;
        public ServiceLifetime? ServiceLifetime { get; } = null;
        public ServiceLifetime? OptionsLifetime { get; } = null;

        public EFCoreOptions(
            string connectionString,
            Func<string, Action<DbContextOptionsBuilder>> dataConnector,
            ServiceLifetime? serviceLifetime = null,
            ServiceLifetime? optionsLifetime = null
        )
        {
            ConnectionString = connectionString;
            DataConnector = dataConnector;
            ServiceLifetime = serviceLifetime;
            OptionsLifetime = optionsLifetime;
        }
    }
}

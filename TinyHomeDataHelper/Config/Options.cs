using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace TinyHomeDataHelper.Config
{
    public class TinyHomeDataHelperOptions
    {
        public EFCoreOptions? EFCore { get; set; }
        public string DatabaseConnectionString { get; }

        public TinyHomeDataHelperOptions(
            string databaseConnectionString
        )
        {
            DatabaseConnectionString = databaseConnectionString;
        }
    }

    public class TinyHomeDataHelperOptions<TDbConnection> : TinyHomeDataHelperOptions
        where TDbConnection : DbConnection
    {
        public DapperOptions<TDbConnection>? DapperOptions { get; set; }
        
        public TinyHomeDataHelperOptions(string databaseConnectionString) 
            : base (databaseConnectionString) 
        {
        }
    }

    public class DapperOptions<TDbConnection>
        where TDbConnection : DbConnection
    {
        public Func<string, TDbConnection>? ConnectionGetter { get; } = null;

        public DapperOptions(
            Func<string, TDbConnection>? connectionGetter = null
        )
        {
            ConnectionGetter = connectionGetter;
        }
    }

    public class EFCoreOptions
    {
        public Func<string, Action<DbContextOptionsBuilder>>? DataConnector = null;
        public ServiceLifetime? ServiceLifetime { get; } = null;
        public ServiceLifetime? OptionsLifetime { get; } = null;

        public EFCoreOptions(
            Func<string, Action<DbContextOptionsBuilder>>? dataConnector = null,
            ServiceLifetime? serviceLifetime = null,
            ServiceLifetime? optionsLifetime = null
        )
        {
            DataConnector = dataConnector;
            ServiceLifetime = serviceLifetime;
            OptionsLifetime = optionsLifetime;
        }
    }
}

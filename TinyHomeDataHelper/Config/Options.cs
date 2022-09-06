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

    public class EFCoreOptions
    {
        public Func<string, DbContextOptionsBuilder, DbContextOptionsBuilder> DataConnector;
        public ServiceLifetime? ServiceLifetime { get; } = null;
        public ServiceLifetime? OptionsLifetime { get; } = null;

        public EFCoreOptions(
            Func<string, DbContextOptionsBuilder, DbContextOptionsBuilder> dataConnector,
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

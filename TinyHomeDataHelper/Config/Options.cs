using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace TinyHomeDataHelper.Config
{
    public class TinyHomeDataHelperOptions
    {
        public EFCoreOptions? EFCore { get; set; }
    }

    public class EFCoreOptions
    {
        public Action<IServiceProvider, DbContextOptionsBuilder>? OptionsAction { get; }
        public ServiceLifetime ServiceLifetime { get; }
        public ServiceLifetime OptionsLifetime { get; }

        public EFCoreOptions(
            Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction,
            ServiceLifetime serviceLifetime = ServiceLifetime.Scoped,
            ServiceLifetime optionsLifetime = ServiceLifetime.Scoped
        )
        {
            OptionsAction = optionsAction;
            ServiceLifetime = serviceLifetime;
            OptionsLifetime = optionsLifetime;
        }
    }
}
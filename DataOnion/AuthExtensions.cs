using Microsoft.Extensions.DependencyInjection;
using DataOnion.Auth;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace DataOnion
{
    public interface IFluentAuthOnion : 
        IFluentRedisOnion, 
        IFluentAuthStategyOnion,
        IFluentTwoFactorAuthOnion {}

    public interface IFluentRedisOnion
    {
        IFluentAuthOnion ConfigureRedis(string connectionString);
    }

    public interface IFluentAuthStategyOnion
    {
        IFluentAuthOnion ConfigureSlidingExpiration<T>(
            TimeSpan expiration,
            TimeSpan? absoluteExpiration,
            string authPrefix,
            Func<HashEntry[], T> makeFromHash,
            string expirationKey = "expiration"
        ) where T : class, IAuthStorable<T>;

        IFluentAuthOnion AddCustomAuthStrategy<T>(IAuthServiceStrategy<T> authStrategy);
    }

    public interface IFluentTwoFactorAuthOnion
    {
        IFluentAuthOnion ConfigureTwoFactorAuth(
            string envPrefix,
            string twoFactorAuthPrefix
        );
    }

    public class FluentAuthOnion : IFluentAuthOnion
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly string _environmentPrefix;

        public FluentAuthOnion(
            IServiceCollection serviceCollection,
            string environmentPrefix
        )
        {
            _serviceCollection = serviceCollection;
            _environmentPrefix = environmentPrefix;
        }

        public IFluentAuthOnion ConfigureRedis(string connectionString)
        {
            _serviceCollection.AddSingleton<IRedisManager>(provider =>
                new RedisManager(
                    connectionString,
                    provider.GetService<ILogger<RedisManager>>()
                )
            );

            return this;
        }

        public IFluentAuthOnion ConfigureSlidingExpiration<T>(
            TimeSpan slidingExpiration,
            TimeSpan? absoluteExpiration,
            string authPrefix,
            Func<HashEntry[], T> makeFromHash,
            string expirationKey = "expiration"
        )
            where T : class, IAuthStorable<T>
        {
            _serviceCollection.AddScoped(typeof(IAuthService<>), typeof(AuthService<>));
            _serviceCollection.AddScoped(typeof(IAuthServiceStrategy<>), typeof(SlidingExpirationStrategy<>));

            _serviceCollection.AddSingleton<SlidingExpirationConfig<T>>(new SlidingExpirationConfig<T>(
                slidingExpiration,
                absoluteExpiration,
                expirationKey,
                _environmentPrefix,
                authPrefix,
                makeFromHash
            ));

            return this;
        }

        public IFluentAuthOnion AddCustomAuthStrategy<T>(IAuthServiceStrategy<T> authStrategy)
        {
            _serviceCollection.AddScoped(typeof(IAuthService<>), typeof(AuthService<>));
            _serviceCollection.AddSingleton<IAuthServiceStrategy<T>>(authStrategy);

            return this;
        }

        public IFluentAuthOnion ConfigureTwoFactorAuth(
            string envPrefix,
            string twoFactorAuthPrefix
        )
        {
            _serviceCollection.AddScoped<ITwoFactorRedisContext, TwoFactorRedisContext>( provider =>
                new TwoFactorRedisContext(
                    envPrefix,
                    twoFactorAuthPrefix,
                    provider.GetService<IRedisManager>(),
                    provider.GetService<ILogger<TwoFactorRedisContext>>()
                )
            );
            _serviceCollection.AddScoped<ITwoFactorAuthService, TwoFactorAuthService>();

            return this;
        }
    }

    public static class AuthHelperExtensions
    {
        public static IFluentAuthOnion AddAuthOnion(
            this IServiceCollection serviceCollection,
            string environmentPrefix
        )
        {
            return new FluentAuthOnion(
                serviceCollection,
                environmentPrefix
            );
        }
    }
}
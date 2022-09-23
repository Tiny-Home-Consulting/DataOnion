using Microsoft.Extensions.DependencyInjection;
using DataOnion.Auth;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace DataOnion
{
    public interface IFluentAuthOnion : IFluentRedisOnion, IFluentAuthStategyOnion {}

    public interface IFluentRedisOnion
    {
        IFluentAuthStategyOnion ConfigureRedis(string connectionString);
    }

    public interface IFluentAuthStategyOnion
    {
        IFluentRedisOnion ConfigureSlidingExpiration<T>(
            TimeSpan expiration,
            TimeSpan? absoluteExpiration,
            string authPrefix,
            Func<HashEntry[], T> makeFromHash,
            string expirationKey = "expiration"
        ) where T : class, IAuthStorable<T>;

        IFluentRedisOnion AddCustomAuthStrategy<T>(IAuthServiceStrategy<T> authStrategy);
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

        public IFluentAuthStategyOnion ConfigureRedis(string connectionString)
        {
            _serviceCollection.AddSingleton<IRedisManager>(provider =>
                new RedisManager(
                    connectionString,
                    provider.GetService<ILogger<RedisManager>>()
                )
            );

            return this;
        }

        public IFluentRedisOnion ConfigureSlidingExpiration<T>(
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

        public IFluentRedisOnion AddCustomAuthStrategy<T>(IAuthServiceStrategy<T> authStrategy)
        {
            _serviceCollection.AddScoped(typeof(IAuthService<>), typeof(AuthService<>));
            _serviceCollection.AddSingleton<IAuthServiceStrategy<T>>(authStrategy);

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
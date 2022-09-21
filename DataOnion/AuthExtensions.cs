using Microsoft.Extensions.DependencyInjection;
using DataOnion.Auth;
using StackExchange.Redis;

namespace DataOnion
{
    public interface IFluentAuthOnion
    {
        IFluentAuthOnion ConfigureSlidingExpiration<T>(
            TimeSpan expiration,
            TimeSpan? absoluteExpiration,
            string authPrefix,
            Func<HashEntry[], T> makeFromHash,
            string expirationKey = "expiration"
        )
            where T : class, IAuthStorable<T>;

        IFluentAuthOnion ConfigureRedis(string connectionString);
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
            _serviceCollection.AddSingleton<IRedisManager>(new RedisManager(connectionString));

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
            _serviceCollection.AddScoped(typeof(IAuthServiceStrategy<>), typeof(SlidingExpirationStrategy<>));

            _serviceCollection.AddSingleton<SlidingExpirationConfig<T>>(new SlidingExpirationConfig<T>
            {
                AbsoluteExpiration = absoluteExpiration,
                AuthPrefix = authPrefix,
                ConstructFromHash = makeFromHash,
                EnvironmentPrefix = _environmentPrefix,
                ExpirationKey = expirationKey,
                SlidingExpiration = slidingExpiration
            });

            _serviceCollection.AddScoped<IAuthService<T>>(provider =>
                new AuthService<T>(provider.GetRequiredService<IAuthServiceStrategy<T>>())
            );

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
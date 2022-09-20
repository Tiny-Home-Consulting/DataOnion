using Microsoft.Extensions.DependencyInjection;
using DataOnion.Auth;
using StackExchange.Redis;

namespace DataOnion
{
    public class RedisConnectionFailedException : RedisException
    {
        public RedisConnectionFailedException(
            string? message = null
        ) : base(message ?? $"Failed to establish Redis connection.")
        {
        }
    }

    public interface IFluentAuthOnion
    {
        IFluentAuthOnion ConfigureSlidingExpiration<T>(
            TimeSpan expiration,
            Func<HashEntry[], T> makeFromHash
        )
            where T : class, IAuthStorable<T>;

        IFluentAuthOnion ConfigureRedis(string connectionString);
    }

    public class FluentAuthOnion : IFluentAuthOnion
    {
        private readonly IServiceCollection _serviceCollection;
        private ConnectionMultiplexer? _existingConnection = null;

        public FluentAuthOnion(
            IServiceCollection serviceCollection
        )
        {
            _serviceCollection = serviceCollection;

            _serviceCollection.AddScoped(typeof(IAuthService<>), typeof(AuthService<>));
        }

        public IFluentAuthOnion ConfigureRedis(string connectionString)
        {
            // The `_existingConnection` object is a singleton, in a sense, since it's a stored property here.
            // I don't know how much it matters which service lifetime is chosen.
            _serviceCollection.AddTransient<IDatabase>(_ => {
                try
                {
                    if (_existingConnection == null || !_existingConnection.IsConnected)
                    {
                        _existingConnection = ConnectionMultiplexer.Connect(connectionString);
                    }

                    return _existingConnection.GetDatabase();
                }
                catch (RedisException e)
                {
                    throw new RedisConnectionFailedException(e.Message);
                }
            });

            return this;
        }

        public IFluentAuthOnion ConfigureSlidingExpiration<T>(
            TimeSpan expiration,
            Func<HashEntry[], T> makeFromHash
        )
            where T : class, IAuthStorable<T>
        {
            _serviceCollection.AddScoped<IAuthServiceStrategy<T>>(provider =>
                new SlidingExpirationStrategy<T>(
                    provider.GetRequiredService<IDatabase>(),
                    expiration,
                    makeFromHash
                )
            );

            return this;
        }
    }

    public static class AuthHelperExtensions
    {
        public static IFluentAuthOnion AddAuthOnion(
            this IServiceCollection serviceCollection
        )
        {
            return new FluentAuthOnion(
                serviceCollection
            );
        }
    }
}
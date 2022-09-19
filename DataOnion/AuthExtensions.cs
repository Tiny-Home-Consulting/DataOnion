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
        IFluentAuthOnion ConfigureSlidingExpiration(
            TimeSpan expiration
        );
    }

    public class FluentAuthOnion : IFluentAuthOnion
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly string _connectionString;
        private ConnectionMultiplexer? _existingConnection = null;

        private ConnectionMultiplexer _connection
        {
            get
            {
                try
                {
                    if (_existingConnection == null || !_existingConnection.IsConnected)
                    {
                        _existingConnection = ConnectionMultiplexer.Connect(_connectionString);
                    }

                    return _existingConnection;
                }
                catch (RedisException e)
                {
                    throw new RedisConnectionFailedException(e.Message);
                }
            }
        }
        private IDatabase _database => _connection.GetDatabase();

        public FluentAuthOnion(
            IServiceCollection serviceCollection,
            string connectionString
        )
        {
            _serviceCollection = serviceCollection;
            _connectionString = connectionString;

            _serviceCollection.AddSingleton<IDatabase>(_database);
        }

        public IFluentAuthOnion ConfigureSlidingExpiration(
            TimeSpan expiration
        )
        {
            

            return this;
        }
    }

    public static class AuthHelperExtensions
    {
        public static IFluentAuthOnion AddAuthOnion(
            this IServiceCollection serviceCollection,
            string connectionString
        )
        {
            return new FluentAuthOnion(
                serviceCollection,
                connectionString
            );
        }
    }
}
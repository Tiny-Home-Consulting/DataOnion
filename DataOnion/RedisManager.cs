using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace DataOnion
{
    public interface IRedisManager
    {
        IDatabase GetDatabase();
    }

    public class RedisManager : IRedisManager
    {
        private readonly string _connectionString;
        private readonly ILogger? _logger;
        private ConnectionMultiplexer? _existingConnection = null;

        private ConnectionMultiplexer _connection
        {
            get
            {
                if (_existingConnection == null || !_existingConnection.IsConnected)
                {
                    _logger?.LogDebug(
                        "Redis connection not established; connecting to '{0}'",
                        _connectionString
                    );

                    try
                    {
                        _existingConnection = ConnectionMultiplexer.Connect(_connectionString);
                    }
                    catch (RedisException e)
                    {
                        _logger?.LogError(
                            e,
                            "Error connecting to Redis. See exception message for details."
                        );
                        throw;
                    }

                    _logger?.LogDebug(
                        "Successfully connected to Redis."
                    );
                }

                return _existingConnection;
            }
        }

        public RedisManager(
            string connectionString,
            ILogger? logger
        )
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public IDatabase GetDatabase() => _connection.GetDatabase();
    }
}
using StackExchange.Redis;

namespace DataOnion
{
    public interface IRedisManager
    {
        IDatabase GetDatabase();
    }

    public class RedisManager : IRedisManager
    {
        private readonly string _connectionString;
        private ConnectionMultiplexer? _existingConnection = null;

        private ConnectionMultiplexer _connection
        {
            get
            {
                if (_existingConnection == null || !_existingConnection.IsConnected)
                {
                    _existingConnection = ConnectionMultiplexer.Connect(_connectionString);
                }

                return _existingConnection;
            }
        }

        public RedisManager(
            string connectionString
        )
        {
            _connectionString = connectionString;
        }

        public IDatabase GetDatabase() => _connection.GetDatabase();
    }
}
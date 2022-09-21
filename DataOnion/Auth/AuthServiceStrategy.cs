using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace DataOnion.Auth
{
    internal interface IAuthServiceStrategy<T>
    {
        Task<T?> LoginAsync(T userSession);
        Task<T?> GetSessionAsync(string id);
        Task LogoutAsync(string id);
    }

    public interface IAuthStorable<T> : IEquatable<T>
    {
        string GetId();
        IEnumerable<HashEntry> ToRedisHash();
    }

    public class SlidingExpirationConfig<T>
    {
        public TimeSpan SlidingExpiration { get; set; }
        public TimeSpan? AbsoluteExpiration { get; set; }
        public string ExpirationKey { get; set; }
        public string EnvironmentPrefix { get; set; }
        public string AuthPrefix { get; set; }
        public Func<HashEntry[], T> ConstructFromHash { get; set; }
    }

    public class SlidingExpirationStrategy<T> : IAuthServiceStrategy<T>
        where T : class, IAuthStorable<T>
    {
        private readonly IRedisManager _redisManager;
        private readonly SlidingExpirationConfig<T> _config;
        private readonly ILogger? _logger; // currently unused

        private IDatabase _database => _redisManager.GetDatabase();
        private string _expirationTimeStr => DateTimeOffset.UtcNow
            .Add(_config.SlidingExpiration)
            .ToUnixTimeSeconds()
            .ToString();

        // Making the logger default to null means it won't throw an exception if there is no logger in DI.
        public SlidingExpirationStrategy(
            IRedisManager redisManager,
            SlidingExpirationConfig<T> config,
            ILogger? logger = null
        )
        {
            _redisManager = redisManager;
            _config = config;
            _logger = logger;
        }

        private async Task SetExpirationAsync(RedisKey key)
        {
            await _database.HashSetAsync(
                key,
                _config.ExpirationKey,
                _expirationTimeStr
            );
        }

        private RedisKey BuildKey(string id) => new($"{_config.EnvironmentPrefix}_${_config.AuthPrefix}_${id}");

        public async Task<T?> LoginAsync(T userSession)
        {
            var id = BuildKey(userSession.GetId());
            var redisHash = await _database.HashGetAllAsync(id);

            if (redisHash.Length == 0)
            {
                await _database.HashSetAsync(
                    id,
                    userSession
                        .ToRedisHash()
                        .Append(new HashEntry(_config.ExpirationKey, _expirationTimeStr))
                        .ToArray()
                );
                await _database.KeyExpireAsync(id, _config.AbsoluteExpiration);
                return userSession;
            }
            else
            {
                var session = _config.ConstructFromHash(redisHash);
                if (userSession.Equals(session))
                {
                    await SetExpirationAsync(id);
                    return session;
                }
                return null;
            }
        }

        public async Task<T?> GetSessionAsync(string id)
        {
            var redisKey = BuildKey(id);
            var expirationRedisValue = await _database.HashGetAsync(
                redisKey,
                _config.ExpirationKey
            );

            if (expirationRedisValue.IsNull)
            {
                return null;
            }

            if (expirationRedisValue.TryParse(out long expirationSeconds))
            {
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expirationSeconds);
                if (expirationTime < DateTimeOffset.UtcNow)
                {
                    await _database.KeyDeleteAsync(redisKey);
                    return null;
                }
            }

            await SetExpirationAsync(redisKey);
            var fullRedisHash = await _database.HashGetAllAsync(redisKey);
            return _config.ConstructFromHash(fullRedisHash);
        }

        public async Task LogoutAsync(string id)
        {
            var key = BuildKey(id);
            await _database.KeyDeleteAsync(key);
        }
    }
}
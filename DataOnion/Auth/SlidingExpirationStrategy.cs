using StackExchange.Redis;

namespace DataOnion.Auth
{
    public class SlidingExpirationStrategy<T> : IAuthServiceStrategy<T>
        where T : class, IAuthStorable<T>
    {
        public static string ExpiryKey { get; } = "expiration";

        private readonly IDatabase _database;
        private readonly TimeSpan _timeout;
        private readonly TimeSpan? _absoluteExpiration;
        private readonly string _keyPrefix;
        private readonly Func<HashEntry[], T> _makeFromHash;

        private string _expirationTimeStr => DateTimeOffset.UtcNow
            .Add(_timeout)
            .ToUnixTimeSeconds()
            .ToString();

        public SlidingExpirationStrategy(
            IDatabase database,
            TimeSpan timeout,
            TimeSpan? absoluteExpiration,
            string keyPrefix,
            Func<HashEntry[], T> makeFromHash
        )
        {
            _database = database;
            _timeout = timeout;
            _absoluteExpiration = absoluteExpiration;
            _keyPrefix = keyPrefix;
            _makeFromHash = makeFromHash;
        }

        private async Task SetExpirationAsync(RedisKey id)
        {
            await _database.HashSetAsync(
                id,
                ExpiryKey,
                _expirationTimeStr
            );
        }

        public async Task<T?> LoginAsync(T userSession)
        {
            var id = new RedisKey(_keyPrefix + userSession.GetId());
            var redisHash = await _database.HashGetAllAsync(id);

            if (redisHash.Length == 0)
            {
                await _database.HashSetAsync(
                    id,
                    userSession
                        .ToRedisHash()
                        .Append(new HashEntry(ExpiryKey, _expirationTimeStr))
                        .ToArray()
                );
                await _database.KeyExpireAsync(id, _absoluteExpiration);
                return userSession;
            }
            else
            {
                var session = _makeFromHash(redisHash);
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
            var redisKey = new RedisKey(_keyPrefix + id);
            var expirationRedisValue = await _database.HashGetAsync(
                redisKey,
                ExpiryKey
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
            return _makeFromHash(fullRedisHash);
        }

        public async Task LogoutAsync(string id)
        {
            var key = new RedisKey(_keyPrefix + id);
            await _database.KeyDeleteAsync(key);
        }
    }
}
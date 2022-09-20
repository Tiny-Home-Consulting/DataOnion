using StackExchange.Redis;

namespace DataOnion.Auth
{
    public class SlidingExpirationStrategy<T> : IAuthServiceStrategy<T>
        where T : class, IAuthStorable<T>
    {
        public static string ExpiryKey { get; } = "expiration";

        private readonly IDatabase _database;
        private readonly TimeSpan _timeout;
        private readonly Func<HashEntry[], T> _makeFromHash;

        private string _expirationTimeStr => DateTimeOffset.UtcNow
            .Add(_timeout)
            .ToUnixTimeSeconds()
            .ToString();

        public SlidingExpirationStrategy(
            IDatabase database,
            TimeSpan timeout,
            Func<HashEntry[], T> makeFromHash
        )
        {
            _database = database;
            _timeout = timeout;
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

        public async Task<bool> LoginAsync(T details)
        {
            var id = new RedisKey(details.GetId());
            var redisHash = await _database.HashGetAllAsync(id);

            if (redisHash.Length == 0)
            {
                await _database.HashSetAsync(
                    id,
                    details
                        .ToRedisHash()
                        .Append(new HashEntry(ExpiryKey, _expirationTimeStr))
                        .ToArray()
                );
                return true;
            }
            else
            {
                var session = _makeFromHash(redisHash);
                if (details.IsSameSessionAs(session))
                {
                    await SetExpirationAsync(id);
                    return true;
                }
                return false;
            }
        }

        public async Task<T?> CheckSessionAsync(string id)
        {
            var redisKey = new RedisKey(id);
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
            await _database.KeyDeleteAsync(id);
        }

        public async Task<bool> SetAbsoluteExpirationAsync(string id, TimeSpan expiration)
        {
            return await _database.KeyExpireAsync(id, expiration);
        }
    }
}
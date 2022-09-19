using StackExchange.Redis;

namespace DataOnion.Auth
{
    public class SlidingExpirationAuthService<T> : IAuthService<T>
        where T : new()
    {
        public static string ExpiryKey { get; } = "expiration";

        private readonly IDatabase _database;
        private readonly TimeSpan _timeout;
        private readonly Func<T, string> _getId;
        private readonly Func<T, T, bool> _isSameSession;

        private string _expirationTimeStr => DateTimeOffset.UtcNow
            .Add(_timeout)
            .ToUnixTimeSeconds()
            .ToString();

        public SlidingExpirationAuthService(
            IDatabase database,
            TimeSpan timeout,
            Func<T, string> getId,
            Func<T, T, bool> isSameSession
        )
        {
            _database = database;
            _timeout = timeout;
            _getId = getId;
            _isSameSession = isSameSession;
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
            var id = new RedisKey(_getId(details));
            var redisHash = await _database.HashGetAllAsync(id);

            if (redisHash.Length == 0)
            {
                await _database.HashSetAsync(
                    id,
                    Utils.MakeRedisHashEntries(details)
                        .Append(new HashEntry(ExpiryKey, _expirationTimeStr))
                        .ToArray()
                );
                return true;
            }
            else
            {
                var session = Utils.ConstructFromRedisHash<T>(redisHash);
                if (_isSameSession(session, details))
                {
                    await SetExpirationAsync(id);
                    return true;
                }
                return false;
            }
        }
    }
}
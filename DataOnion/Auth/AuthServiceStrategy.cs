using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace DataOnion.Auth
{
    public interface IAuthServiceStrategy<T>
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
        public TimeSpan SlidingExpiration { get; private set; }
        public TimeSpan? AbsoluteExpiration { get; private set; }
        public string ExpirationKey { get; private set; }
        public string EnvironmentPrefix { get; private set; }
        public string AuthPrefix { get; private set; }
        public Func<HashEntry[], T> ConstructFromHash { get; private set; }

        public SlidingExpirationConfig(
            TimeSpan slidingExpiration,
            TimeSpan? absoluteExpiration,
            string expirationKey,
            string environmentPrefix,
            string authPrefix,
            Func<HashEntry[], T> constructFromHash
        )
        {
            SlidingExpiration = slidingExpiration;
            AbsoluteExpiration = absoluteExpiration;
            ExpirationKey = expirationKey;
            EnvironmentPrefix = environmentPrefix;
            AuthPrefix = authPrefix;
            ConstructFromHash = constructFromHash;
        }
    }

    internal class SlidingExpirationStrategy<T> : IAuthServiceStrategy<T>
        where T : class, IAuthStorable<T>
    {
        private readonly IRedisManager _redisManager;
        private readonly SlidingExpirationConfig<T> _config;
        private readonly ILogger? _logger;

        private IDatabase _database => _redisManager.GetDatabase();
        private DateTimeOffset _expirationTime => DateTimeOffset.UtcNow
            .Add(_config.SlidingExpiration);

        public SlidingExpirationStrategy(
            IRedisManager redisManager,
            SlidingExpirationConfig<T> config,
            ILogger<SlidingExpirationStrategy<T>>? logger = null
        )
        {
            _redisManager = redisManager;
            _config = config;
            _logger = logger;
        }

        private async Task SetExpirationAsync(RedisKey key)
        {
            var newExpiration = _expirationTime;
            _logger?.LogDebug(
                "Bumping expiration for key '{0}' to {1}",
                key,
                newExpiration
            );
            await _database.HashSetAsync(
                key,
                _config.ExpirationKey,
                newExpiration.ToUnixTimeSeconds().ToString()
            );
        }

        private RedisKey BuildKey(string id) => new($"{_config.EnvironmentPrefix}_{_config.AuthPrefix}_{id}");

        private async Task CreateSessionAsync(RedisKey id, T userSession)
        {
            await _database.HashSetAsync(
                id,
                userSession
                    .ToRedisHash()
                    .Append(new HashEntry(
                        _config.ExpirationKey,
                        _expirationTime.ToUnixTimeSeconds().ToString()
                    ))
                    .ToArray()
            );
            await _database.KeyExpireAsync(id, _config.AbsoluteExpiration);
        }

        public async Task<T?> LoginAsync(T userSession)
        {
            var id = BuildKey(userSession.GetId());
            _logger?.LogDebug(
                "Fetching Redis item '{0}' as hash",
                id
            );
            var redisHash = await _database.HashGetAllAsync(id);
            // If the value stored was not a hash, that will throw a RedisServerException

            if (redisHash.Length == 0)
            {
                _logger?.LogDebug(
                    "Session does not exist, creating hash in Redis at '{0}'",
                    id
                );
                await CreateSessionAsync(id, userSession);
                return userSession;
            }
            else
            {
                _logger?.LogDebug(
                    "Redis item with id '{0}' found; comparing to provided session",
                    id
                );

                T session;
                try
                {
                    session = _config.ConstructFromHash(redisHash);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(
                        "Redis item with id '{0}' cannot be deserialized; deleting and recreating it. Error message: {1}",
                        id,
                        e
                    );
                    try
                    {
                        await _database.KeyDeleteAsync(id);
                        await CreateSessionAsync(id, userSession);
                        return userSession;
                    }
                    catch (RedisException re)
                    {
                        _logger?.LogError(
                            re,
                            "Unexpected Redis error, see exception message for details"
                        );
                        throw;
                    }
                }

                if (userSession.Equals(session))
                {
                    _logger?.LogDebug(
                        "Existing session at '{0}' is still valid, reusing",
                        id
                    );
                    try
                    {
                        await SetExpirationAsync(id);
                    }
                    catch (RedisException e)
                    {
                        _logger?.LogError(
                            e,
                            "Unexpected Redis error, see exception message for details"
                        );
                        throw;
                    }
                    return session;
                }
                else
                {
                    _logger?.LogDebug(
                        "Session provided does not match existing session at '{0}'",
                        id
                    );
                    return null;
                }
            }
        }

        public async Task<T?> GetSessionAsync(string id)
        {
            var redisKey = BuildKey(id);
            _logger?.LogDebug(
                "Fetching Redis item '{0}' as hash",
                redisKey
            );
            var expirationRedisValue = await _database.HashGetAsync(
                redisKey,
                _config.ExpirationKey
            );

            if (expirationRedisValue.IsNull)
            {
                _logger?.LogDebug(
                    "Redis item '{0}' does not exist or has no expiration",
                    redisKey
                );
                return null;
            }

            if (expirationRedisValue.TryParse(out long expirationSeconds))
            {
                _logger?.LogDebug(
                    "Converting {0} from Unix seconds to DateTimeOffset",
                    expirationSeconds
                );
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expirationSeconds);
                if (expirationTime < DateTimeOffset.UtcNow)
                {
                    _logger?.LogDebug(
                        "Session at '{0}' expired, removing from Redis",
                        id
                    );
                    await _database.KeyDeleteAsync(redisKey);
                    return null;
                }
            }
            else
            {
                _logger?.LogDebug(
                    "Redis item '{0}' has invalid expiration: {1}. Invalidating session.",
                    redisKey,
                    expirationRedisValue
                );
                await _database.KeyDeleteAsync(redisKey);
                return null;
            }

            try
            {
                await SetExpirationAsync(redisKey);
                var fullRedisHash = await _database.HashGetAllAsync(redisKey);
                
                try
                {
                    return _config.ConstructFromHash(fullRedisHash);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(
                        "Redis item with id '{0}' cannot be deserialized; deleting it. Error message: {1}",
                        id,
                        e
                    );
                    await _database.KeyDeleteAsync(redisKey);
                    return null;
                }
            }
            catch (RedisException e)
            {
                _logger?.LogError(
                    e,
                    "Unexpected Redis error, see exception message for details"
                );
                throw;
            }
        }

        public async Task LogoutAsync(string id)
        {
            var key = BuildKey(id);
            await _database.KeyDeleteAsync(key);
        }
    }
}
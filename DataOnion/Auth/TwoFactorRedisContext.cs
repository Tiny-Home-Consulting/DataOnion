using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataOnion.Auth
{
    public interface ITwoFactorRedisContext
    {
        Task CreateTwoFactorRequestAsync(string userId, TwoFactorRequest request);
        Task<IEnumerable<TwoFactorRequest>> FetchTwoFactorRequestsAsync(
            string userId,
            VerificationMethod method
        );
        Task DeleteTwoFactorRequestsAsync(
            string userId
        );
    }

    public class TwoFactorRequest
    {
        public string Did { get; set; }
        public int VerificationCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid Token { get; set; }
        public VerificationMethod Method { get; set; }
    }

    public class TwoFactorRedisContext : ITwoFactorRedisContext
    {
        private readonly string _environmentPrefix;
        private readonly string _twoFactorRequestPrefix;
        private readonly ILogger? _logger;
        private readonly IRedisManager _redisManager;
        private IDatabase _database => _redisManager.GetDatabase();

        public TwoFactorRedisContext(
            string environmentPrefix,
            string twoFactorRequestPrefix,
            IRedisManager redisManager,
            ILogger<TwoFactorRedisContext>? logger
        )
        {
            _environmentPrefix = environmentPrefix;
            _twoFactorRequestPrefix = twoFactorRequestPrefix;
            _redisManager = redisManager;
            _logger = logger;
        }

        public async Task CreateTwoFactorRequestAsync(string userId, TwoFactorRequest request)
        {
            var keyPrefix = _twoFactorRequestPrefix;
            var method = request.Method;
            var redisKey = BuildTwoFactorKey(
                userId,
                method
            );
            var stringEnumConverter = new JsonStringEnumConverter();
            var serializationOptions = new JsonSerializerOptions();
            serializationOptions.Converters.Add(stringEnumConverter);

            await _database.ListLeftPushAsync(
                redisKey, 
                new RedisValue(
                    JsonSerializer.Serialize(
                        request, 
                        serializationOptions
                    )
                )
            );
        }

        public async Task<IEnumerable<TwoFactorRequest>> FetchTwoFactorRequestsAsync(
            string userId,
            VerificationMethod method
        )
        {
            var key = BuildTwoFactorKey(userId, method);

            try
            {
                var stringEnumConverter = new JsonStringEnumConverter();
                var serializationOptions = new JsonSerializerOptions();
                serializationOptions.Converters.Add(stringEnumConverter);

                var values = await FetchValuesAsync(
                    key
                );

                var requests = values.Select(val => 
                    JsonSerializer.Deserialize<TwoFactorRequest>(
                        val, 
                        serializationOptions
                    )
                );

                return requests;
            } 
            catch (JsonException e)
            {
                _logger?.LogError(
                    e, 
                    "Error deserializing a 2FA request for user {UserID}. This means most likely the JSON data stored is not valid. Key: {Key}", 
                    userId,
                    key.ToString()
                );

                return new List<TwoFactorRequest>();
            }
            catch (Exception e)
            {
                _logger?.LogError(
                    e,
                    "Unexpected error occurred trying to retrieve values in Redis for user {UserID}. Key: {Key}",
                    userId,
                    key.ToString()
                );

                return new List<TwoFactorRequest>();
            }
        }

        private async Task<IEnumerable<RedisValue>> FetchValuesAsync(
            RedisKey key
        )
        {
            try
            {
                var stringEnumConverter = new JsonStringEnumConverter();
                var serializationOptions = new JsonSerializerOptions();
                serializationOptions.Converters.Add(stringEnumConverter);

                var values = await _database.ListRangeAsync(key);

                return values;
            } catch (RedisServerException e)
            {
                _logger?.LogError(
                    e, 
                    "Error retrieving values in Redis for key {Key}. This means most likely the data structure used to store the data in Redis is not a list.", 
                    key.ToString()
                );

                return new List<RedisValue>();
            }
        }

        public async Task DeleteTwoFactorRequestsAsync(string userId)
        {
            var keys = new RedisKey[]
            {
                BuildTwoFactorKey(userId, VerificationMethod.Call),
                BuildTwoFactorKey(userId, VerificationMethod.Text)
            };

            foreach (var key in keys)
            {
                await _database.KeyDeleteAsync(key);
            }
        }

        private RedisKey BuildTwoFactorKey(
            string userId,
            VerificationMethod method
        )
        {
            var envPrefix = _environmentPrefix;
            var twoFactorPrefix = _twoFactorRequestPrefix;
            var methodString = method.ToString().ToLower();
            
            return new RedisKey(
                $"{envPrefix}_{twoFactorPrefix}_{userId}_{methodString}"
            );
        }
    }
}
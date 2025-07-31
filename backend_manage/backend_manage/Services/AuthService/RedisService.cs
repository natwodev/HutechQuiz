using backend_manage.Services.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Threading.Tasks;

namespace backend_manage.Services
{
    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisService> _logger;

        public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public bool IsConnected => _redis.IsConnected;

        public IDatabase GetDatabase()
        {
            return _redis.GetDatabase();
        }

        public async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null)
        {
            try
            {
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("Redis không khả dụng, bỏ qua set key: {Key}", key);
                    return false;
                }

                var db = _redis.GetDatabase();
                return await db.StringSetAsync(key, value, expiry);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối Redis để set key: {Key}", key);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi set key: {Key}", key);
                return false;
            }
        }

        public async Task<string?> StringGetAsync(string key)
        {
            try
            {
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("Redis không khả dụng, bỏ qua get key: {Key}", key);
                    return null;
                }

                var db = _redis.GetDatabase();
                var value = await db.StringGetAsync(key);
                return value.HasValue ? value.ToString() : null;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối Redis để get key: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi get key: {Key}", key);
                return null;
            }
        }

        public async Task<bool> KeyDeleteAsync(string key)
        {
            try
            {
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("Redis không khả dụng, bỏ qua delete key: {Key}", key);
                    return false;
                }

                var db = _redis.GetDatabase();
                return await db.KeyDeleteAsync(key);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối Redis để delete key: {Key}", key);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi delete key: {Key}", key);
                return false;
            }
        }

        public async Task<bool> KeyExistsAsync(string key)
        {
            try
            {
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("Redis không khả dụng, bỏ qua check key: {Key}", key);
                    return false;
                }

                var db = _redis.GetDatabase();
                return await db.KeyExistsAsync(key);
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối Redis để check key: {Key}", key);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi check key: {Key}", key);
                return false;
            }
        }


    }

    public class RedisFallbackService : IRedisService
    {
        private readonly ILogger<RedisFallbackService> _logger;

        public RedisFallbackService(ILogger<RedisFallbackService> logger)
        {
            _logger = logger;
        }

        public bool IsConnected => false;

        public IDatabase GetDatabase()
        {
            throw new InvalidOperationException("Redis không khả dụng");
        }

        public async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null)
        {
            _logger.LogWarning("Redis fallback: Bỏ qua set key {Key} - Redis không khả dụng", key);
            return false;
        }

        public async Task<string?> StringGetAsync(string key)
        {
            _logger.LogWarning("Redis fallback: Bỏ qua get key {Key} - Redis không khả dụng", key);
            return null;
        }

        public async Task<bool> KeyDeleteAsync(string key)
        {
            _logger.LogWarning("Redis fallback: Bỏ qua delete key {Key} - Redis không khả dụng", key);
            return false;
        }

        public async Task<bool> KeyExistsAsync(string key)
        {
            _logger.LogWarning("Redis fallback: Bỏ qua check key {Key} - Redis không khả dụng", key);
            return false;
        }
    }
} 
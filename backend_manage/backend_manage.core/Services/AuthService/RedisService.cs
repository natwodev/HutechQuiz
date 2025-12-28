using backend_manage.core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace backend_manage.core.Services.AuthService
{
    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisService> _logger;
        private bool _isConnected;
        private DateTime _lastConnectionCheck = DateTime.MinValue;
        private readonly TimeSpan _connectionCheckInterval = TimeSpan.FromMinutes(10); // Tăng interval lên 10 phút
        private readonly TimeSpan _fastFailInterval = TimeSpan.FromSeconds(30); // Fast fail khi có lỗi
        private bool _hasRecentFailure = false;

        public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
        {
            _redis = redis;
            _logger = logger;
            _isConnected = redis.IsConnected;
            _logger.LogInformation("RedisService được khởi tạo với trạng thái kết nối: {IsConnected}", _isConnected);
        }

        public bool IsConnected 
        { 
            get 
            {
                var now = DateTime.UtcNow;
                
                // Nếu có lỗi gần đây, kiểm tra nhanh hơn
                var checkInterval = _hasRecentFailure ? _fastFailInterval : _connectionCheckInterval;
                
                // Chỉ kiểm tra lại kết nối sau mỗi interval để tránh ping liên tục
                if (now - _lastConnectionCheck > checkInterval)
                {
                    var wasConnected = _isConnected;
                    _isConnected = _redis.IsConnected;
                    _lastConnectionCheck = now;
                    
                    // Nếu trạng thái thay đổi, log
                    if (wasConnected != _isConnected)
                    {
                        if (!_isConnected)
                        {
                            _logger.LogWarning("Redis connection lost - sẽ sử dụng fallback cho các operation");
                            _hasRecentFailure = true;
                        }
                        else
                        {
                            _logger.LogInformation("Redis connection restored");
                            _hasRecentFailure = false;
                        }
                    }
                }
                return _isConnected;
            }
        }

        public IDatabase GetDatabase()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Redis không khả dụng");
            }
            return _redis.GetDatabase();
        }

        public async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null)
        {
            try
            {
                if (!IsConnected)
                {
                    _logger.LogDebug("Redis không khả dụng, bỏ qua set key: {Key}", key);
                    return false;
                }

                var db = _redis.GetDatabase();
                var result = await db.StringSetAsync(key, value, expiry);
                
                // Reset failure flag nếu operation thành công
                if (result) _hasRecentFailure = false;
                
                return result;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối Redis để set key: {Key}", key);
                _isConnected = false;
                _hasRecentFailure = true;
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
                if (!IsConnected)
                {
                    _logger.LogDebug("Redis không khả dụng, bỏ qua get key: {Key}", key);
                    return null;
                }

                var db = _redis.GetDatabase();
                var value = await db.StringGetAsync(key);
                
                // Reset failure flag nếu operation thành công
                _hasRecentFailure = false;
                
                return value.HasValue ? value.ToString() : null;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối Redis để get key: {Key}", key);
                _isConnected = false;
                _hasRecentFailure = true;
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
                if (!IsConnected)
                {
                    _logger.LogDebug("Redis không khả dụng, bỏ qua delete key: {Key}", key);
                    return false;
                }

                var db = _redis.GetDatabase();
                var result = await db.KeyDeleteAsync(key);
                
                // Reset failure flag nếu operation thành công
                if (result) _hasRecentFailure = false;
                
                return result;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối Redis để delete key: {Key}", key);
                _isConnected = false;
                _hasRecentFailure = true;
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi delete key: {Key}", key);
                return false;
            }
        }

        public async Task<long> KeyDeleteAsync(IEnumerable<string> keys)
        {
            try
            {
                if (!IsConnected || keys == null || !keys.Any())
                {
                    return 0;
                }

                var db = _redis.GetDatabase();
                var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
                var result = await db.KeyDeleteAsync(redisKeys);
                
                _hasRecentFailure = false;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi delete nhiều keys");
                return 0;
            }
        }

        public async Task<bool> KeyExistsAsync(string key)
        {
            try
            {
                if (!IsConnected)
                {
                    _logger.LogDebug("Redis không khả dụng, bỏ qua check key: {Key}", key);
                    return false;
                }

                var db = _redis.GetDatabase();
                var result = await db.KeyExistsAsync(key);
                
                // Reset failure flag nếu operation thành công
                _hasRecentFailure = false;
                
                return result;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối Redis để check key: {Key}", key);
                _isConnected = false;
                _hasRecentFailure = true;
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi check key: {Key}", key);
                return false;
            }
        }

        public IServer GetServer()
        {
            var endpoints = _redis.GetEndPoints();
            return _redis.GetServer(endpoints[0]);
        }

        // Hash operations
        public async Task<bool> HashSetAsync(string key, string hashField, string value, TimeSpan? expiry = null)
        {
            try
            {
                if (!IsConnected)
                {
                    _logger.LogDebug("Redis không khả dụng, bỏ qua hash set: {Key}:{Field}", key, hashField);
                    return false;
                }

                var db = _redis.GetDatabase();
                var result = await db.HashSetAsync(key, hashField, value);
                
                // Set expiry if provided
                if (result && expiry.HasValue)
                {
                    await db.KeyExpireAsync(key, expiry.Value);
                }
                
                // Reset failure flag nếu operation thành công
                if (result) _hasRecentFailure = false;
                
                return result;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối Redis để hash set: {Key}:{Field}", key, hashField);
                _isConnected = false;
                _hasRecentFailure = true;
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hash set: {Key}:{Field}", key, hashField);
                return false;
            }
        }

        public async Task<string?> HashGetAsync(string key, string hashField)
        {
            try
            {
                if (!IsConnected)
                {
                    _logger.LogDebug("Redis không khả dụng, bỏ qua hash get: {Key}:{Field}", key, hashField);
                    return null;
                }

                var db = _redis.GetDatabase();
                var value = await db.HashGetAsync(key, hashField);
                
                // Reset failure flag nếu operation thành công
                _hasRecentFailure = false;
                
                return value.HasValue ? value.ToString() : null;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối Redis để hash get: {Key}:{Field}", key, hashField);
                _isConnected = false;
                _hasRecentFailure = true;
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hash get: {Key}:{Field}", key, hashField);
                return null;
            }
        }

        public async Task<Dictionary<string, string>?> HashGetAllAsync(string key)
        {
            try
            {
                if (!IsConnected)
                {
                    _logger.LogDebug("Redis không khả dụng, bỏ qua hash get all: {Key}", key);
                    return null;
                }

                var db = _redis.GetDatabase();
                var hashEntries = await db.HashGetAllAsync(key);
                
                // Reset failure flag nếu operation thành công
                _hasRecentFailure = false;
                
                if (hashEntries.Length == 0)
                    return null;

                var result = new Dictionary<string, string>();
                foreach (var entry in hashEntries)
                {
                    result[entry.Name] = entry.Value;
                }
                
                return result;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối Redis để hash get all: {Key}", key);
                _isConnected = false;
                _hasRecentFailure = true;
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hash get all: {Key}", key);
                return null;
            }
        }

        public async Task<bool> HashDeleteAsync(string key, string hashField)
        {
            try
            {
                if (!IsConnected)
                {
                    _logger.LogDebug("Redis không khả dụng, bỏ qua hash delete: {Key}:{Field}", key, hashField);
                    return false;
                }

                var db = _redis.GetDatabase();
                var result = await db.HashDeleteAsync(key, hashField);
                
                // Reset failure flag nếu operation thành công
                if (result) _hasRecentFailure = false;
                
                return result;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối Redis để hash delete: {Key}:{Field}", key, hashField);
                _isConnected = false;
                _hasRecentFailure = true;
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hash delete: {Key}:{Field}", key, hashField);
                return false;
            }
        }

        public async Task<bool> HashExistsAsync(string key, string hashField)
        {
            try
            {
                if (!IsConnected)
                {
                    _logger.LogDebug("Redis không khả dụng, bỏ qua hash exists: {Key}:{Field}", key, hashField);
                    return false;
                }

                var db = _redis.GetDatabase();
                var result = await db.HashExistsAsync(key, hashField);
                
                // Reset failure flag nếu operation thành công
                _hasRecentFailure = false;
                
                return result;
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Không thể kết nối Redis để hash exists: {Key}:{Field}", key, hashField);
                _isConnected = false;
                _hasRecentFailure = true;
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hash exists: {Key}:{Field}", key, hashField);
                return false;
            }
        }
    }
    
} 
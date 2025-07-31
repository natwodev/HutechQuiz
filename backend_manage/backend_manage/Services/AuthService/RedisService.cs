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
    }

    public class RedisFallbackService : IRedisService
    {
        private readonly ILogger<RedisFallbackService> _logger;

        public RedisFallbackService(ILogger<RedisFallbackService> logger)
        {
            _logger = logger;
            _logger.LogInformation("RedisFallbackService được khởi tạo - Redis không khả dụng");
        }

        public bool IsConnected => false;

        public IDatabase GetDatabase()
        {
            throw new InvalidOperationException("Redis không khả dụng");
        }

        public async Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null)
        {
            _logger.LogDebug("Redis fallback: Bỏ qua set key {Key} - Redis không khả dụng", key);
            return false;
        }

        public async Task<string?> StringGetAsync(string key)
        {
            _logger.LogDebug("Redis fallback: Bỏ qua get key {Key} - Redis không khả dụng", key);
            return null;
        }

        public async Task<bool> KeyDeleteAsync(string key)
        {
            _logger.LogDebug("Redis fallback: Bỏ qua delete key {Key} - Redis không khả dụng", key);
            return false;
        }

        public async Task<bool> KeyExistsAsync(string key)
        {
            _logger.LogDebug("Redis fallback: Bỏ qua check key {Key} - Redis không khả dụng", key);
            return false;
        }
    }
} 
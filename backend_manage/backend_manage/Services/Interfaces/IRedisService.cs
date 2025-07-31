using StackExchange.Redis;

namespace backend_manage.Services.Interfaces
{
    public interface IRedisService
    {
        bool IsConnected { get; }
        IDatabase GetDatabase();
        Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null);
        Task<string?> StringGetAsync(string key);
        Task<bool> KeyDeleteAsync(string key);
        Task<bool> KeyExistsAsync(string key);
    }
}
using StackExchange.Redis;

namespace backend_manage.core.Services.Interfaces
{
    public interface IRedisService
    {
        bool IsConnected { get; }
        StackExchange.Redis.IDatabase GetDatabase();
        Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null);
        Task<string?> StringGetAsync(string key);
        Task<bool> KeyDeleteAsync(string key);
        Task<long> KeyDeleteAsync(IEnumerable<string> keys);
        Task<bool> KeyExistsAsync(string key);
        IServer GetServer();
        
        // Hash operations
        Task<bool> HashSetAsync(string key, string hashField, string value, TimeSpan? expiry = null);
        Task<string?> HashGetAsync(string key, string hashField);
        Task<Dictionary<string, string>?> HashGetAllAsync(string key);
        Task<bool> HashDeleteAsync(string key, string hashField);
        Task<bool> HashExistsAsync(string key, string hashField);
    }
}
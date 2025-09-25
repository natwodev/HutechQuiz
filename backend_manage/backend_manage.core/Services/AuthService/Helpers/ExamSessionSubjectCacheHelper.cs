using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace backend_manage.core.Services.AuthService.Helpers;

public class ExamSessionSubjectCacheHelper
{
    private readonly IRedisService _redisService;
    private readonly ILogger<ExamSessionSubjectCacheHelper> _logger;
    private readonly IRepository<ExamSessionSubject> _examSessionSubjectRepository;

    public ExamSessionSubjectCacheHelper(
        IRedisService redisService,
        ILogger<ExamSessionSubjectCacheHelper> logger,
        IRepository<ExamSessionSubject> examSessionSubjectRepository)
    {
        _redisService = redisService;
        _logger = logger;
        _examSessionSubjectRepository = examSessionSubjectRepository;
    }

    #region GetIsOpenFromCacheAsync
    public async Task<(bool redisAvailable, bool? isOpen)> GetIsOpenFromCacheAsync(int examSessionSubjectId)
    {
        try
        {
            if (!_redisService.IsConnected)
            {
                _logger.LogDebug("Redis không khả dụng khi lấy IsOpen cho ExamSessionSubjectId {Id}", examSessionSubjectId);
                return (false, null);
            }

            var key = $"exam_session_subject:is_open:{examSessionSubjectId}";
            var db = _redisService.GetDatabase();
            var cachedValue = await db.StringGetAsync(key);
            var cached = cachedValue.HasValue ? cachedValue.ToString() : null;
            if (string.IsNullOrEmpty(cached))
            {
                _logger.LogDebug("Cache miss: {Key}", key);
                return (true, null);
            }

            if (bool.TryParse(cached, out bool value))
            {
                _logger.LogDebug("Redis hit: {Key} = {Value}", key, cached);
                return (true, value);
            }

            _logger.LogWarning("Giá trị cache không hợp lệ cho key {Key}: {Cached}", key, cached);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi đọc cache IsOpen cho ExamSessionSubjectId {Id}", examSessionSubjectId);
            return (false, null);
        }
    }
    #endregion

    #region GetOrComputeIsOpenAsync
    public async Task<bool> GetOrComputeIsOpenAsync(int examSessionSubjectId, TimeSpan? ttl = null)
    {
        var (redisAvailable, cached) = await GetIsOpenFromCacheAsync(examSessionSubjectId);
        if (cached.HasValue)
        {
            return cached.Value;
        }

        var entity = await _examSessionSubjectRepository.GetQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExamSessionSubjectId == examSessionSubjectId);

        if (entity == null)
        {
            if (redisAvailable)
            {
                await SetIsOpenCacheAsync(examSessionSubjectId, false);
            }
            return false;
        }

        bool isOpen;
        if (entity.IsCompleted || !entity.IsActive)
        {
            isOpen = false;
        }
        else
        {
            var now = DateTimeHelper.GetVietnamTime();
            isOpen = now >= entity.StartTime && now <= entity.EndTime;
        }

        if (redisAvailable)
        {
            await SetIsOpenCacheAsync(examSessionSubjectId, isOpen);
        }
        return isOpen;
    }
    #endregion
    
    #region Set/Remove Cache
    public async Task<bool> SetIsOpenCacheAsync(int examSessionSubjectId, bool isOpen)
    {
        try
        {
            if (!_redisService.IsConnected)
            {
                _logger.LogDebug("Redis không khả dụng khi cache IsOpen cho ExamSessionSubjectId {Id}", examSessionSubjectId);
                return false;
            }

            var key = $"exam_session_subject:is_open:{examSessionSubjectId}";
            var db = _redisService.GetDatabase();
            var ok = await db.StringSetAsync(key, isOpen ? "true" : "false", TimeSpan.FromMinutes(30));
            _logger.LogDebug("Đã cache IsOpen={IsOpen} với TTL {TTL} cho key {Key}", isOpen, TimeSpan.FromMinutes(30), key);
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi ghi cache IsOpen cho ExamSessionSubjectId {Id}", examSessionSubjectId);
            return false;
        }
    }
    
    #endregion
}




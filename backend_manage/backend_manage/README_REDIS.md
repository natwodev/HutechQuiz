# Redis và RabbitMQ Configuration - Hệ thống Fallback

## Tổng quan
Hệ thống đã được cấu hình để Redis và RabbitMQ hoạt động như các dịch vụ phụ trợ. Khi các dịch vụ này không khả dụng, hệ thống vẫn chạy bình thường với fallback về database và **thực hiện đầy đủ các tác vụ**.

## Redis Configuration

### Cấu hình hiện tại
- **Connection String**: `localhost:6380,abortConnect=false` (từ appsettings.json)
- **Fallback**: Khi Redis không khả dụng, hệ thống sẽ fallback về database
- **Retry Policy**: Exponential retry với 3 lần thử kết nối
- **Timeout**: 5 giây cho connect, sync, response


### RedisService Wrapper
- Tạo `IRedisService` interface và `RedisService` implementation
- Xử lý tất cả Redis operations với try-catch
- Trả về `false` hoặc `null` khi Redis không khả dụng thay vì throw exception

### RedisFallbackService - ⭐ **CẢI THIỆN MỚI**
- **Mục đích**: Xử lý trường hợp Redis không khả dụng trong Dependency Injection
- **Cách hoạt động**: Tự động detect khi Redis connection fail và sử dụng fallback service
- **Không crash app**: App vẫn khởi động được ngay cả khi Redis không khả dụng



```csharp
// ServiceExtensions.cs - Khởi tạo IConnectionMultiplexer với fallback
services.AddSingleton<IConnectionMultiplexer>(sp => {
    var redisConnectionString = configuration["Redis:ConnectionString"] ?? "localhost:6380,abortConnect=false";
    // ... logic kết nối Redis với retry và fallback
});

// DependencyInjection.cs - Sử dụng IConnectionMultiplexer đã đăng ký
services.AddScoped<IRedisService>(sp =>
{
    try
    {
        var redis = sp.GetRequiredService<IConnectionMultiplexer>();
        var logger = sp.GetRequiredService<ILogger<RedisService>>();
        return new RedisService(redis, logger);
    }
    catch (Exception ex)
    {
        var logger = sp.GetRequiredService<ILogger<RedisFallbackService>>();
        logger.LogWarning(ex, "Không thể khởi tạo Redis service, sẽ sử dụng fallback");
        return new RedisFallbackService(logger);
    }
});
```

### Cách hoạt động
1. **Khi Redis khả dụng**: Cache data để tăng performance
2. **Khi Redis không khả dụng**:
   - Log warning
   - Fallback về database
   - Hệ thống vẫn chạy bình thường


### Các helper classes sử dụng Redis
- `StudentCacheHelper`: Cache thông tin sinh viên
- `StudentExamSessionCacheHelper`: Cache phiên thi của sinh viên
- `ExamPaperHelper`: Cache đề thi và answer key

### Middleware Updates - ⭐ **CẢI THIỆN QUAN TRỌNG**

#### Vấn đề đã được fix:
- **Trước**: Middleware inject `IRedisService` trực tiếp → lỗi "Cannot resolve scoped service from root provider"
- **Sau**: Sử dụng `IServiceProvider` để resolve service khi cần

#### JwtBlacklistMiddleware:
- Sử dụng `IServiceProvider` để resolve `IRedisService` khi cần

### Controller Updates
- **StudentController**: Sử dụng `IRedisService` thay vì trực tiếp `IConnectionMultiplexer`

## RabbitMQ Configuration

### Cấu hình hiện tại
- **Host**: localhost:5672
- **Credentials**: guest/guest
- **Queues**:
   - `save_answer_queue`: Lưu đáp án
   - `submit_exam_queue`: Nộp bài thi
   - `save_exam_queue`: Lưu bài nháp
   - `student_import_queue`: Import sinh viên

### Fallback Strategy - ⭐ **CẢI THIỆN QUAN TRỌNG**
1. **Khi RabbitMQ khả dụng**: Xử lý message queue async bình thường
2. **Khi RabbitMQ không khả dụng**:
   - Sử dụng `RabbitMqFallbackService`
   - **Thực hiện tác vụ trực tiếp** thay vì bỏ qua
   - Không mất dữ liệu, tất cả tác vụ vẫn được xử lý
   - Chỉ khác là không async mà thực hiện đồng bộ

### Error Handling
- **Dependency Injection**: Try-catch khi khởi tạo services
- **Program.cs**: Try-catch khi start consumer
- **Fallback Services**: Thực hiện tác vụ trực tiếp, không bỏ qua

## Cách hoạt động

### Khi Redis khả dụng:
```csharp
// Normal Redis operations
var value = await _redisService.StringGetAsync("key");
if (!string.IsNullOrEmpty(value)) {
    // Process value
}
```

### Khi Redis không khả dụng:
```csharp
// RedisService sẽ log warning và trả về null
var value = await _redisService.StringGetAsync("key"); // Returns null
// Application continues without cache
```

## Testing Fallback

### Test Redis fallback
1. **Dừng Redis server**:
   ```bash
   # Windows
   net stop redis
   
   # Linux/Mac
   sudo systemctl stop redis
   # hoặc
   redis-cli shutdown
   ```

2. **Chạy ứng dụng**:
   ```bash
   dotnet run
   ```

3. **Kiểm tra logs**:
   - Ứng dụng sẽ log warning về Redis không khả dụng
   - API calls sẽ hoạt động bình thường (chậm hơn do không có cache)
   - **App không crash** khi Redis không khả dụng



### Test RabbitMQ fallback
1. Tắt RabbitMQ server
2. Khởi động ứng dụng
3. Kiểm tra log: "RabbitMQ không khả dụng, sẽ thực hiện tác vụ trực tiếp"
4. **Tất cả tác vụ vẫn được thực hiện đầy đủ** (lưu đáp án, nộp bài, import sinh viên)

## Các API endpoints bị ảnh hưởng

### Khi Redis không khả dụng:
- **Import progress**: Trả về 503 Service Unavailable
- **Cache operations**: Bỏ qua và fallback về database
- **Blacklist check**: Bỏ qua kiểm tra

### Khi Redis khả dụng:
- Tất cả operations hoạt động bình thường
- Cache được sử dụng để tăng performance

## Cấu hình trong appsettings.json

```json
{
  "Redis": {
    "ConnectionString": "localhost:6380,abortConnect=false"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "ExchangeName": "hutech_quiz_exchange",
    "QueueName": "hutech_quiz_queue",
    "RetryCount": 3,
    "RetryInterval": 2
  }
}
```

## Log Messages

### Redis
- ✅ "Redis service đã được khởi tạo thành công"
- ⚠️ "Không thể khởi tạo Redis service, sẽ sử dụng fallback"
- ⚠️ "Redis không khả dụng, sẽ sử dụng fallback"
- ❌ "Không thể kết nối Redis, sẽ sử dụng fallback"
- ⚠️ "Redis không khả dụng, bỏ qua kiểm tra blacklist token"
- ⚠️ "Redis không khả dụng, sử dụng chế độ bảo trì mặc định (false)"
- ⚠️ "Redis không khả dụng, không thể lấy progress cho job {JobId}"
- ⚠️ "Redis fallback: Bỏ qua {operation} - Redis không khả dụng"


### RabbitMQ
- ✅ "RabbitMQ service đã được khởi tạo thành công"
- ✅ "RabbitMQ consumer đã được khởi động thành công"
- ⚠️ "Không thể khởi tạo RabbitMQ service, sẽ sử dụng fallback"
- ⚠️ "RabbitMQ không khả dụng - thực hiện tác vụ trực tiếp cho queue {QueueName}"
- ✅ "Đã thực hiện tác vụ trực tiếp cho queue {QueueName}"

## Performance Impact

### Khi có Redis và RabbitMQ
- **High Performance**: Cache nhanh, message queue xử lý async
- **Scalability**: Có thể handle nhiều requests đồng thời
- **Async Processing**: Tác vụ được xử lý trong background

### Khi không có Redis và RabbitMQ
- **Reduced Performance**: Phải query database trực tiếp
- **Still Functional**: Tất cả chức năng vẫn hoạt động
- **No Data Loss**: Dữ liệu được lưu trực tiếp vào database
- **Synchronous Processing**: Tác vụ được xử lý đồng bộ (chậm hơn nhưng đảm bảo)

## Monitoring

### Redis Health Check
```csharp
// Kiểm tra Redis connection
if (_redisService.IsConnected) {
    // Redis is available
} else {
    // Redis is not available
}

public static bool IsRedisConnected(this IConnectionMultiplexer redis, ILogger logger)
{
    try
    {
        return redis.IsConnected;
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Redis không khả dụng");
        return false;
    }
}
```

### RabbitMQ Health Check
```csharp
public void CheckQueueStatus(string queueName)
{
    try
    {
        if (_channel?.IsOpen ?? false)
        {
            var queueInfo = _channel.QueueDeclarePassive(queueName);
            _logger.LogInformation("Trạng thái queue {QueueName}: Messages={MessageCount}, Consumers={ConsumerCount}", 
                queueName, queueInfo.MessageCount, queueInfo.ConsumerCount);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Lỗi khi kiểm tra trạng thái queue cho {QueueName}", queueName);
    }
}
```

## Troubleshooting

### Lỗi "Cannot resolve scoped service from root provider":
- **Nguyên nhân**: Middleware inject scoped service trực tiếp
- **Giải pháp**: ✅ Đã fix bằng cách sử dụng `IServiceProvider` và tạo scope

### Lỗi timeout khi Redis không chạy:
- **Nguyên nhân**: Middleware cố gắng kết nối Redis
- **Giải pháp**: ✅ Đã được fix bằng RedisService wrapper và RedisFallbackService

### Performance issues khi không có Redis:
- **Nguyên nhân**: Không có cache
- **Giải pháp**: Tối ưu database queries, sử dụng in-memory cache

### Memory usage cao:
- **Nguyên nhân**: Không có Redis để cache
- **Giải pháp**: Implement in-memory caching cho critical data

## Best Practices

1. **Always use fallback**: Đảm bảo hệ thống không crash khi Redis/RabbitMQ down
2. **No data loss**: Tất cả tác vụ vẫn được thực hiện, không bỏ qua
3. **Log everything**: Ghi log chi tiết để debug
4. **Monitor health**: Kiểm tra trạng thái dịch vụ định kỳ
5. **Graceful degradation**: Hệ thống vẫn hoạt động với performance thấp hơn
6. **Synchronous fallback**: Khi RabbitMQ down, tác vụ được xử lý đồng bộ thay vì bỏ qua
7. **Proper DI**: Sử dụng `IServiceProvider` cho middleware thay vì inject scoped service trực tiếp

## So sánh trước và sau cải thiện

### Trước cải thiện:
- ❌ RabbitMQ down → bỏ qua tất cả messages
- ❌ Redis down → timeout errors và crash
- ❌ Middleware inject scoped service → "Cannot resolve scoped service from root provider"
- ❌ Mất dữ liệu khi services không khả dụng
- ❌ Tác vụ không được thực hiện

### Sau cải thiện:
- ✅ RabbitMQ down → thực hiện tác vụ trực tiếp
- ✅ Redis down → graceful fallback, không timeout
- ✅ Middleware sử dụng `IServiceProvider` → không lỗi DI
- ✅ Không mất dữ liệu, tất cả tác vụ vẫn được xử lý
- ✅ Hệ thống vẫn hoạt động đầy đủ chức năng
- ✅ App khởi động được ngay cả khi Redis/RabbitMQ không khả dụng


## Cấu trúc file đã được cập nhật

### Files đã sửa:
- `Configurations/DependencyInjection.cs`: Thêm fallback logic cho Redis
- `Services/AuthService/RedisService.cs`: Thêm `RedisFallbackService`
- `Middlewares/JwtBlacklistMiddleware.cs`: Sử dụng `IServiceProvider`
- `Services/AuthService/Helpers/StudentExamSessionCacheHelper.cs`: Thêm methods để cập nhật StudentAnswersString
- `Services/AuthService/Helpers/StudentAnswerHelper.cs`: Sử dụng cache helper thay vì tạo key riêng
- `Services/AuthService/StudentService.cs`: Cập nhật để sử dụng cache helper

### Files đã loại bỏ:
- `Authentication/MaintenanceController.cs`: Loại bỏ chức năng maintenance mode
- `Middlewares/MaintenanceMiddleware.cs`: Đơn giản hóa thành pass-through middleware

### Files không thay đổi:
- `Messages/RabbitMQ/RabbitMQService.cs`: Đã có fallback
- `Messages/RabbitMQ/RabbitMQConsumer.cs`: Đã có fallback

## Cải thiện mới: Tối ưu hóa Redis Keys

### **Trước cải thiện:**
- Tạo key riêng: `student_answers:{studentCode}:{shuffledExamPaperId}`
- Dữ liệu bị phân tán giữa nhiều keys
- Khó quản lý và đồng bộ

### **Sau cải thiện:**
- ✅ **Đã loại bỏ hoàn toàn** key `student_answers:{studentCode}:{shuffledExamPaperId}`
- Sử dụng field `StudentAnswersString` trong `StudentExamSessionCacheDto`
- Key: `student_exam_session:{studentCode}:{studentExamSessionId}`
- Dữ liệu tập trung, dễ quản lý và đồng bộ

### **Lợi ích:**
- ✅ **Giảm số lượng keys** trong Redis (từ 3 keys xuống 2 keys)
- ✅ **Tập trung dữ liệu** vào một nơi duy nhất
- ✅ **Đồng bộ** với database structure
- ✅ **Dễ quản lý** và maintain
- ✅ **Performance tốt hơn** khi truy vấn
- ✅ **Code sạch hơn** - không còn logic phức tạp để parse key

 
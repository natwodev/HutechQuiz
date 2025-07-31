# Cấu hình Redis và RabbitMQ - Hệ thống Fallback

## Tổng quan
Hệ thống đã được cấu hình để Redis và RabbitMQ hoạt động như các dịch vụ phụ trợ. Khi các dịch vụ này không khả dụng, hệ thống vẫn chạy bình thường với fallback về database và **thực hiện đầy đủ các tác vụ**.

## Redis Configuration

### Cấu hình hiện tại
- **Connection String**: `localhost:6380,abortConnect=false` (từ appsettings.json)
- **Fallback**: Khi Redis không khả dụng, hệ thống sẽ fallback về database
- **Retry Policy**: Exponential retry với 10 lần thử kết nối
- **Timeout**: 30 giây cho connect, sync, response

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

## Cách test fallback

### Test Redis fallback
1. Tắt Redis server
2. Khởi động ứng dụng
3. Kiểm tra log: "Redis không khả dụng, sẽ sử dụng fallback"
4. Hệ thống vẫn chạy bình thường

### Test RabbitMQ fallback
1. Tắt RabbitMQ server
2. Khởi động ứng dụng
3. Kiểm tra log: "RabbitMQ không khả dụng, sẽ thực hiện tác vụ trực tiếp"
4. **Tất cả tác vụ vẫn được thực hiện đầy đủ** (lưu đáp án, nộp bài, import sinh viên)

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
- ✅ "Kết nối Redis thành công!"
- ⚠️ "Redis không khả dụng, sẽ sử dụng fallback"
- ❌ "Không thể kết nối Redis, sẽ sử dụng fallback"

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

## Best Practices

1. **Always use fallback**: Đảm bảo hệ thống không crash khi Redis/RabbitMQ down
2. **No data loss**: Tất cả tác vụ vẫn được thực hiện, không bỏ qua
3. **Log everything**: Ghi log chi tiết để debug
4. **Monitor health**: Kiểm tra trạng thái dịch vụ định kỳ
5. **Graceful degradation**: Hệ thống vẫn hoạt động với performance thấp hơn
6. **Synchronous fallback**: Khi RabbitMQ down, tác vụ được xử lý đồng bộ thay vì bỏ qua

## So sánh trước và sau cải thiện

### Trước cải thiện:
- ❌ RabbitMQ down → bỏ qua tất cả messages
- ❌ Mất dữ liệu khi RabbitMQ không khả dụng
- ❌ Tác vụ không được thực hiện

### Sau cải thiện:
- ✅ RabbitMQ down → thực hiện tác vụ trực tiếp
- ✅ Không mất dữ liệu, tất cả tác vụ vẫn được xử lý
- ✅ Hệ thống vẫn hoạt động đầy đủ chức năng 
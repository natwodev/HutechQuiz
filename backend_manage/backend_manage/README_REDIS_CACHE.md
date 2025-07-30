# Redis Cache Management for Student Service

## Tổng quan

Tài liệu này mô tả các phương thức để quản lý cache Redis cho dữ liệu sinh viên và phiên thi trong hệ thống HutechQuiz.

## Xử lý trường hợp đã có dữ liệu trong Cache

### Logic xử lý khi tải dữ liệu lên Redis

Khi gọi các phương thức `PreloadStudentsToRedisAsync()` hoặc `PreloadStudentExamSessionsToRedisAsync()`, hệ thống sẽ:

1. **Kiểm tra sự tồn tại**: Kiểm tra xem dữ liệu đã có trong cache chưa
2. **So sánh version**: So sánh version hoặc UpdatedAt để quyết định có cập nhật không
3. **Xử lý thông minh**:
   - **Thêm mới**: Nếu chưa tồn tại trong cache
   - **Cập nhật**: Nếu đã tồn tại nhưng có thay đổi (version cao hơn hoặc UpdatedAt mới hơn)
   - **Bỏ qua**: Nếu đã tồn tại và không có thay đổi

### Ví dụ xử lý sinh viên

```csharp
// Khi gọi PreloadStudentsToRedisAsync()
var (success, message, totalProcessed) = await _studentService.PreloadStudentsToRedisAsync();

// Kết quả có thể là:
// "Đã xử lý 1000 sinh viên: Thêm mới 200, Cập nhật 300, Bỏ qua 500"
```

### Ví dụ xử lý StudentExamSession

```csharp
// Khi gọi PreloadStudentExamSessionsToRedisAsync()
var (success, message, totalProcessed) = await _studentService.PreloadStudentExamSessionsToRedisAsync();

// Kết quả có thể là:
// "Đã xử lý 500 StudentExamSession: Thêm mới 50, Cập nhật 150, Bỏ qua 300"
```

## Các phương thức Cache

### 1. PreloadStudentsToRedisAsync()
**Mục đích**: Tải tất cả sinh viên từ database lên Redis cache
**Xử lý**: Thông minh - chỉ cập nhật khi có thay đổi
**Trả về**: `(bool Success, string Message, int TotalProcessed)`

```csharp
var (success, message, totalProcessed) = await _studentService.PreloadStudentsToRedisAsync();
// Message: "Đã xử lý 1000 sinh viên: Thêm mới 200, Cập nhật 300, Bỏ qua 500"
```

### 2. PreloadStudentExamSessionsToRedisAsync()
**Mục đích**: Tải tất cả StudentExamSession từ database lên Redis cache
**Xử lý**: Thông minh - chỉ cập nhật khi có thay đổi
**Trả về**: `(bool Success, string Message, int TotalProcessed)`

```csharp
var (success, message, totalProcessed) = await _studentService.PreloadStudentExamSessionsToRedisAsync();
```

### 3. PreloadAllDataToRedisAsync()
**Mục đích**: Tải tất cả dữ liệu (sinh viên + phiên thi) lên Redis cache
**Trả về**: `(bool Success, string Message, Dictionary<string, int> CachedCounts)`

```csharp
var (success, message, cachedCounts) = await _studentService.PreloadAllDataToRedisAsync();
```

### 4. CheckCacheStatusAsync()
**Mục đích**: Kiểm tra trạng thái cache hiện tại
**Trả về**: `(bool Success, string Message, Dictionary<string, object> CacheInfo)`

```csharp
var (success, message, cacheInfo) = await _studentService.CheckCacheStatusAsync();
// cacheInfo chứa: StudentCount, SessionCount, PaperCount, AnswerKeyCount, StudentAnswerCount, TotalKeys
```

### 5. ClearOldCacheAsync()
**Mục đích**: Xóa cache cũ (TTL < 1 giờ)
**Trả về**: `(bool Success, string Message, int DeletedCount)`

```csharp
var (success, message, deletedCount) = await _studentService.ClearOldCacheAsync();
```

### 6. RefreshCacheAsync()
**Mục đích**: Refresh toàn bộ cache (xóa cũ + tải mới)
**Trả về**: `(bool Success, string Message, Dictionary<string, int> Results)`

```csharp
var (success, message, results) = await _studentService.RefreshCacheAsync();
```

### 7. GetStudentFromRedisAsync(string studentCode)
**Mục đích**: Lấy thông tin sinh viên từ Redis cache
**Trả về**: `Student?`

```csharp
var student = await _studentService.GetStudentFromRedisAsync("SV001");
```

### 8. UpdateStudentInRedisAsync(Student student)
**Mục đích**: Cập nhật thông tin sinh viên trong Redis cache
**Trả về**: `bool`

```csharp
var success = await _studentService.UpdateStudentInRedisAsync(student);
```

### 9. RemoveStudentFromRedisAsync(string studentCode)
**Mục đích**: Xóa sinh viên khỏi Redis cache
**Trả về**: `bool`

```csharp
var success = await _studentService.RemoveStudentFromRedisAsync("SV001");
```

## API Endpoints

### Tải dữ liệu lên Redis Cache

#### Tải sinh viên (thông minh)
```http
POST /api/Student/preload-students-to-redis
Authorization: Bearer {token}
```

#### Tải StudentExamSessions (thông minh)
```http
POST /api/Student/preload-student-exam-sessions-to-redis
Authorization: Bearer {token}
```

#### Tải tất cả dữ liệu
```http
POST /api/Student/preload-all-data-to-redis
Authorization: Bearer {token}
```

### Quản lý Cache

#### Kiểm tra trạng thái cache
```http
GET /api/Student/check-cache-status
Authorization: Bearer {token}
```

#### Xóa cache cũ
```http
POST /api/Student/clear-old-cache
Authorization: Bearer {token}
```

#### Refresh cache
```http
POST /api/Student/refresh-cache
Authorization: Bearer {token}
```

### Quản lý dữ liệu trong Redis

#### Lấy sinh viên từ Redis
```http
GET /api/Student/get-student-from-redis/{studentCode}
Authorization: Bearer {token}
```

#### Cập nhật sinh viên trong Redis
```http
PUT /api/Student/update-student-in-redis
Authorization: Bearer {token}
Content-Type: application/json

{
  "studentId": 1,
  "studentCode": "SV001",
  "firstName": "Nguyễn Văn",
  "lastName": "A"
}
```

#### Xóa sinh viên khỏi Redis
```http
DELETE /api/Student/remove-student-from-redis/{studentCode}
Authorization: Bearer {token}
```

## Cache Key Patterns

### Sinh viên
- **Pattern**: `student:{studentCode}`
- **Ví dụ**: `student:SV001`
- **TTL**: 6 giờ

### StudentExamSession
- **Pattern**: `student_exam_session:{studentCode}:{studentExamSessionId}`
- **Ví dụ**: `student_exam_session:SV001:123`
- **TTL**: 6 giờ

### ShuffledExamPaper
- **Pattern**: `shuffled_exam_paper:{shuffledExamPaperId}`
- **Ví dụ**: `shuffled_exam_paper:456`
- **TTL**: 6 giờ

### Answer Key
- **Pattern**: `answer_key:{shuffledExamPaperId}`
- **Ví dụ**: `answer_key:456`
- **TTL**: 6 giờ

### Student Answers
- **Pattern**: `student_answers:{studentCode}:{shuffledExamPaperId}`
- **Ví dụ**: `student_answers:SV001:456`
- **TTL**: 6 giờ

## Logic xử lý thông minh

### Khi tải sinh viên lên cache:

1. **Kiểm tra tồn tại**: `student:{studentCode}` đã có trong Redis chưa?
2. **So sánh version**: Nếu đã tồn tại, so sánh `Version` và `UpdatedAt`
3. **Quyết định**:
   - **Thêm mới**: Nếu chưa tồn tại
   - **Cập nhật**: Nếu `Version` cao hơn hoặc `UpdatedAt` mới hơn
   - **Bỏ qua**: Nếu không có thay đổi

### Khi tải StudentExamSession lên cache:

1. **Kiểm tra tồn tại**: `student_exam_session:{studentCode}:{studentExamSessionId}` đã có chưa?
2. **So sánh dữ liệu**: Kiểm tra `Version`, `UpdatedAt`, `StudentAnswersString`, `IsCompleted`
3. **Quyết định**:
   - **Thêm mới**: Nếu chưa tồn tại
   - **Cập nhật**: Nếu có thay đổi trong bất kỳ trường nào
   - **Bỏ qua**: Nếu không có thay đổi

## Cách sử dụng

### 1. Khởi tạo cache khi khởi động ứng dụng

```csharp
// Tải tất cả dữ liệu lên Redis cache (thông minh)
var (success, message, cachedCounts) = await studentService.PreloadAllDataToRedisAsync();
if (success)
{
    logger.LogInformation("Đã tải thành công dữ liệu lên Redis cache: {Message}", message);
}
```

### 2. Kiểm tra trạng thái cache

```csharp
// Kiểm tra cache hiện tại
var (success, message, cacheInfo) = await studentService.CheckCacheStatusAsync();
if (success)
{
    logger.LogInformation("Cache status: {Message}", message);
    // cacheInfo chứa thông tin chi tiết về số lượng key trong cache
}
```

### 3. Refresh cache khi cần

```csharp
// Refresh toàn bộ cache
var (success, message, results) = await studentService.RefreshCacheAsync();
if (success)
{
    logger.LogInformation("Refresh cache thành công: {Message}", message);
}
```

### 4. Sử dụng trong các phương thức hiện có

```csharp
// Thay vì truy vấn database trực tiếp
var student = await _repository.GetQueryable().FirstOrDefaultAsync(x => x.StudentCode == studentCode);

// Sử dụng cache trước, fallback về database
var student = await GetStudentFromRedisAsync(studentCode);
if (student == null)
{
    student = await _repository.GetQueryable().FirstOrDefaultAsync(x => x.StudentCode == studentCode);
    if (student != null)
    {
        await UpdateStudentInRedisAsync(student);
    }
}
```

### 5. Cập nhật cache khi có thay đổi

```csharp
// Khi thêm sinh viên mới
var newStudent = await _repository.AddAsync(student);
await UpdateStudentInRedisAsync(newStudent);

// Khi cập nhật sinh viên
var updatedStudent = await _repository.UpdateAsync(student);
await UpdateStudentInRedisAsync(updatedStudent);

// Khi xóa sinh viên
await _repository.DeleteAsync(studentId);
await RemoveStudentFromRedisAsync(studentCode);
```

## Lợi ích của xử lý thông minh

1. **Hiệu suất cao**: Chỉ cập nhật khi cần thiết
2. **Tiết kiệm tài nguyên**: Giảm số lượng operations không cần thiết
3. **Tính nhất quán**: Đảm bảo dữ liệu luôn mới nhất
4. **Monitoring tốt**: Có thống kê chi tiết về số lượng thêm/cập nhật/bỏ qua
5. **Fault tolerance**: Xử lý lỗi deserialize và tự động cập nhật lại

## Lưu ý

1. **TTL**: Tất cả cache có TTL 6 giờ để tránh memory leak
2. **Batch operations**: Sử dụng Redis batch để tối ưu performance
3. **Error handling**: Có xử lý lỗi và logging đầy đủ
4. **Authorization**: Các API cache yêu cầu quyền Admin hoặc ITManager
5. **Consistency**: Đảm bảo tính nhất quán giữa cache và database
6. **Smart updates**: Chỉ cập nhật khi thực sự có thay đổi

## Monitoring

Có thể monitor Redis cache thông qua:

1. **Logs**: Tất cả operations đều có logging chi tiết
2. **Cache status**: API kiểm tra trạng thái cache
3. **Metrics**: Số lượng cache hits/misses và operations
4. **Health checks**: Kiểm tra kết nối Redis
5. **Memory usage**: Theo dõi memory usage của Redis 
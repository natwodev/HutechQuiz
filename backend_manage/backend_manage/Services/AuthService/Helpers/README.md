# StudentService Helper Classes

## Tổng quan
Đã tách các method helper từ `StudentService` thành các helper classes riêng biệt để cải thiện tính tổ chức và tái sử dụng code.

## Các Helper Classes

### 1. StudentCacheHelper
**Chức năng**: Xử lý cache Redis cho Student entity
**Methods**:
- `GetStudentFromRedisAsync(string studentCode)` - Lấy student từ Redis cache
- `UpdateStudentInRedisAsync(Student student)` - Cập nhật student vào Redis cache
- `RemoveStudentFromRedisAsync(string studentCode)` - Xóa student khỏi Redis cache
- `PreloadStudentsToRedisAsync(IEnumerable<Student> students)` - Tải danh sách students lên Redis

### 2. StudentExamSessionCacheHelper
**Chức năng**: Xử lý cache Redis cho StudentExamSession
**Methods**:
- `GetStudentExamSessionsFromRedisCacheAsync(string studentCode)` - Lấy sessions từ Redis cache
- `CacheStudentExamSessionsForStudentAsync(string studentCode, List<StudentExamSession> sessions)` - Cache sessions cho student
- `PreloadStudentExamSessionsToRedisAsync(IEnumerable<StudentExamSession> studentExamSessions)` - Tải sessions lên Redis
- `GetStudentExamSessionFromCacheAsync(IDatabase db, string sessionCacheKey, int shuffledExamPaperId)` - Lấy session cụ thể từ cache

### 3. ExamPaperHelper
**Chức năng**: Xử lý đề thi và answer key
**Methods**:
- `GetExamFromRedisAsync(int shuffledExamPaperId)` - Lấy đề thi từ Redis
- `GetExamFromDatabaseAsync(int shuffledExamPaperId)` - Lấy đề thi từ database
- `CacheExamPaperAsync(int shuffledExamPaperId, ShuffledExamPaperDto paperDto, string answerKey)` - Cache đề thi và answer key
- `GetRandomExamPaperAsync(int examSessionSubjectId)` - Lấy đề thi ngẫu nhiên
- `GetAnswerKeyAsync(int shuffledExamPaperId)` - Lấy answer key
- `ParseAnswerKey(string answerKeyString)` - Parse answer key string

### 4. StudentAnswerHelper
**Chức năng**: Xử lý đáp án của sinh viên
**Methods**:
- `UpdateSingleAnswerAsync(string studentCode, int shuffledExamPaperId, int index, string answer)` - Cập nhật đáp án đơn lẻ
- `UpdateStudentAnswersOptimizedAsync(RedisValue studentAnswers, List<SaveAnswerDto> saveAnswerDtos, string studentAnswerKey, IDatabase db)` - Cập nhật nhiều đáp án
- `ParseStudentAnswers(string answersString)` - Parse đáp án của sinh viên
- `CreateAnswersString(Dictionary<int, string> answers)` - Tạo chuỗi đáp án
- `CalculateScoreOptimized(Dictionary<int, string> currentAnswers, Dictionary<int, string> correctAnswerPairs)` - Tính điểm
- `CreateExamSubmissionMessage(...)` - Tạo message nộp bài
- `CreateSaveExamMessage(...)` - Tạo message lưu bài
- `CreateAnswerSavedMessage(...)` - Tạo message lưu đáp án

### 5. StudentValidationHelper
**Chức năng**: Validation logic
**Methods**:
- `ValidateStudentExamSessionAsync(string studentCode, int shuffledExamPaperId)` - Validate session từ cache và DB
- `ValidateStudentExamSessionOptimizedAsync(string studentCode, int shuffledExamPaperId, StudentExamSession? cachedSession)` - Validate tối ưu

## Cần làm tiếp:

### 1. Đăng ký DI Container
Thêm vào `Program.cs` hoặc `DependencyInjection.cs`:
```csharp
services.AddScoped<StudentCacheHelper>();
services.AddScoped<StudentExamSessionCacheHelper>();
services.AddScoped<ExamPaperHelper>();
services.AddScoped<StudentAnswerHelper>();
services.AddScoped<StudentValidationHelper>();
```

### 2. Xóa các method helper cũ trong StudentService
Cần xóa các method sau khỏi `StudentService`:
- `GetExamFromRedisAsync`
- `GetExamFromDatabaseAsync`
- `GetStudentFromRedisAsync`
- `UpdateStudentInRedisAsync`
- `RemoveStudentFromRedisAsync`
- `PreloadStudentsToRedisAsync`
- `GetStudentExamSessionsFromRedisCacheAsync`
- `CacheStudentExamSessionsForStudentAsync`
- `PreloadStudentExamSessionsToRedisAsync`
- `PreloadAllDataToRedisAsync`
- `CheckCacheStatusAsync`
- `ClearOldCacheAsync`
- `RefreshCacheAsync`
- `GetStudentExamSessionFromCacheAsync`
- `ValidateStudentExamSessionOptimizedAsync`
- `UpdateStudentAnswersOptimizedAsync`
- `ParseAnswerKey`
- `ParseStudentAnswers`
- `CreateAnswersString`
- `CalculateScoreOptimized`
- `CreateExamSubmissionMessage`
- `CreateSaveExamMessage`
- `ConvertToCacheDtoAsync`

### 3. Cập nhật StudentService
Đảm bảo `StudentService` sử dụng các helper classes thay vì các method cũ.

## Lợi ích:
1. **Tách biệt trách nhiệm**: Mỗi helper class có một trách nhiệm cụ thể
2. **Tái sử dụng**: Có thể sử dụng các helper classes ở nhiều nơi khác
3. **Dễ test**: Có thể test từng helper class riêng biệt
4. **Dễ maintain**: Code ngắn gọn và có tổ chức hơn
5. **Dependency Injection**: Có thể inject các dependencies cần thiết 
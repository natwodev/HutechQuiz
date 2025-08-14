




//                       _oo0oo_
//                      o8888888o
//                      88" . "88
//                      (| -_- |)
//                      0\  =  /0
//                    ___/`---'\___
//                  .' \\|     |// '.
//                 / \\|||  :  |||// \
//                / _||||| -:- |||||- \
//               |   | \\\  -  /// |   |
//               | \_|  ''\---/''  |_/ |
//               \  .-\__  '-'  ___/-. /
//             ___'. .'  /--.--\  `. .'___
//          ."" '<  `.___\_<|>_/___.' >' "".
//         | | :  `- \`.;`\ _ /`;.`/ - ` : | |
//         \  \ `_.   \_ __\ /__ _/   .-` /  /
//     =====`-.____`.___ \_____/___.-`___.-'=====
//                       `=---='
//
//     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//            Phật phù hộ, không bao giờ BUG
//     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

















# HutechQuiz - Backend Management System

## 📋 Tổng quan

HutechQuiz là hệ thống quản lý backend cho ứng dụng thi trực tuyến của trường Đại học Công nghệ TP.HCM (Hutech). Hệ thống cung cấp các API để quản lý các khía cạnh khác nhau của quá trình thi, bao gồm quản lý năm học, học kỳ, đợt thi, ca thi, và các thành phần liên quan.

## 🏗️ Kiến trúc hệ thống

```
backend_manage/
├── backend_manage/                 # Web API project
│   ├── Controllers/               # API controllers
│   ├── Program.cs                 # Application entry point
│   └── Dockerfile                 # Docker configuration
├── backend_manage.core/           # Core business logic
│   ├── Entities/                  # Domain entities
│   ├── Services/                  # Business services
│   ├── Repositories/              # Data access layer
│   └── Data/                      # Database context
└── backend_manage.shared/         # Shared DTOs and models
    └── DTOs/                      # Data transfer objects
```

## 🚀 Công nghệ sử dụng

- **.NET 9.0** - Framework chính
- **Entity Framework Core** - ORM
- **SQL Server** - Database
- **Redis** - Caching
- **RabbitMQ** - Message queuing
- **SignalR** - Real-time communication
- **JWT** - Authentication
- **AutoMapper** - Object mapping
- **Serilog** - Logging
- **Docker** - Containerization

## 📦 Cài đặt và chạy

### Yêu cầu hệ thống

- .NET 9.0 SDK
- SQL Server 2019+
- Redis 6.0+
- Docker (tùy chọn)

### Cách 1: Chạy trực tiếp

1. **Clone repository**
   ```bash
   git clone <repository-url>
   cd backend_manage
   ```

2. **Cấu hình database**
   - Cập nhật connection string trong `backend_manage/appsettings.json`
   - Chạy migrations:
   ```bash
   cd backend_manage
   dotnet ef database update
   ```

3. **Chạy ứng dụng**
   ```bash
   dotnet run
   ```

### Cách 2: Sử dụng Docker

1. **Build và chạy với Docker Compose**
   ```bash
   docker-compose up --build
   ```

2. **Hoặc build Docker image riêng**
   ```bash
   docker build -t backend_manage ./backend_manage
   docker run -p 8080:8080 backend_manage
   ```

## 🔐 Xác thực và phân quyền

Hệ thống sử dụng JWT (JSON Web Token) để xác thực. Các endpoint được bảo vệ theo role:

- **Admin**: Quản lý toàn bộ hệ thống
- **Lecturer**: Quản lý ca thi và bài thi
- **Student**: Tham gia thi

## 📚 API Documentation

### 1. API Năm học (AcademicYear)

| Method | Endpoint | Quyền | Mô tả |
|--------|----------|-------|-------|
| GET | `/api/AcademicYear` | Admin | Lấy danh sách tất cả năm học |
| GET | `/api/AcademicYear/{id}` | Admin | Lấy chi tiết năm học theo ID |
| POST | `/api/AcademicYear` | Admin | Tạo mới năm học |
| PUT | `/api/AcademicYear/{id}` | Admin | Cập nhật năm học |
| DELETE | `/api/AcademicYear/{id}` | Admin | Xóa năm học |

### 2. API Học kỳ (Semester)

| Method | Endpoint | Quyền | Mô tả |
|--------|----------|-------|-------|
| GET | `/api/Semester` | Admin | Lấy danh sách tất cả học kỳ |
| GET | `/api/Semester/{id}` | Admin | Lấy chi tiết học kỳ theo ID |
| POST | `/api/Semester` | Admin | Tạo mới học kỳ |
| PUT | `/api/Semester/{id}` | Admin | Cập nhật học kỳ |
| DELETE | `/api/Semester/{id}` | Admin | Xóa học kỳ |

### 3. API Đợt thi (ExamBatch)

| Method | Endpoint | Quyền | Mô tả |
|--------|----------|-------|-------|
| GET | `/api/ExamBatch` | Admin | Lấy danh sách tất cả đợt thi |
| GET | `/api/ExamBatch/{id}` | Admin | Lấy chi tiết đợt thi theo ID |
| POST | `/api/ExamBatch` | Admin | Tạo mới đợt thi |
| PUT | `/api/ExamBatch/{id}` | Admin | Cập nhật đợt thi |
| DELETE | `/api/ExamBatch/{id}` | Admin | Xóa đợt thi |

### 4. API Chi tiết đợt thi (ExamBatchDetail)

| Method | Endpoint | Quyền | Mô tả |
|--------|----------|-------|-------|
| GET | `/api/ExamBatchDetail` | Admin | Lấy danh sách chi tiết đợt thi |
| GET | `/api/ExamBatchDetail/{id}` | Admin | Lấy chi tiết đợt thi theo ID |
| POST | `/api/ExamBatchDetail` | Admin | Tạo mới chi tiết đợt thi |
| PUT | `/api/ExamBatchDetail/{id}` | Admin | Cập nhật chi tiết đợt thi |
| DELETE | `/api/ExamBatchDetail/{id}` | Admin | Xóa chi tiết đợt thi |

### 5. API Ca thi (ExamSession)

| Method | Endpoint | Quyền | Mô tả |
|--------|----------|-------|-------|
| GET | `/api/ExamSession` | Admin | Lấy danh sách tất cả ca thi |
| GET | `/api/ExamSession/{id}` | Admin | Lấy chi tiết ca thi theo ID |
| POST | `/api/ExamSession` | Admin | Tạo mới ca thi |
| PUT | `/api/ExamSession/{id}` | Admin | Cập nhật ca thi |
| DELETE | `/api/ExamSession/{id}` | Admin | Xóa ca thi |

### 6. API Khoa tham gia ca thi (ExamSessionDepartment)

| Method | Endpoint | Quyền | Mô tả |
|--------|----------|-------|-------|
| GET | `/api/ExamSessionDepartment` | Admin | Lấy danh sách khoa tham gia ca thi |
| GET | `/api/ExamSessionDepartment/{id}` | Admin | Lấy chi tiết khoa tham gia ca thi |
| POST | `/api/ExamSessionDepartment` | Admin | Thêm khoa tham gia ca thi |
| PUT | `/api/ExamSessionDepartment/{id}` | Admin | Cập nhật khoa tham gia ca thi |
| DELETE | `/api/ExamSessionDepartment/{id}` | Admin | Xóa khoa tham gia ca thi |

### 7. API Ca thi môn học (ExamSessionSubject)

| Method | Endpoint | Quyền | Mô tả |
|--------|----------|-------|-------|
| GET | `/api/ExamSessionSubject` | Admin | Lấy danh sách ca thi môn học |
| GET | `/api/ExamSessionSubject/{id}` | Admin | Lấy chi tiết ca thi môn học |
| POST | `/api/ExamSessionSubject` | Admin | Tạo mới ca thi môn học |
| PUT | `/api/ExamSessionSubject/{id}` | Admin | Cập nhật ca thi môn học |
| DELETE | `/api/ExamSessionSubject/{id}` | Admin | Xóa ca thi môn học |
| PATCH | `/api/ExamSessionSubject/{id}/original-exam-paper/{originalExamPaperId}` | Admin | Cập nhật đề thi gốc |
| GET | `/api/ExamSessionSubject/with-rooms` | Admin | Lấy ca thi môn học với phòng thi |

## 🔄 Quy trình tạo ca thi

1. **Tạo năm học** → Định nghĩa năm học mới
2. **Tạo học kỳ** → Thêm học kỳ vào năm học
3. **Tạo đợt thi** → Thiết lập đợt thi cho học kỳ
4. **Tạo chi tiết đợt thi** → Cấu hình các thông tin chi tiết
5. **Tạo ca thi** → Thiết lập ca thi cho đợt thi
6. **Thêm khoa tham gia** → Gán khoa vào ca thi
7. **Tạo ca thi môn học** → Thiết lập môn học cho ca thi

## 🐳 Deployment

### Docker Deployment

1. **Build image**
   ```bash
   docker build -t backend_manage ./backend_manage
   ```

2. **Run container**
   ```bash
   docker run -d \
     --name backend_manage \
     -p 8080:8080 \
     -e ConnectionStrings__DefaultConnection="your-connection-string" \
     -e Redis__ConnectionString="your-redis-connection" \
     backend_manage
   ```

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | - |
| `Redis__ConnectionString` | Redis connection string | `localhost:6379` |
| `Jwt__Secret` | JWT secret key | - |
| `Jwt__Issuer` | JWT issuer | - |
| `Jwt__Audience` | JWT audience | - |

## 📊 Monitoring và Logging

- **Serilog** được cấu hình để ghi log vào console và file
- **Health checks** cho Redis và database
- **Performance monitoring** với Kestrel server optimization

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 Support

Để được hỗ trợ:
- Tạo issue trên GitHub
- Liên hệ team phát triển
- Xem documentation chi tiết trong code

## 🔄 Version History

- **v1.0.0** - Initial release
- **v1.1.0** - Added Redis caching
- **v1.2.0** - Added SignalR notifications
- **v1.3.0** - Added RabbitMQ integration








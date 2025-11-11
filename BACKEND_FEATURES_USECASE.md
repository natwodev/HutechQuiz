# HutechQuiz Backend - Tài liệu Chức năng và Use Case

## 📋 Mục lục
- [Tổng quan hệ thống](#tổng-quan-hệ-thống)
- [Actors (Người dùng)](#actors-người-dùng)
- [Phân tích chức năng theo Module](#phân-tích-chức-năng-theo-module)
- [Use Case Diagram Tổng quát](#use-case-diagram-tổng-quát)
- [Chi tiết Use Cases](#chi-tiết-use-cases)

---

## 🎯 Tổng quan hệ thống

**HutechQuiz Backend** là hệ thống quản lý thi trực tuyến với kiến trúc phân tầng, hỗ trợ:
- Quản lý tổ chức thi (năm học, học kỳ, đợt thi, ca thi)
- Quản lý đề thi (nhập từ XML, tạo đề hoán vị)
- Quản lý sinh viên và giảng viên
- Thực hiện thi trực tuyến (làm bài, nộp bài, chấm điểm)
- Giám sát phòng thi (real-time với SignalR)
- Phân quyền chi tiết theo vai trò

**Công nghệ:**
- .NET 9.0, ASP.NET Core Web API
- Entity Framework Core + SQL Server
- Redis (Caching), RabbitMQ (Message Queue)
- SignalR (Real-time), JWT (Authentication)

---

## 👥 Actors (Người dùng)

### 1. **Admin (Quản trị viên)**
   - Quản lý toàn bộ hệ thống với quyền cao nhất
   - **Quản lý cấu trúc học vụ:**
     - Tạo, sửa, xóa năm học
     - Tạo, sửa, xóa học kỳ
     - Quản lý khoa/phòng ban
   - **Quản lý tổ chức thi:**
     - Tạo và quản lý đợt thi
     - Tạo và quản lý ca thi
     - Thiết lập ca thi môn học
     - Phân công giảng viên coi thi
     - Gán phòng thi và đề thi
   - **Quản lý đề thi:**
     - Import đề thi từ file XML
     - Xem chi tiết đề thi gốc
     - Tạo đề thi hoán vị tự động
     - Xem chi tiết đề hoán vị
   - **Quản lý sinh viên:**
     - Import danh sách sinh viên từ Excel
     - Xem thông tin sinh viên
     - Kích hoạt/vô hiệu đăng nhập sinh viên
     - Thêm thời gian làm bài cho sinh viên
     - Xem bảng điểm theo ca thi
   - **Quản lý giảng viên:**
     - Thêm giảng viên mới
     - Xem danh sách giảng viên
     - Cập nhật thông tin giảng viên
   - **Quản lý hệ thống:**
     - Gửi thông báo real-time
     - Giám sát trạng thái RabbitMQ
     - Quản lý Redis cache
     - Xem logs hệ thống

### 2. **AcademicAffairs (Phòng Đào tạo)**
   - Quản lý cấu trúc học vụ (năm học, học kỳ, đợt thi)
   - Quản lý ca thi và môn thi
   - Xem báo cáo

### 3. **Lecturer (Giảng viên)**
   - Xem lịch coi thi được phân công
   - Giám sát phòng thi
   - Nộp bài thay sinh viên (force submit)
   - Xuất điểm sinh viên

### 4. **Student (Sinh viên)**
   - Đăng nhập bằng mã sinh viên
   - Xem lịch thi
   - Làm bài thi trực tuyến
   - Nộp bài thi
   - Xem kết quả

### 5. **ExamManager (Quản lý thi)**
   - Quản lý đề thi
   - Phân công giảng viên coi thi
   - Giám sát quá trình thi

### 6. **ITManager (Quản lý IT)**
   - Quản lý hệ thống kỹ thuật
   - Giám sát queue, Redis
   - Xử lý sự cố kỹ thuật

---

## 📊 Phân tích chức năng theo Module

### **MODULE 1: QUẢN LÝ XÁC THỰC (Authentication)**

#### Controller: `AuthController`
| STT | Chức năng | Endpoint | Method | Quyền | Mô tả |
|-----|-----------|----------|--------|-------|-------|
| 1 | Đăng nhập hệ thống | `/api/auth/login` | POST | Public | Đăng nhập cho Admin, AcademicAffairs, ExamManager, ITManager |
| 2 | Đăng xuất | `/api/auth/logout` | POST | Authenticated | Hủy token JWT |
| 3 | Đăng nhập giảng viên | `/api/auth/lecturer-login` | POST | Public | Đăng nhập bằng 2 mã giảng viên |

**Use Cases:**
- UC001: Đăng nhập hệ thống (Admin/Staff)
- UC002: Đăng nhập giảng viên
- UC003: Đăng xuất

---

### **MODULE 2: QUẢN LÝ CẤU TRÚC HỌC VỤ**

#### 2.1. Quản lý Năm học (AcademicYear)
**Controller:** `AcademicYearController`

| STT | Chức năng | Endpoint | Method | Quyền |
|-----|-----------|----------|--------|-------|
| 1 | Xem danh sách năm học | `/api/AcademicYear` | GET | AcademicAffairs/Admin |
| 2 | Xem chi tiết năm học | `/api/AcademicYear/{id}` | GET | AcademicAffairs/Admin |
| 3 | Tạo năm học mới | `/api/AcademicYear` | POST | AcademicAffairs/Admin |
| 4 | Cập nhật năm học | `/api/AcademicYear/{id}` | PUT | AcademicAffairs/Admin |
| 5 | Xóa năm học | `/api/AcademicYear/{id}` | DELETE | AcademicAffairs/Admin |

**Use Cases:**
- UC010: Quản lý năm học (CRUD)

#### 2.2. Quản lý Học kỳ (Semester)
**Controller:** `SemesterController`

| STT | Chức năng | Endpoint | Method | Quyền |
|-----|-----------|----------|--------|-------|
| 1 | Xem danh sách học kỳ | `/api/Semester` | GET | AcademicAffairs/Admin |
| 2 | Xem chi tiết học kỳ | `/api/Semester/{id}` | GET | AcademicAffairs/Admin |
| 3 | Tạo học kỳ mới | `/api/Semester` | POST | AcademicAffairs/Admin |
| 4 | Cập nhật học kỳ | `/api/Semester/{id}` | PUT | AcademicAffairs/Admin |
| 5 | Xóa học kỳ | `/api/Semester/{id}` | DELETE | AcademicAffairs/Admin |

**Use Cases:**
- UC011: Quản lý học kỳ (CRUD)

#### 2.3. Quản lý Khoa (Department)
**Controller:** `DepartmentController`

| STT | Chức năng | Endpoint | Method | Quyền |
|-----|-----------|----------|--------|-------|
| 1 | Xem danh sách khoa | `/api/Department` | GET | Admin |
| 2 | Xem chi tiết khoa | `/api/Department/{id}` | GET | Admin |
| 3 | Tạo khoa mới | `/api/Department` | POST | Admin |
| 4 | Cập nhật khoa | `/api/Department/{id}` | PUT | Admin |
| 5 | Xóa khoa | `/api/Department/{id}` | DELETE | Admin |

**Use Cases:**
- UC012: Quản lý khoa (CRUD)

---

### **MODULE 3: QUẢN LÝ ĐỢT THI VÀ CA THI**

#### 3.1. Quản lý Đợt thi (ExamBatch)
**Controller:** `ExamBatchController`

| STT | Chức năng | Endpoint | Method | Quyền |
|-----|-----------|----------|--------|-------|
| 1 | Xem danh sách đợt thi | `/api/ExamBatch` | GET | AcademicAffairs/Admin |
| 2 | Xem chi tiết đợt thi | `/api/ExamBatch/{id}` | GET | AcademicAffairs/Admin |
| 3 | Tạo đợt thi mới | `/api/ExamBatch` | POST | AcademicAffairs/Admin |
| 4 | Cập nhật đợt thi | `/api/ExamBatch/{id}` | PUT | AcademicAffairs/Admin |
| 5 | Xóa đợt thi | `/api/ExamBatch/{id}` | DELETE | AcademicAffairs/Admin |

**Use Cases:**
- UC020: Quản lý đợt thi (CRUD)

#### 3.2. Quản lý Chi tiết đợt thi (ExamBatchDetail)
**Controller:** `ExamBatchDetailController`

| STT | Chức năng | Endpoint | Method | Quyền |
|-----|-----------|----------|--------|-------|
| 1 | Xem danh sách chi tiết đợt thi | `/api/ExamBatchDetail` | GET | AcademicAffairs/Admin |
| 2 | Xem chi tiết | `/api/ExamBatchDetail/{id}` | GET | AcademicAffairs/Admin |
| 3 | Tạo chi tiết đợt thi | `/api/ExamBatchDetail` | POST | AcademicAffairs/Admin |
| 4 | Cập nhật chi tiết đợt thi | `/api/ExamBatchDetail/{id}` | PUT | AcademicAffairs/Admin |
| 5 | Xóa chi tiết đợt thi | `/api/ExamBatchDetail/{id}` | DELETE | AcademicAffairs/Admin |

**Use Cases:**
- UC021: Quản lý chi tiết đợt thi (CRUD)

#### 3.3. Quản lý Ca thi (ExamSession)
**Controller:** `ExamSessionController`

| STT | Chức năng | Endpoint | Method | Quyền |
|-----|-----------|----------|--------|-------|
| 1 | Xem danh sách ca thi | `/api/ExamSession` | GET | AcademicAffairs/Admin |
| 2 | Xem chi tiết ca thi | `/api/ExamSession/{id}` | GET | AcademicAffairs/Admin |
| 3 | Tạo ca thi mới | `/api/ExamSession` | POST | AcademicAffairs/Admin |
| 4 | Cập nhật ca thi | `/api/ExamSession/{id}` | PUT | AcademicAffairs/Admin |
| 5 | Xóa ca thi | `/api/ExamSession/{id}` | DELETE | AcademicAffairs/Admin |

**Use Cases:**
- UC022: Quản lý ca thi (CRUD)

#### 3.4. Quản lý Ca thi môn học (ExamSessionSubject)
**Controller:** `ExamSessionSubjectController`

| STT | Chức năng | Endpoint | Method | Quyền |
|-----|-----------|----------|--------|-------|
| 1 | Xem danh sách ca thi môn học | `/api/ExamSessionSubject` | GET | Authenticated |
| 2 | Xem chi tiết ca thi môn học | `/api/ExamSessionSubject/{id}` | GET | Authenticated |
| 3 | Tạo ca thi môn học | `/api/ExamSessionSubject` | POST | Authenticated |
| 4 | Cập nhật ca thi môn học | `/api/ExamSessionSubject/{id}` | PUT | Authenticated |
| 5 | Xóa ca thi môn học | `/api/ExamSessionSubject/{id}` | DELETE | Authenticated |
| 6 | Lấy theo phòng thi | `/api/ExamSessionSubject/room/{examRoomId}` | GET | Authenticated |
| 7 | Lấy theo giảng viên | `/api/ExamSessionSubject/lecturer/{lecturerId}` | GET | Authenticated |
| 8 | Kiểm tra đang mở | `/api/ExamSessionSubject/{id}/is-open` | GET | Authenticated |
| 9 | Phân công giảng viên | `/api/ExamSessionSubject/assign-lecturer` | POST | Authenticated |
| 10 | Hủy phân công giảng viên | `/api/ExamSessionSubject/unassign-lecturer` | POST | Authenticated |
| 11 | Cập nhật phòng thi | `/api/ExamSessionSubject/{id}/exam-room` | PUT | Authenticated |
| 12 | Cập nhật đề thi gốc | `/api/ExamSessionSubject/{id}/original-exam-paper` | PUT | Authenticated |
| 13 | Cập nhật trạng thái hoạt động | `/api/ExamSessionSubject/is-active` | POST | Authenticated |
| 14 | Xem ca thi với sinh viên | `/api/ExamSessionSubject/{id}/with-students` | GET | Authenticated |
| 15 | Xem trạng thái phòng thi của giảng viên | `/api/ExamSessionSubject/lecturer/subject-exam-room-status` | GET | Lecturer |

**Use Cases:**
- UC023: Quản lý ca thi môn học (CRUD)
- UC024: Phân công giảng viên coi thi
- UC025: Cập nhật phòng thi và đề thi
- UC026: Kích hoạt/Vô hiệu hóa ca thi

---

### **MODULE 4: QUẢN LÝ ĐỀ THI**

#### 4.1. Quản lý Đề thi gốc (OriginalExamPaper)
**Controller:** `OriginalExamPaperController`

| STT | Chức năng | Endpoint | Method | Quyền |
|-----|-----------|----------|--------|-------|
| 1 | Import đề thi từ XML | `/api/OriginalExamPaper/import-xml` | POST | Admin |
| 2 | Xem đề thi với chi tiết | `/api/OriginalExamPaper/{core}/with-details` | GET | Admin |
| 3 | Tạo đề thi hoán vị | `/api/OriginalExamPaper/create-shuffled` | POST | Admin |

**Use Cases:**
- UC030: Import đề thi từ file XML
- UC031: Xem chi tiết đề thi gốc
- UC032: Tạo đề thi hoán vị tự động

#### 4.2. Quản lý Đề thi hoán vị (ShuffledExamPaper)
**Controller:** `ShuffledExamPaperController`

| STT | Chức năng | Endpoint | Method | Quyền |
|-----|-----------|----------|--------|-------|
| 1 | Xem đề hoán vị với chi tiết | `/api/ShuffledExamPaper/{core}/with-details` | GET | Admin |

**Use Cases:**
- UC033: Xem chi tiết đề thi hoán vị

---

### **MODULE 5: QUẢN LÝ SINH VIÊN**

#### Controller: `StudentController`

| STT | Chức năng | Endpoint | Method | Quyền |
|-----|-----------|----------|--------|-------|
| 1 | Đăng nhập sinh viên | `/api/Student/login` | POST | Public |
| 2 | Import sinh viên từ Excel | `/api/Student/import-excel` | POST | Admin |
| 3 | Xem thông tin sinh viên theo mã | `/api/Student/by-code/{studentCode}` | GET | Authenticated |
| 4 | Xem profile sinh viên | `/api/Student/profile` | GET | Student |
| 5 | Bắt đầu làm bài thi | `/api/Student/start-exam` | POST | Student |
| 6 | Xem lịch thi của sinh viên | `/api/Student/exam-sessions` | GET | Student |
| 7 | Xem sinh viên theo ca thi môn học | `/api/Student/by-exam-session-subject` | GET | Authenticated |
| 8 | Thêm thời gian làm bài | `/api/Student/extra-minutes` | POST | Admin/Lecturer |
| 9 | Lưu đáp án | `/api/Student/save-answer` | POST | Student |
| 10 | Nộp bài thi | `/api/Student/submit-exam` | POST | Student |
| 11 | Kích hoạt/vô hiệu đăng nhập | `/api/Student/active-login` | POST | Admin |
| 12 | Xem điểm theo ca thi môn học | `/api/Student/grades/{examSessionSubjectId}` | GET | Admin/Lecturer |
| 13 | Xuất điểm ra Excel | `/api/Student/grades/{examSessionSubjectId}/export` | GET | Lecturer |
| 14 | Lấy kết quả nộp bài | `/api/Student/get-submission-result` | POST | Student |

**Use Cases:**
- UC040: Đăng nhập sinh viên
- UC041: Import danh sách sinh viên từ Excel
- UC042: Quản lý thông tin sinh viên
- UC043: Bắt đầu làm bài thi
- UC044: Lưu đáp án trong quá trình thi
- UC045: Nộp bài thi
- UC046: Xem kết quả thi
- UC047: Thêm thời gian làm bài (trường hợp đặc biệt)
- UC048: Xuất bảng điểm

---

### **MODULE 6: QUẢN LÝ GIẢNG VIÊN**

#### Controller: `LecturerController`

| STT | Chức năng | Endpoint | Method | Quyền |
|-----|-----------|----------|--------|-------|
| 1 | Thêm giảng viên | `/api/Lecturer` | POST | Admin |
| 2 | Xem danh sách giảng viên | `/api/Lecturer` | GET | Admin |
| 3 | Xem thông tin giảng viên | `/api/Lecturer/{lecturerCode}` | GET | Admin |
| 4 | Xem profile giảng viên | `/api/Lecturer/profile` | GET | Lecturer |
| 5 | Nộp bài thay sinh viên (Force Submit) | `/api/Lecturer/force-submit` | POST | Lecturer |
| 6 | Reset thời gian bắt đầu thi | `/api/Lecturer/reset-exam-session-start-time` | POST | Authenticated |

**Use Cases:**
- UC050: Quản lý thông tin giảng viên
- UC051: Xem lịch coi thi
- UC052: Giám sát phòng thi
- UC053: Nộp bài thay sinh viên (trường hợp đặc biệt)

---

### **MODULE 7: THÔNG BÁO VÀ GIÁM SÁT**

#### 7.1. Thông báo (Notification)
**Controller:** `NotificationController`

| STT | Chức năng | Endpoint | Method | Quyền |
|-----|-----------|----------|--------|-------|
| 1 | Gửi thông báo nhắc giờ thi | `/api/Notification/send-exam-reminder` | POST | Admin |

**Use Cases:**
- UC060: Gửi thông báo nhắc nhở

#### 7.2. Giám sát hệ thống (QueueStatus)
**Controller:** `QueueStatusController`

| STT | Chức năng | Endpoint | Method | Quyền |
|-----|-----------|----------|--------|-------|
| 1 | Kiểm tra trạng thái Queue | `/api/QueueStatus/check` | GET | Admin/ITManager |

**Use Cases:**
- UC061: Giám sát trạng thái RabbitMQ

---

## 🎨 Use Case Diagram Tổng quát

### Nhóm Use Case chính:

```
┌─────────────────────────────────────────────────────────────┐
│                   HUTECHQUIZ BACKEND SYSTEM                  │
└─────────────────────────────────────────────────────────────┘

┌────────────────┐                                  ┌──────────────────┐
│     Admin      │                                  │ AcademicAffairs  │
└────────────────┘                                  └──────────────────┘
        │                                                     │
        ├── UC010: Quản lý năm học                          │
        ├── UC011: Quản lý học kỳ                           ├─────────┐
        ├── UC012: Quản lý khoa                             │         │
        ├── UC020: Quản lý đợt thi ────────────────────────┤         │
        ├── UC021: Quản lý chi tiết đợt thi ───────────────┤         │
        ├── UC022: Quản lý ca thi ─────────────────────────┤         │
        ├── UC023: Quản lý ca thi môn học ─────────────────┤         │
        ├── UC030: Import đề thi từ XML                     │         │
        ├── UC032: Tạo đề thi hoán vị                       │         │
        ├── UC041: Import sinh viên từ Excel                │         │
        ├── UC042: Quản lý sinh viên                        │         │
        ├── UC050: Quản lý giảng viên                       │         │
        ├── UC024: Phân công giảng viên ────────────────────┘         │
        └── UC061: Giám sát hệ thống                                  │
                                                                       │
┌────────────────┐                                  ┌──────────────────┐
│    Lecturer    │                                  │  ExamManager     │
└────────────────┘                                  └──────────────────┘
        │                                                     │
        ├── UC002: Đăng nhập giảng viên                     │
        ├── UC051: Xem lịch coi thi                         ├─────────┐
        ├── UC052: Giám sát phòng thi                       │         │
        ├── UC053: Nộp bài thay sinh viên                   │         │
        └── UC048: Xuất bảng điểm                           │         │
                                                             │         │
┌────────────────┐                                           │         │
│    Student     │                                           │         │
└────────────────┘                                           │         │
        │                                                     │         │
        ├── UC040: Đăng nhập sinh viên                      │         │
        ├── UC043: Bắt đầu làm bài thi                      │         │
        ├── UC044: Lưu đáp án                               │         │
        ├── UC045: Nộp bài thi                              │         │
        └── UC046: Xem kết quả thi                          │         │
                                                             │         │
┌────────────────┐                                           │         │
│   ITManager    │                                           │         │
└────────────────┘                                           │         │
        │                                                     │         │
        ├── UC061: Giám sát hệ thống ───────────────────────┘         │
        └── UC062: Quản lý Redis/RabbitMQ                             │
```

---

## 📖 Chi tiết Use Cases

### **NHÓM 1: XÁC THỰC (Authentication)**

#### UC001: Đăng nhập hệ thống
- **Actor:** Admin, AcademicAffairs, ExamManager, ITManager
- **Mô tả:** Người dùng đăng nhập bằng email và password
- **Luồng chính:**
  1. Người dùng nhập email và password
  2. Hệ thống xác thực thông tin
  3. Hệ thống tạo JWT token
  4. Trả về token cho client

#### UC002: Đăng nhập giảng viên
- **Actor:** Lecturer
- **Mô tả:** Giảng viên đăng nhập bằng 2 mã giảng viên
- **Luồng chính:**
  1. Giảng viên nhập LecturerCode1 và LecturerCode2
  2. Hệ thống xác thực 2 mã
  3. Hệ thống tạo JWT token với role Lecturer
  4. Trả về token

#### UC003: Đăng xuất
- **Actor:** Authenticated User
- **Mô tả:** Đăng xuất khỏi hệ thống
- **Luồng chính:**
  1. Người dùng gửi yêu cầu logout với token
  2. Hệ thống blacklist token
  3. Trả về thông báo thành công

---

### **NHÓM 2: QUẢN LÝ CẤU TRÚC HỌC VỤ**

#### UC010: Quản lý năm học
- **Actor:** Admin, AcademicAffairs
- **Mô tả:** CRUD năm học (VD: 2024-2025)
- **Chức năng:**
  - Tạo năm học mới
  - Xem danh sách năm học
  - Cập nhật thông tin năm học
  - Xóa năm học

#### UC011: Quản lý học kỳ
- **Actor:** Admin, AcademicAffairs
- **Mô tả:** CRUD học kỳ (VD: HK1, HK2, HK3)
- **Chức năng:**
  - Tạo học kỳ mới thuộc năm học
  - Xem danh sách học kỳ
  - Cập nhật thông tin học kỳ
  - Xóa học kỳ

#### UC012: Quản lý khoa
- **Actor:** Admin
- **Mô tả:** CRUD khoa/phòng ban
- **Chức năng:**
  - Tạo khoa mới
  - Xem danh sách khoa
  - Cập nhật thông tin khoa
  - Xóa khoa

---

### **NHÓM 3: QUẢN LÝ ĐỢT THI VÀ CA THI**

#### UC020: Quản lý đợt thi
- **Actor:** Admin, AcademicAffairs
- **Mô tả:** Tạo và quản lý đợt thi (VD: Đợt thi GK, CK)
- **Chức năng:**
  - Tạo đợt thi mới
  - Xem danh sách đợt thi
  - Cập nhật thông tin đợt thi
  - Xóa đợt thi

#### UC021: Quản lý chi tiết đợt thi
- **Actor:** Admin, AcademicAffairs
- **Mô tả:** Quản lý thông tin chi tiết của đợt thi
- **Chức năng:**
  - Thêm chi tiết đợt thi
  - Cập nhật thông tin
  - Xóa chi tiết

#### UC022: Quản lý ca thi
- **Actor:** Admin, AcademicAffairs
- **Mô tả:** Tạo và quản lý ca thi (VD: Ca 1: 7h-9h, Ca 2: 9h30-11h30)
- **Chức năng:**
  - Tạo ca thi mới
  - Xem danh sách ca thi
  - Cập nhật ca thi
  - Xóa ca thi

#### UC023: Quản lý ca thi môn học
- **Actor:** Admin, AcademicAffairs, ExamManager
- **Mô tả:** Gán môn học vào ca thi cụ thể
- **Chức năng:**
  - Tạo ca thi môn học
  - Cập nhật thông tin
  - Gán phòng thi
  - Gán đề thi gốc
  - Kích hoạt/vô hiệu hóa

#### UC024: Phân công giảng viên coi thi
- **Actor:** Admin, ExamManager
- **Mô tả:** Phân công giảng viên coi thi cho ca thi môn học
- **Luồng chính:**
  1. Chọn ca thi môn học
  2. Chọn giảng viên coi thi
  3. Xác nhận phân công
  4. Hệ thống lưu thông tin

#### UC025: Cập nhật phòng thi và đề thi
- **Actor:** Admin, ExamManager
- **Mô tả:** Cập nhật phòng thi và đề thi gốc cho ca thi môn học
- **Chức năng:**
  - Gán phòng thi
  - Gán đề thi gốc
  - Thay đổi phòng/đề

#### UC026: Kích hoạt/Vô hiệu hóa ca thi
- **Actor:** Admin, ExamManager
- **Mô tả:** Bật/tắt ca thi môn học
- **Luồng chính:**
  1. Chọn ca thi môn học
  2. Chuyển trạng thái active/inactive
  3. Hệ thống cập nhật

---

### **NHÓM 4: QUẢN LÝ ĐỀ THI**

#### UC030: Import đề thi từ XML
- **Actor:** Admin
- **Mô tả:** Import đề thi từ file XML
- **Luồng chính:**
  1. Upload file XML
  2. Nhập mã đề thi gốc
  3. Hệ thống parse XML
  4. Lưu đề thi vào database
  5. Tạo cấu trúc câu hỏi và đáp án

#### UC031: Xem chi tiết đề thi gốc
- **Actor:** Admin
- **Mô tả:** Xem đầy đủ thông tin đề thi gốc
- **Thông tin hiển thị:**
  - Thông tin đề thi
  - Danh sách câu hỏi
  - Chi tiết đáp án
  - Điểm số

#### UC032: Tạo đề thi hoán vị tự động
- **Actor:** Admin
- **Mô tả:** Tạo nhiều đề hoán vị từ đề gốc
- **Luồng chính:**
  1. Chọn đề thi gốc
  2. Nhập số lượng đề cần tạo
  3. Hệ thống tạo đề hoán vị:
     - Hoán vị thứ tự câu hỏi
     - Hoán vị thứ tự đáp án
  4. Lưu các đề hoán vị

#### UC033: Xem chi tiết đề thi hoán vị
- **Actor:** Admin
- **Mô tả:** Xem thông tin đề thi đã hoán vị

---

### **NHÓM 5: QUẢN LÝ SINH VIÊN VÀ THI**

#### UC040: Đăng nhập sinh viên
- **Actor:** Student
- **Mô tả:** Sinh viên đăng nhập bằng 2 mã sinh viên
- **Luồng chính:**
  1. Nhập StudentCode1 và StudentCode2
  2. Hệ thống xác thực
  3. Tạo JWT token với role Student
  4. Trả về token và thông tin sinh viên

#### UC041: Import sinh viên từ Excel
- **Actor:** Admin
- **Mô tả:** Import danh sách sinh viên từ file Excel
- **Luồng chính:**
  1. Upload file Excel
  2. Nhập mã ca thi môn học
  3. Hệ thống đọc file Excel
  4. Tạo/cập nhật thông tin sinh viên
  5. Tạo StudentExamSession (phiên thi)
  6. Trả về số lượng sinh viên đã import

#### UC042: Quản lý thông tin sinh viên
- **Actor:** Admin
- **Mô tả:** Xem và quản lý thông tin sinh viên
- **Chức năng:**
  - Xem danh sách sinh viên
  - Xem thông tin chi tiết
  - Cập nhật thông tin
  - Kích hoạt/vô hiệu đăng nhập

#### UC043: Bắt đầu làm bài thi
- **Actor:** Student
- **Mô tả:** Sinh viên bắt đầu làm bài thi
- **Luồng chính:**
  1. Chọn phiên thi
  2. Hệ thống kiểm tra:
     - Thời gian thi hợp lệ
     - Ca thi đang mở
     - Chưa nộp bài
  3. Lấy đề thi hoán vị
  4. Tạo/cập nhật StudentExamSession
  5. Ghi nhận thời gian bắt đầu
  6. Trả về đề thi

#### UC044: Lưu đáp án trong quá trình thi
- **Actor:** Student
- **Mô tả:** Lưu đáp án từng câu trong quá trình làm bài
- **Luồng chính:**
  1. Sinh viên chọn đáp án
  2. Gửi request với key (số câu) và value (đáp án)
  3. Hệ thống validate:
     - Thời gian còn hiệu lực
     - Chưa nộp bài
  4. Cập nhật đáp án vào StudentExamSession
  5. Gửi message vào RabbitMQ (student_answer_saved_queue)
  6. Lưu vào Redis (cache)
  7. Trả về trạng thái thành công

#### UC045: Nộp bài thi
- **Actor:** Student
- **Mô tả:** Nộp bài thi khi hoàn thành
- **Luồng chính:**
  1. Sinh viên click "Nộp bài"
  2. Hệ thống kiểm tra:
     - Đã bắt đầu làm bài
     - Chưa nộp bài trước đó
  3. Lấy đáp án từ Redis (nếu có)
  4. Cập nhật trạng thái IsSubmitted = true
  5. Ghi nhận thời gian nộp bài
  6. Gửi message vào RabbitMQ (exam_submission_queue)
  7. Chấm điểm tự động
  8. Lưu kết quả
  9. Trả về thông báo thành công

#### UC046: Xem kết quả thi
- **Actor:** Student
- **Mô tả:** Xem điểm và kết quả sau khi nộp bài
- **Luồng chính:**
  1. Gửi yêu cầu xem kết quả
  2. Hệ thống kiểm tra đã nộp bài
  3. Trả về thông tin:
     - Điểm số
     - Thời gian làm bài
     - Trạng thái

#### UC047: Thêm thời gian làm bài
- **Actor:** Admin, Lecturer
- **Mô tả:** Thêm thời gian cho sinh viên (trường hợp đặc biệt)
- **Luồng chính:**
  1. Chọn sinh viên
  2. Nhập số phút thêm
  3. Nhập lý do
  4. Hệ thống cập nhật ExtraMinutes
  5. Ghi log

#### UC048: Xuất bảng điểm
- **Actor:** Lecturer
- **Mô tả:** Xuất bảng điểm ra file Excel
- **Luồng chính:**
  1. Chọn ca thi môn học
  2. Hệ thống lấy danh sách điểm
  3. Tạo file Excel với thông tin:
     - STT, Mã SV, Họ tên, Lớp
     - Điểm, Thời gian làm bài
     - Trạng thái nộp bài
  4. Download file Excel

---

### **NHÓM 6: QUẢN LÝ GIẢNG VIÊN**

#### UC050: Quản lý thông tin giảng viên
- **Actor:** Admin
- **Mô tả:** CRUD thông tin giảng viên
- **Chức năng:**
  - Thêm giảng viên mới
  - Xem danh sách giảng viên
  - Cập nhật thông tin
  - Xóa giảng viên

#### UC051: Xem lịch coi thi
- **Actor:** Lecturer
- **Mô tả:** Giảng viên xem lịch coi thi được phân công
- **Luồng chính:**
  1. Giảng viên đăng nhập
  2. Hệ thống lấy danh sách ca thi được phân công
  3. Hiển thị thông tin:
     - Môn học
     - Thời gian
     - Phòng thi
     - Số lượng sinh viên

#### UC052: Giám sát phòng thi
- **Actor:** Lecturer
- **Mô tả:** Giám sát sinh viên trong phòng thi
- **Luồng chính:**
  1. Vào phòng thi (ca thi môn học)
  2. Xem danh sách sinh viên:
     - Trạng thái đăng nhập
     - Trạng thái làm bài
     - Thời gian còn lại
  3. Real-time update với SignalR

#### UC053: Nộp bài thay sinh viên
- **Actor:** Lecturer
- **Mô tả:** Nộp bài thay sinh viên (khi hết giờ, sự cố)
- **Luồng chính:**
  1. Chọn sinh viên
  2. Xác nhận nộp bài
  3. Hệ thống:
     - Lấy đáp án từ Redis
     - Cập nhật trạng thái
     - Chấm điểm
     - Ghi log (nộp bởi giảng viên)

---

### **NHÓM 7: THÔNG BÁO VÀ GIÁM SÁT**

#### UC060: Gửi thông báo nhắc nhở
- **Actor:** Admin
- **Mô tả:** Gửi thông báo real-time cho sinh viên
- **Luồng chính:**
  1. Soạn nội dung thông báo
  2. Chọn thời gian/ca thi
  3. Gửi thông báo qua SignalR
  4. Sinh viên nhận thông báo real-time

#### UC061: Giám sát trạng thái RabbitMQ
- **Actor:** Admin, ITManager
- **Mô tả:** Kiểm tra trạng thái các queue
- **Luồng chính:**
  1. Truy cập endpoint kiểm tra
  2. Hệ thống kiểm tra:
     - student_answer_saved_queue
     - exam_submission_queue
  3. Trả về trạng thái và số lượng message

#### UC062: Quản lý Redis/RabbitMQ
- **Actor:** ITManager
- **Mô tả:** Quản lý hạ tầng kỹ thuật
- **Chức năng:**
  - Kiểm tra kết nối Redis
  - Xóa cache
  - Reset queue

---

## 🔄 Quy trình nghiệp vụ chính

### **Quy trình 1: Tổ chức một kỳ thi**

```
1. Tạo Năm học (UC010)
   ↓
2. Tạo Học kỳ (UC011)
   ↓
3. Tạo Đợt thi (UC020)
   ↓
4. Tạo Chi tiết đợt thi (UC021)
   ↓
5. Tạo Ca thi (UC022)
   ↓
6. Tạo Ca thi môn học (UC023)
   ↓
7. Import Đề thi từ XML (UC030)
   ↓
8. Tạo Đề hoán vị (UC032)
   ↓
9. Gán đề thi cho ca thi môn học (UC025)
   ↓
10. Import Sinh viên (UC041)
   ↓
11. Phân công Giảng viên coi thi (UC024)
   ↓
12. Kích hoạt Ca thi (UC026)
```

### **Quy trình 2: Sinh viên tham gia thi**

```
1. Đăng nhập (UC040)
   ↓
2. Xem lịch thi
   ↓
3. Bắt đầu làm bài (UC043)
   ↓
4. Lưu đáp án (UC044) [Lặp lại nhiều lần]
   ↓
5. Nộp bài (UC045)
   ↓
6. Xem kết quả (UC046)
```

### **Quy trình 3: Giảng viên coi thi**

```
1. Đăng nhập giảng viên (UC002)
   ↓
2. Xem lịch coi thi (UC051)
   ↓
3. Vào phòng thi (UC052)
   ↓
4. Giám sát sinh viên
   ↓
5. Nộp bài thay (nếu cần) (UC053)
   ↓
6. Xuất bảng điểm (UC048)
```

---

## 📝 Entities (Các thực thể chính)

| Entity | Mô tả |
|--------|-------|
| **ApplicationUser** | Người dùng hệ thống (Admin, Staff) |
| **AcademicYear** | Năm học |
| **Semester** | Học kỳ |
| **Department** | Khoa/Phòng ban |
| **ExamBatch** | Đợt thi |
| **ExamBatchDetail** | Chi tiết đợt thi |
| **ExamSession** | Ca thi |
| **ExamSessionSubject** | Ca thi môn học |
| **ExamRoom** | Phòng thi |
| **Subject** | Môn học |
| **Chapter** | Chương (trong môn học) |
| **OriginalExamPaper** | Đề thi gốc |
| **OriginalExamPaperDetail** | Chi tiết câu hỏi đề gốc |
| **ShuffledExamPaper** | Đề thi hoán vị |
| **Answers** | Đáp án |
| **Student** | Sinh viên |
| **StudentExamSession** | Phiên thi của sinh viên |
| **Lecturer** | Giảng viên |

---

## 🔐 Phân quyền (Authorization Policies)

| Policy | Roles | Mô tả |
|--------|-------|-------|
| **AdminOnly** | Admin | Chỉ Admin |
| **StudentOnly** | Student | Chỉ Sinh viên |
| **LecturerOnly** | Lecturer | Chỉ Giảng viên |
| **AcademicAffairsOnly** | AcademicAffairs | Chỉ Phòng Đào tạo |
| **ExamManagerOnly** | ExamManager | Chỉ Quản lý thi |
| **ITManagerOnly** | ITManager | Chỉ Quản lý IT |
| **AcademicAffairsOrAdmin** | Admin, AcademicAffairs | Admin hoặc Phòng Đào tạo |
| **LecturerOrAdmin** | Admin, Lecturer | Admin hoặc Giảng viên |
| **StudentOrAdmin** | Admin, Student | Admin hoặc Sinh viên |
| **AcademicManagement** | Admin, AcademicAffairs, ExamManager | Quản lý học vụ |
| **StaffOnly** | Admin, Lecturer, AcademicAffairs, ExamManager, ITManager | Tất cả nhân viên |
| **AllUsers** | Tất cả | Tất cả người dùng |

---

## 🚀 Công nghệ và Kiến trúc

### **Backend Architecture**

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│                  (API Controllers)                       │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                    Business Layer                        │
│              (Services + AutoMapper)                     │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                   Data Access Layer                      │
│        (Repositories + EF Core + SQL Server)            │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                    │
│  - Redis (Caching)                                      │
│  - RabbitMQ (Message Queue)                             │
│  - SignalR (Real-time)                                  │
│  - JWT (Authentication)                                 │
│  - Serilog (Logging)                                    │
└─────────────────────────────────────────────────────────┘
```

### **Technology Stack**

- **Framework:** .NET 9.0
- **ORM:** Entity Framework Core
- **Database:** SQL Server
- **Cache:** Redis
- **Message Queue:** RabbitMQ
- **Real-time:** SignalR
- **Authentication:** JWT Bearer Token
- **Logging:** Serilog
- **Object Mapping:** AutoMapper
- **Excel Processing:** EPPlus
- **XML Processing:** System.Xml

---

## 📊 Database Schema (Tóm tắt)

### **Core Tables:**
- ApplicationUsers
- AcademicYears
- Semesters
- Departments
- ExamBatches
- ExamBatchDetails
- ExamSessions
- ExamSessionSubjects
- ExamRooms
- Subjects
- OriginalExamPapers
- OriginalExamPaperDetails
- ShuffledExamPapers
- Students
- StudentExamSessions
- Lecturers
- Answers

---

## 🎯 Tổng kết

Hệ thống HutechQuiz Backend cung cấp **61 Use Cases chính**, được nhóm thành **7 module lớn**:

1. **Authentication (3 UCs):** Xác thực và phân quyền
2. **Academic Structure (3 UCs):** Quản lý cấu trúc học vụ
3. **Exam Organization (7 UCs):** Quản lý đợt thi và ca thi
4. **Exam Papers (4 UCs):** Quản lý đề thi
5. **Students & Exams (9 UCs):** Quản lý sinh viên và thi
6. **Lecturers (4 UCs):** Quản lý giảng viên
7. **Monitoring (3 UCs):** Giám sát và thông báo

Hệ thống hỗ trợ **6 loại người dùng** với phân quyền chi tiết, sử dụng kiến trúc **phân tầng rõ ràng**, tích hợp **Redis, RabbitMQ, SignalR** để đảm bảo hiệu năng và real-time.

---

## 📅 Version History
- **v1.0** - 2025-01-11: Tài liệu phiên bản đầu tiên

---

**Prepared by:** Backend Development Team  
**Last Updated:** 2025-01-11

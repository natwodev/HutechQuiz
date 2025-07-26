# Quy trình tạo ca thi cho môn học (Phòng đào tạo)

## API Năm học (AcademicYear)

- **Lấy tất cả năm học**
  - `GET /api/AcademicYear`
  - Quyền: Admin
  - Mô tả: Lấy danh sách tất cả năm học

- **Lấy chi tiết năm học theo ID**
  - `GET /api/AcademicYear/{id}`
  - Quyền: Admin
  - Mô tả: Lấy thông tin chi tiết của một năm học

- **Tạo mới năm học**
  - `POST /api/AcademicYear`
  - Quyền: Admin
  - Mô tả: Tạo mới năm học, có thể kèm danh sách học kỳ

- **Cập nhật năm học**
  - `PUT /api/AcademicYear/{id}`
  - Quyền: Admin
  - Mô tả: Cập nhật thông tin năm học

- **Xóa năm học**
  - `DELETE /api/AcademicYear/{id}`
  - Quyền: Admin
  - Mô tả: Xóa năm học

## API Học kỳ (Semester)

- **Lấy tất cả học kỳ**
  - `GET /api/Semester`
  - Quyền: Admin
  - Mô tả: Lấy danh sách tất cả học kỳ

- **Lấy chi tiết học kỳ theo ID**
  - `GET /api/Semester/{id}`
  - Quyền: Admin
  - Mô tả: Lấy thông tin chi tiết của một học kỳ

- **Tạo mới học kỳ**
  - `POST /api/Semester`
  - Quyền: Admin
  - Mô tả: Tạo mới học kỳ cho một năm học đã có

- **Cập nhật học kỳ**
  - `PUT /api/Semester/{id}`
  - Quyền: Admin
  - Mô tả: Cập nhật thông tin học kỳ

- **Xóa học kỳ**
  - `DELETE /api/Semester/{id}`
  - Quyền: Admin
  - Mô tả: Xóa học kỳ

## API Đợt thi (ExamBatch)

- **Lấy tất cả đợt thi**
  - `GET /api/ExamBatch`
  - Quyền: Admin
  - Mô tả: Lấy danh sách tất cả đợt thi

- **Lấy chi tiết đợt thi theo ID**
  - `GET /api/ExamBatch/{id}`
  - Quyền: Admin
  - Mô tả: Lấy thông tin chi tiết của một đợt thi

- **Tạo mới đợt thi**
  - `POST /api/ExamBatch`
  - Quyền: Admin
  - Mô tả: Tạo mới đợt thi

- **Cập nhật đợt thi**
  - `PUT /api/ExamBatch/{id}`
  - Quyền: Admin
  - Mô tả: Cập nhật thông tin đợt thi

- **Xóa đợt thi**
  - `DELETE /api/ExamBatch/{id}`
  - Quyền: Admin
  - Mô tả: Xóa đợt thi

## API Chi tiết đợt thi (ExamBatchDetail)

- **Lấy tất cả chi tiết đợt thi**
  - `GET /api/ExamBatchDetail`
  - Quyền: Admin
  - Mô tả: Lấy danh sách tất cả chi tiết đợt thi

- **Lấy chi tiết đợt thi theo ID**
  - `GET /api/ExamBatchDetail/{id}`
  - Quyền: Admin
  - Mô tả: Lấy thông tin chi tiết của một chi tiết đợt thi

- **Tạo mới chi tiết đợt thi**
  - `POST /api/ExamBatchDetail`
  - Quyền: Admin
  - Mô tả: Tạo mới chi tiết đợt thi

- **Cập nhật chi tiết đợt thi**
  - `PUT /api/ExamBatchDetail/{id}`
  - Quyền: Admin
  - Mô tả: Cập nhật thông tin chi tiết đợt thi

- **Xóa chi tiết đợt thi**
  - `DELETE /api/ExamBatchDetail/{id}`
  - Quyền: Admin
  - Mô tả: Xóa chi tiết đợt thi

## API Ca thi (ExamSession)

- **Lấy tất cả ca thi**
  - `GET /api/ExamSession`
  - Quyền: Admin
  - Mô tả: Lấy danh sách tất cả ca thi

- **Lấy chi tiết ca thi theo ID**
  - `GET /api/ExamSession/{id}`
  - Quyền: Admin
  - Mô tả: Lấy thông tin chi tiết của một ca thi

- **Tạo mới ca thi**
  - `POST /api/ExamSession`
  - Quyền: Admin
  - Mô tả: Tạo mới ca thi

- **Cập nhật ca thi**
  - `PUT /api/ExamSession/{id}`
  - Quyền: Admin
  - Mô tả: Cập nhật thông tin ca thi

- **Xóa ca thi**
  - `DELETE /api/ExamSession/{id}`
  - Quyền: Admin
  - Mô tả: Xóa ca thi

## API Khoa tham gia ca thi (ExamSessionDepartment)

- **Lấy tất cả khoa tham gia ca thi**
  - `GET /api/ExamSessionDepartment`
  - Quyền: Admin
  - Mô tả: Lấy danh sách tất cả khoa tham gia các ca thi

- **Lấy chi tiết khoa tham gia ca thi theo ID**
  - `GET /api/ExamSessionDepartment/{id}`
  - Quyền: Admin
  - Mô tả: Lấy thông tin chi tiết của một khoa tham gia ca thi

- **Tạo mới khoa tham gia ca thi**
  - `POST /api/ExamSessionDepartment`
  - Quyền: Admin
  - Mô tả: Thêm mới khoa tham gia một ca thi

- **Cập nhật khoa tham gia ca thi**
  - `PUT /api/ExamSessionDepartment/{id}`
  - Quyền: Admin
  - Mô tả: Cập nhật thông tin khoa tham gia ca thi

- **Xóa khoa tham gia ca thi**
  - `DELETE /api/ExamSessionDepartment/{id}`
  - Quyền: Admin
  - Mô tả: Xóa khoa tham gia ca thi

## API Ca thi môn học (ExamSessionSubject)

- **Lấy tất cả ca thi môn học**
  - `GET /api/ExamSessionSubject`
  - Quyền: Admin
  - Mô tả: Lấy danh sách tất cả ca thi môn học

- **Lấy chi tiết ca thi môn học theo ID**
  - `GET /api/ExamSessionSubject/{id}`
  - Quyền: Admin
  - Mô tả: Lấy thông tin chi tiết của một ca thi môn học

- **Tạo mới ca thi môn học**
  - `POST /api/ExamSessionSubject`
  - Quyền: Admin
  - Mô tả: Tạo mới ca thi môn học

- **Cập nhật ca thi môn học**
  - `PUT /api/ExamSessionSubject/{id}`
  - Quyền: Admin
  - Mô tả: Cập nhật thông tin ca thi môn học

- **Xóa ca thi môn học**
  - `DELETE /api/ExamSessionSubject/{id}`
  - Quyền: Admin
  - Mô tả: Xóa ca thi môn học

- **Cập nhật đề thi gốc cho ca thi môn học**
  - `PATCH /api/ExamSessionSubject/{id}/original-exam-paper/{originalExamPaperId}`
  - Quyền: Admin
  - Mô tả: Cập nhật đề thi gốc cho một ca thi môn học

- **Lấy tất cả ca thi môn học với phòng thi**
  - `GET /api/ExamSessionSubject/with-rooms`
  - Quyền: Admin
  - Mô tả: Lấy danh sách ca thi môn học kèm thông tin phòng thi





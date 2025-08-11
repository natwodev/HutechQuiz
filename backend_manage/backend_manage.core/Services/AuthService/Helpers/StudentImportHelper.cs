using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using backend_manage.core.Repositories.Interfaces;
using backend_manage.shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace backend_manage.core.Services.AuthService.Helpers;

public class StudentImportHelper
{
    private readonly IRepository<Student> _studentRepository;
    private readonly IRepository<StudentExamSession> _studentExamSessionRepository;
    private readonly IRepository<ExamSessionSubject> _examSessionSubjectRepository;
    private readonly ILogger<StudentImportHelper> _logger;

    public StudentImportHelper(
        IRepository<Student> studentRepository,
        IRepository<StudentExamSession> studentExamSessionRepository,
        IRepository<ExamSessionSubject> examSessionSubjectRepository,
        ILogger<StudentImportHelper> logger)
    {
        _studentRepository = studentRepository;
        _studentExamSessionRepository = studentExamSessionRepository;
        _examSessionSubjectRepository = examSessionSubjectRepository;
        _logger = logger;
    }

    public async Task<StudentImportResultDto> ImportFromExcelStreamAsync(Stream stream, string examSessionSubjectCore, int examRoomId, string userId)
    {
        try
        {
            _logger.LogInformation("Bắt đầu import student từ stream. ExamSessionSubjectCore: {Core}, ExamRoomId: {RoomId}", examSessionSubjectCore, examRoomId);

            var students = new List<Student>();
            
            using (var package = new ExcelPackage(stream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;
                
                _logger.LogInformation("Tìm thấy {RowCount} rows trong file Excel", rowCount);
                
                for (int row = 2; row <= rowCount; row++) // Bỏ qua header
                {
                    var studentCode = worksheet.Cells[row, 2].Text;
                    var firstName = worksheet.Cells[row, 3].Text;
                    var lastName = worksheet.Cells[row, 4].Text;
                    
                    if (!string.IsNullOrWhiteSpace(studentCode))
                    {
                        students.Add(new Student
                        {
                            StudentCode = studentCode,
                            FirstName = firstName,
                            LastName = lastName,
                            CreatedBy = userId,
                            CreatedAt = DateTimeHelper.GetVietnamTime()
                        });
                    }
                }
            }

            _logger.LogInformation("Đã parse {StudentCount} sinh viên từ file Excel", students.Count);

            // Lấy danh sách StudentCode đã tồn tại
            var existingStudents = (await _studentRepository.GetAllAsync()).ToDictionary(s => s.StudentCode);
            
            // Lấy ExamSessionSubjectId từ examSessionSubjectCore
            var examSessionSubject = await _examSessionSubjectRepository.GetQueryable()
                .FirstOrDefaultAsync(x => x.ExamSessionSubjectCore == examSessionSubjectCore);
            
            if (examSessionSubject == null)
                throw new Exception($"Không tìm thấy ExamSessionSubject với core: {examSessionSubjectCore}");

            int? examRoomIdValue = examRoomId;
            int addedCount = 0;
            int studentExamSessionAdded = 0;

            foreach (var student in students)
            {
                Student dbStudent;
                if (!existingStudents.ContainsKey(student.StudentCode))
                {
                    dbStudent = await _studentRepository.AddAsync(student);
                    addedCount++;
                    _logger.LogDebug("Đã thêm sinh viên mới: {StudentCode}", student.StudentCode);
                }
                else
                {
                    // Nếu đã tồn tại thì tăng version, cập nhật UpdatedBy, UpdatedAt
                    dbStudent = existingStudents[student.StudentCode];
                    dbStudent.Version += 1;
                    dbStudent.UpdatedBy = userId;
                    dbStudent.UpdatedAt = DateTimeHelper.GetVietnamTime();
                    await _studentRepository.UpdateAsync(dbStudent);
                    _logger.LogDebug("Đã cập nhật sinh viên: {StudentCode}", student.StudentCode);
                }

                // Chỉ tạo mới nếu chưa có StudentExamSession trùng StudentId + ExamSessionSubjectId
                var exists = await _studentExamSessionRepository.GetQueryable()
                    .AnyAsync(x => x.StudentId == dbStudent.StudentId && x.ExamSessionSubjectId == examSessionSubject.ExamSessionSubjectId);
                
                if (!exists)
                {
                    var studentExamSession = new StudentExamSession
                    {
                        StudentId = dbStudent.StudentId,
                        StudentCode = student.StudentCode,
                        ExamSessionSubjectId = examSessionSubject.ExamSessionSubjectId,
                        ExamRoomId = examRoomIdValue,
                        CreatedBy = userId,
                        CreatedAt = DateTimeHelper.GetVietnamTime(),
                        StudentAnswersString = "",
                        IsCompleted = false,
                        Score = 0,
                        // Cache thời gian từ ExamSessionSubject để tránh join
                        ExamSessionStartTime = examSessionSubject.StartTime,
                        ExamSessionEndTime = examSessionSubject.EndTime
                    };
                    await _studentExamSessionRepository.AddAsync(studentExamSession);
                    studentExamSessionAdded++;
                    _logger.LogDebug("Đã tạo StudentExamSession cho sinh viên: {StudentCode} với thời gian {StartTime} - {EndTime}", 
                        student.StudentCode, examSessionSubject.StartTime, examSessionSubject.EndTime);
                }
            }

            var result = new StudentImportResultDto 
            { 
                StudentsAdded = addedCount, 
                StudentExamSessionsAdded = studentExamSessionAdded 
            };

            _logger.LogInformation("Hoàn thành import. Đã thêm {StudentsAdded} sinh viên, {SessionsAdded} phiên thi", 
                result.StudentsAdded, result.StudentExamSessionsAdded);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi import student từ stream");
            throw;
        }
    }
} 
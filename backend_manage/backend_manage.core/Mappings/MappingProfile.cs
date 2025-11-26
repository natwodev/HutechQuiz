using AutoMapper;
using backend_manage.core.Entities;
using backend_manage.core.Messages;
using backend_manage.shared.DTOs;
using System.Text.Json;

namespace backend_manage.core.Mappings;
public class MappingProfile : Profile
{
    public MappingProfile()
    {
       
        // Department
        CreateMap<Department, DepartmentDto>();
        CreateMap<DepartmentCreateDto, Department>();
        CreateMap<DepartmentUpdateDto, Department>();
        

        // AcademicYear
        CreateMap<AcademicYear, AcademicYearDto>()
            .ForMember(dest => dest.YearName, opt => opt.MapFrom(src => src.AcademicYearName))
            .ForMember(dest => dest.Semesters, opt => opt.MapFrom(src => src.Semesters));
        CreateMap<AcademicYearCreateDto, AcademicYear>()
            .ForMember(dest => dest.AcademicYearName, opt => opt.MapFrom(src => src.YearName));
        CreateMap<AcademicYearUpdateDto, AcademicYear>()
            .ForMember(dest => dest.AcademicYearName, opt => opt.MapFrom(src => src.YearName));
    
        // Semester
        CreateMap<Semester, SemesterDto>()
            .ForMember(dest => dest.AcademicYearId, opt => opt.MapFrom(src => src.AcademicYearId))
            .ForMember(dest => dest.AcademicYearName, opt => opt.MapFrom(src => src.AcademicYear.AcademicYearName));
        CreateMap<SemesterCreateDto, Semester>();
        CreateMap<SemesterUpdateDto, Semester>();

        // ExamBatch
        CreateMap<ExamBatch, ExamBatchDto>()
            .ForMember(dest => dest.BatchName, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Semester.SemesterName))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ExamBatchDetails, opt => opt.NullSubstitute(new List<ExamBatchDetailDto>()))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        CreateMap<ExamBatchCreateDto, ExamBatch>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.BatchName))
            .ForMember(dest => dest.SemesterId, opt => opt.MapFrom(src => src.SemesterId))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        CreateMap<ExamBatchUpdateDto, ExamBatch>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.BatchName))
            .ForMember(dest => dest.SemesterId, opt => opt.MapFrom(src => src.SemesterId))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        

        // Chapter
        CreateMap<Chapter, ChapterDto>();
        CreateMap<ChapterCreateDto, Chapter>();
        CreateMap<ChapterUpdateDto, Chapter>();

        // Student
        CreateMap<Student, StudentDto>();
        CreateMap<StudentCreateDto, Student>();
        CreateMap<StudentUpdateDto, Student>();

        // Subject
        CreateMap<Subject, SubjectDto>();
        CreateMap<SubjectCreateDto, Subject>();
        CreateMap<SubjectUpdateDto, Subject>();

        CreateMap<ExamBatchDetail, ExamBatchDetailDto>()
            .ForMember(dest => dest.ExamBatchName, opt => opt.MapFrom(src => src.ExamBatch != null ? src.ExamBatch.Name : null));
        CreateMap<ExamBatchDetailCreateDto, ExamBatchDetail>();
        CreateMap<ExamBatchDetailUpdateDto, ExamBatchDetail>();

        CreateMap<ExamSession, ExamSessionDto>()
            .ForMember(dest => dest.ExamBatchDetailId, opt => opt.MapFrom(src => src.ExamBatchDetailId))
            .ForMember(dest => dest.ExamBatchDetailName, opt => opt.MapFrom(src => src.ExamBatchDetail != null ? src.ExamBatchDetail.Name : null));
        CreateMap<ExamSessionCreateDto, ExamSession>();
        CreateMap<ExamSessionUpdateDto, ExamSession>();

        CreateMap<ExamSessionSubject, ExamSessionSubjectDto>()
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectName : null))
            .ForMember(dest => dest.OriginalExamPaperTitle, opt => opt.MapFrom(src => src.OriginalExamPaper != null ? src.OriginalExamPaper.Title : null))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
            .ForMember(dest => dest.ExamSessionSubjectCore, opt => opt.MapFrom(src => src.ExamSessionSubjectCore))
            .ForMember(dest => dest.ExamRoomId, opt => opt.MapFrom(src => src.ExamRoomId))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.ExamRoom != null ? src.ExamRoom.RoomName : null));
        CreateMap<ExamSessionSubjectCreateDto, ExamSessionSubject>();
        CreateMap<ExamSessionSubjectUpdateDto, ExamSessionSubject>();

        // OriginalExamPaper & Detail
        CreateMap<OriginalExamPaper, OriginalExamPaperDto>()
            .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.OriginalExamPaperDetails.Where(d => d.ParentQuestionId == null)));
        CreateMap<OriginalExamPaperDetail, OriginalExamPaperDetailDto>()
            .ForMember(dest => dest.ChildQuestions, opt => opt.MapFrom(src => src.ChildQuestions))
            .ForMember(dest => dest.ParentQuestion, opt => opt.MapFrom(src => src.ParentQuestion))
            .ForMember(dest => dest.Answers, opt => opt.MapFrom(src => src.Answers));

        // Mapping cho Answers entity
        CreateMap<Answers, AnswerDto>();
        CreateMap<AnswerCreateDto, Answers>();
        CreateMap<AnswerUpdateDto, Answers>();
        
        // Mapping cho OriginalExamDto
        CreateMap<OriginalExamPaper, OriginalExamDto>()
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject.SubjectName));


        CreateMap<StudentExamSession, StudentExamSessionDto>()
            .ForMember(dest => dest.ExamSessionSubjectId, opt => opt.MapFrom(src => src.ExamSessionSubjectId))
            .ForMember(dest => dest.StudentExamSessionId, opt => opt.MapFrom(src => src.StudentExamSessionId))
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.ExamSessionSubject.Subject.SubjectName))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.ExamSessionSubject.ExamRoom.RoomName))
            .ForMember(dest => dest.ExamSessionName, opt => opt.MapFrom(src => src.ExamSessionSubject.ExamSession.Name))
            .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.ExamSessionSubject.Duration))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.ExamSessionSubject.StartTime))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.ExamSessionSubject.EndTime))
            .ForMember(dest => dest.ExtraMinutes, opt => opt.MapFrom(src => src.ExtraMinutes))
            .ForMember(dest => dest.RemainingMinutes, opt => opt.MapFrom(src => src.RemainingMinutes));

        // Mapping cho StudentExamSessionCacheDto
        CreateMap<StudentExamSession, StudentExamSessionCacheDto>()
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.ExamSessionSubject.Subject.SubjectName))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.ExamSessionSubject.ExamRoom.RoomName))
            .ForMember(dest => dest.ExamSessionName, opt => opt.MapFrom(src => src.ExamSessionSubject.ExamSession.Name))
            .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.ExamSessionSubject.Duration))
            .ForMember(dest => dest.ExamSessionStartTime, opt => opt.MapFrom(src => src.ExamSessionStartTime))
            .ForMember(dest => dest.ExamSessionEndTime, opt => opt.MapFrom(src => src.ExamSessionEndTime))
            .ForMember(dest => dest.ExamRoomId, opt => opt.MapFrom(src => src.ExamSessionSubject.ExamRoomId))
            .ForMember(dest => dest.RemainingMinutes, opt => opt.MapFrom(src => src.RemainingMinutes));

        // Mapping ngược từ StudentExamSessionCacheDto về StudentExamSession
        CreateMap<StudentExamSessionCacheDto, StudentExamSession>();

        CreateMap<StudentExamSession, StudentExamRoomStatusDto>()
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student.StudentCode))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Student.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.Student.LastName))
            .ForMember(dest => dest.IsLogin, opt => opt.MapFrom(src => src.Student.IsLogin))
            .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.IsCompleted))
            .ForMember(dest => dest.ExamSessionSubjectId, opt => opt.MapFrom(src => src.ExamSessionSubjectId))
            .ForMember(dest => dest.StudentExamSessionId, opt => opt.MapFrom(src => src.StudentExamSessionId))
            .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.ExamSessionSubject.Duration))
            .ForMember(dest => dest.ExtraMinutes, opt => opt.MapFrom(src => src.ExtraMinutes))
            .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.Score))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime));

        CreateMap<StudentExamSession, ExamSessionSubjectRoomDto>()
            .ForMember(dest => dest.ExamSessionSubjectId, opt => opt.MapFrom(src => src.ExamSessionSubjectId))
            .ForMember(dest => dest.ExamRoomId, opt => opt.MapFrom(src => src.ExamSessionSubject.ExamRoomId))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.ExamSessionSubject.ExamRoom.RoomName))
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.ExamSessionSubject.Subject.SubjectName));

        // Mapping cho ExamSessionSubject → ExamSessionSubjectRoomDto
        CreateMap<ExamSessionSubject, ExamSessionSubjectRoomDto>()
            .ForMember(dest => dest.ExamSessionSubjectId, opt => opt.MapFrom(src => src.ExamSessionSubjectId))
            .ForMember(dest => dest.ExamRoomId, opt => opt.MapFrom(src => src.ExamRoomId))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.ExamRoom.RoomName))
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject.SubjectName));

        // Mapping cho ExamSessionSubject → ExamSessionSubjectDto
        CreateMap<ExamSessionSubject, ExamSessionSubjectDto>()
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject.SubjectName))
            .ForMember(dest => dest.OriginalExamPaperTitle, opt => opt.MapFrom(src => src.OriginalExamPaper.Title))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.ExamRoom.RoomName))
            .ForMember(dest => dest.MonitorName, opt => opt.MapFrom(src => src.Monitor != null ? $"{src.Monitor.LastName} {src.Monitor.FirstName}" : null));

        // Mapping cho ExamSessionSubject → SubjectExamRoomStatusDto
        CreateMap<ExamSessionSubject, SubjectExamRoomStatusDto>()
            .ForMember(dest => dest.SubjectId, opt => opt.MapFrom(src => src.SubjectId))
            .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.Subject.SubjectCore))
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject.SubjectName))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.ExamRoom.RoomName))
            .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.Duration))
            .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.IsCompleted))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.ExamSessionStartTime, opt => opt.MapFrom(src => src.StartTime))
            .ForMember(dest => dest.ExamSessionEndTime, opt => opt.MapFrom(src => src.EndTime))
            .ForMember(dest => dest.ExamSessionName, opt => opt.MapFrom(src => src.ExamSession.Name))
            .ForMember(dest => dest.LecturerCode, opt => opt.MapFrom(src => src.Monitor.LecturerCode));

        // Lecturer
        CreateMap<LecturerCreateDto, Lecturer>();
        CreateMap<Lecturer, LecturerDto>()
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.DepartmentName : null));
        CreateMap<StudentExamSessionCacheDto, StudentExamSessionDto>();

        // ExamRoom
        CreateMap<ExamRoom, ExamRoomDto>();

        // Mapping cho ExamSubmissionMessage
        CreateMap<ExamSubmissionDto, ExamSubmissionMessage>()
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode))
            .ForMember(dest => dest.ShuffledExamPaperId, opt => opt.MapFrom(src => src.ShuffledExamPaperId))
            .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.Score ?? 0))
            .ForMember(dest => dest.CorrectAnswers, opt => opt.MapFrom(src => src.CorrectAnswers ?? 0))
            .ForMember(dest => dest.TotalQuestions, opt => opt.MapFrom(src => src.TotalQuestions ?? 0))
            .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.Score.HasValue))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
            .ForMember(dest => dest.StudentAnswersString, opt => opt.MapFrom(src => src.StudentAnswersString));

        // Mapping cho StudentAnswerSavedMessage
        CreateMap<(string StudentCode, int StudentExamSessionId, int key, int value, string NewAnswersString), StudentAnswerSavedMessage>()
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode))
            .ForMember(dest => dest.StudentExamSessionId, opt => opt.MapFrom(src => src.StudentExamSessionId))
            .ForMember(dest => dest.key, opt => opt.MapFrom(src => src.key))
            .ForMember(dest => dest.value, opt => opt.MapFrom(src => src.value))
            .ForMember(dest => dest.NewAnswersString, opt => opt.MapFrom(src => src.NewAnswersString));

        // Mapping cho ShuffledExamPaper
        CreateMap<ShuffledExamPaper, ShuffledExamPaperDto>()
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectName : null))
            .ForMember(dest => dest.SubjectCode, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectCore : null))
            .ForMember(dest => dest.QuestionStructures, opt => opt.MapFrom(src => ParseQuestionStructure(src.QuestionStructure)));
    }


    private static List<QuestionStructureDto> ParseQuestionStructure(string? questionStructureJson)
    {
        if (string.IsNullOrEmpty(questionStructureJson))
            return new List<QuestionStructureDto>();

        try
        {
            // Dùng Newtonsoft.Json để deserialize vì khi lưu cũng dùng JsonConvert (Newtonsoft.Json)
            var questionStructures = Newtonsoft.Json.JsonConvert.DeserializeObject<List<QuestionStructureDto>>(questionStructureJson);
            return questionStructures ?? new List<QuestionStructureDto>();
        }
        catch (Exception ex)
        {
            // Log lỗi để debug
            System.Diagnostics.Debug.WriteLine($"Lỗi khi parse QuestionStructure: {ex.Message}");
            return new List<QuestionStructureDto>();
        }
    }
}

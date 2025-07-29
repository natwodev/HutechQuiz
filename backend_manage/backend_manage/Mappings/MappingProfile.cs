using AutoMapper;
using backend_manage.DTOs;
using backend_manage.Entities;
using backend_manage.Messages;


namespace backend_manage.Mappings;
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

        CreateMap<ExamSessionDepartment, ExamSessionDepartmentDto>()
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.DepartmentName : null))
            .ForMember(dest => dest.ExamSessionName, opt => opt.MapFrom(src => src.ExamSession != null ? src.ExamSession.Name : null));
        CreateMap<ExamSessionDepartmentCreateDto, ExamSessionDepartment>();
        CreateMap<ExamSessionDepartmentUpdateDto, ExamSessionDepartment>();

        CreateMap<ExamSessionSubject, ExamSessionSubjectDto>()
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.Subject != null ? src.Subject.SubjectName : null))
            .ForMember(dest => dest.OriginalExamPaperTitle, opt => opt.MapFrom(src => src.OriginalExamPaper != null ? src.OriginalExamPaper.Title : null))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
            .ForMember(dest => dest.ExamSessionSubjectCore, opt => opt.MapFrom(src => src.ExamSessionSubjectCore));
        CreateMap<ExamSessionSubjectCreateDto, ExamSessionSubject>();
        CreateMap<ExamSessionSubjectUpdateDto, ExamSessionSubject>();

        // OriginalExamPaper & Detail
        CreateMap<OriginalExamPaper, OriginalExamPaperDto>()
            .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.OriginalExamPaperDetails.Where(d => d.ParentQuestionId == null)));
        CreateMap<OriginalExamPaperDetail, OriginalExamPaperDetailDto>()
            .ForMember(dest => dest.ChildQuestions, opt => opt.MapFrom(src => src.ChildQuestions));

        // ShuffledExamPaper & Detail
        CreateMap<ShuffledExamPaper, ShuffledExamPaperDto>()
            .ForMember(dest => dest.Details, opt => opt.MapFrom(src => src.ShuffledExamPaperDetails));
        CreateMap<ShuffledExamPaperDetail, ShuffledExamPaperDetailDto>()
            .ForMember(dest => dest.QuestionContent, opt => opt.MapFrom(src => src.OriginalExamPaperDetail.QuestionContent))
            .ForMember(dest => dest.Answer1, opt => opt.Ignore())
            .ForMember(dest => dest.Answer2, opt => opt.Ignore())
            .ForMember(dest => dest.Answer3, opt => opt.Ignore())
            .ForMember(dest => dest.Answer4, opt => opt.Ignore())
            .ForMember(dest => dest.ChildQuestions, opt => opt.MapFrom(src => src.ChildQuestions))
            .AfterMap((src, dest) => {
                var original = src.OriginalExamPaperDetail;
                if (original == null || string.IsNullOrEmpty(src.AnswerOrder))
                {
                    dest.Answer1 = original?.Answer1;
                    dest.Answer2 = original?.Answer2;
                    dest.Answer3 = original?.Answer3;
                    dest.Answer4 = original?.Answer4;
                }
                else
                {
                    var answers = new[] { original.Answer1, original.Answer2, original.Answer3, original.Answer4 };
                    var order = src.AnswerOrder.ToCharArray().Select(c => int.Parse(c.ToString()) - 1).ToArray();
                    var shuffledAnswers = order.Select(i => answers.ElementAtOrDefault(i)).ToArray();
                    dest.Answer1 = shuffledAnswers.ElementAtOrDefault(0);
                    dest.Answer2 = shuffledAnswers.ElementAtOrDefault(1);
                    dest.Answer3 = shuffledAnswers.ElementAtOrDefault(2);
                    dest.Answer4 = shuffledAnswers.ElementAtOrDefault(3);
                }
            });

        CreateMap<StudentExamSession, StudentExamSessionDto>()
            .ForMember(dest => dest.ExamSessionSubjectId, opt => opt.MapFrom(src => src.ExamSessionSubjectId))
            .ForMember(dest => dest.StudentExamSessionId, opt => opt.MapFrom(src => src.StudentExamSessionId))
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.ExamSessionSubject.Subject.SubjectName))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.ExamRoom.RoomName))
            .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.ExamSessionSubject.Duration))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.ExamSessionSubject.StartTime))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.ExamSessionSubject.EndTime))
            .ForMember(dest => dest.ExtraMinutes, opt => opt.MapFrom(src => src.ExtraMinutes));

        CreateMap<StudentExamSession, StudentExamRoomStatusDto>()
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.Student.StudentCode))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Student.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.Student.LastName))
            .ForMember(dest => dest.IsLogin, opt => opt.MapFrom(src => src.Student.IsLogin))
            .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.IsCompleted))
            .ForMember(dest => dest.ExamSessionSubjectId, opt => opt.MapFrom(src => src.ExamSessionSubjectId))
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.ExamSessionSubject.Subject.SubjectName))
            .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.ExamSessionSubject.Duration))
            .ForMember(dest => dest.ExtraMinutes, opt => opt.MapFrom(src => src.ExtraMinutes))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime));

        CreateMap<StudentExamSession, ExamSessionSubjectRoomDto>()
            .ForMember(dest => dest.ExamSessionSubjectId, opt => opt.MapFrom(src => src.ExamSessionSubjectId))
            .ForMember(dest => dest.ExamRoomId, opt => opt.MapFrom(src => src.ExamRoomId))
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.ExamRoom.RoomName))
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.ExamSessionSubject.Subject.SubjectName));

        CreateMap<ExamRoomLecturerAssignment, ExamRoomLecturerAssignmentDto>()
            .ForMember(dest => dest.RoomName, opt => opt.MapFrom(src => src.ExamRoom.RoomName))
            .ForMember(dest => dest.SubjectName, opt => opt.MapFrom(src => src.ExamSessionSubject.Subject.SubjectName))
            .ForMember(dest => dest.LecturerName, opt => opt.MapFrom(src => src.Lecturer.LastName + " " + src.Lecturer.FirstName));

        // Lecturer
        CreateMap<LecturerCreateDto, Lecturer>();
        CreateMap<Lecturer, LecturerDto>()
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.DepartmentName : null));

        // Mapping cho ExamSubmissionMessage
        CreateMap<backend_manage.DTOs.ExamSubmissionDto, ExamSubmissionMessage>()
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode))
            .ForMember(dest => dest.ShuffledExamPaperId, opt => opt.MapFrom(src => src.ShuffledExamPaperId))
            .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.Score ?? 0))
            .ForMember(dest => dest.CorrectAnswers, opt => opt.MapFrom(src => src.CorrectAnswers ?? 0))
            .ForMember(dest => dest.TotalQuestions, opt => opt.MapFrom(src => src.TotalQuestions ?? 0))
            .ForMember(dest => dest.IsCompleted, opt => opt.MapFrom(src => src.Score.HasValue))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
            .ForMember(dest => dest.StudentAnswersString, opt => opt.MapFrom(src => src.StudentAnswersString));

        // Mapping cho StudentAnswerSavedMessage
        CreateMap<(string StudentCode, int ShuffledExamPaperId, int Index, string Answer, string NewAnswersString), backend_manage.Messages.RabbitMQ.StudentAnswerSavedMessage>()
            .ForMember(dest => dest.StudentCode, opt => opt.MapFrom(src => src.StudentCode))
            .ForMember(dest => dest.ShuffledExamPaperId, opt => opt.MapFrom(src => src.ShuffledExamPaperId))
            .ForMember(dest => dest.Index, opt => opt.MapFrom(src => src.Index))
            .ForMember(dest => dest.Answer, opt => opt.MapFrom(src => src.Answer))
            .ForMember(dest => dest.NewAnswersString, opt => opt.MapFrom(src => src.NewAnswersString));
    }
}

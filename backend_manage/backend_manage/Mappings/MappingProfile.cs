using AutoMapper;
using backend_manage.DTOs;
using backend_manage.Entities;

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
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime));
        CreateMap<ExamSessionSubjectCreateDto, ExamSessionSubject>();
        CreateMap<ExamSessionSubjectUpdateDto, ExamSessionSubject>();

    }
}

using backend_manage.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace backend_manage.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<OriginalExamPaper> OriginalExamPapers { get; set; }
        public DbSet<ShuffledExamPaper> ShuffledExamPapers { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<ExamBatch> ExamBatches { get; set; }
        public DbSet<ExamSession> ExamSessions { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<ShuffledExamPaperDetail> ShuffledExamPaperDetails { get; set; }
        public DbSet<OriginalExamPaperDetail> OriginalExamPaperDetails { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<StudentExamSession> StudentExamSessions { get; set; }
        public DbSet<StudentAnswer> StudentAnswers { get; set; }
        public DbSet<ExamSessionDepartment> ExamSessionDepartments { get; set; }
        public DbSet<ExamSessionSubject> ExamSessionSubjects { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<AcademicYear> AcademicYears { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<ExamPaperRequest> ExamPaperRequests { get; set; }
        public DbSet<QuizFile> QuizFile { get; set; }
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<ExamRoomLecturerAssignment> ExamRoomLecturerAssignments { get; set; }
        
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            

            // ExamSessionSubject → Subject
            builder.Entity<ExamSessionSubject>()
                .HasOne(e => e.Subject)
                .WithMany(s => s.ExamSessionSubjects) // nếu có navigation ngược
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Restrict); // tránh cascade path

            // ExamSessionSubject → ExamSessionDepartment
            builder.Entity<ExamSessionSubject>()
                .HasOne(e => e.ExamSessionDepartment)
                .WithMany(d => d.ExamSessionSubjects) // nếu có navigation ngược
                .HasForeignKey(e => e.ExamSessionDepartmentId)
                .OnDelete(DeleteBehavior.Cascade); // bạn chọn 1 cascade là đủ

            // Chapter → Subject
            builder.Entity<Chapter>()
                .HasOne(c => c.Subject)
                .WithMany(s => s.Chapters)
                .HasForeignKey(c => c.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Chapter → Chapter (self-ref)
            builder.Entity<Chapter>()
                .HasOne(c => c.ParentChapter)
                .WithMany(c => c.ChildChapters)
                .HasForeignKey(c => c.ParentChapterId)
                .OnDelete(DeleteBehavior.Restrict);

            // Question → Chapter
            builder.Entity<Question>()
                .HasOne(q => q.Chapter)
                .WithMany(c => c.Questions)
                .HasForeignKey(q => q.ChapterId)
                .OnDelete(DeleteBehavior.Cascade);

            // Answer → Question
            builder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            
           // ShuffledExamPaper → Subject
            builder.Entity<ShuffledExamPaper>()
                .HasOne(e => e.Subject)
                .WithMany(s => s.ShuffledExamPapers) // nếu có navigation ngược
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Restrict); // ✅ Tránh multiple cascade path

            // ShuffledExamPaper → ExamSessionSubject
            builder.Entity<ShuffledExamPaper>()
                .HasOne(e => e.ExamSessionSubject)
                .WithMany(es => es.ShuffledExamPapers) // nếu có navigation ngược
                .HasForeignKey(e => e.ExamSessionSubjectId)
                .OnDelete(DeleteBehavior.Cascade); // chỉ 1 quan hệ được cascade
            
            
            // ExamPaperDetail → Question
            builder.Entity<ShuffledExamPaperDetail>()
                .HasOne(e => e.Question)
                .WithMany(q => q.ShuffledExamPaperDetails) // Có navigation ngược
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

           // ExamPaperDetail → Chapter
           builder.Entity<ShuffledExamPaperDetail>()
               .HasOne(e => e.Chapter)
               .WithMany(c => c.ShuffledExamPaperDetails) // nên thêm navigation ngược
               .HasForeignKey(e => e.ChapterId)
               .OnDelete(DeleteBehavior.Restrict);

           // ExamPaperDetail → ShuffledExamPaper
            builder.Entity<ShuffledExamPaperDetail>()
                .HasOne(e => e.ShuffledExamPaper)
                .WithMany(e => e.ShuffledExamPaperDetails) // nếu có navigation
                .HasForeignKey(e => e.ShuffledExamPaperId)
                .OnDelete(DeleteBehavior.Cascade); // chỉ cascade 1 quan hệ

            // StudentExamSession → Student
            builder.Entity<StudentExamSession>()
                .HasOne(s => s.Student)
                .WithMany(st => st.StudentExamSessions) // nếu có navigation ngược
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict); // Hoặc NoAction

           // StudentExamSession → ExamSessionSubject
            builder.Entity<StudentExamSession>()
                .HasOne(s => s.ExamSessionSubject)
                .WithMany(es => es.StudentExamSessions) // nếu có navigation ngược
                .HasForeignKey(s => s.ExamSessionSubjectId)
                .OnDelete(DeleteBehavior.Restrict); // Tránh cascade path

           // StudentExamSession → ShuffledExamPaper
            builder.Entity<StudentExamSession>()
                .HasOne(s => s.ShuffledExamPaper)
                .WithMany(ep => ep.StudentExamSessions) // nếu có navigation ngược
                .HasForeignKey(s => s.ShuffledExamPaperId)
                .OnDelete(DeleteBehavior.Cascade); // giữ lại 1 cascade là đủ

            // StudentAnswer → StudentExamSession
            builder.Entity<StudentAnswer>()
                .HasOne(sa => sa.StudentExamSession)
                .WithMany(ses => ses.StudentAnswers) // nếu có navigation ngược
                .HasForeignKey(sa => sa.StudentExamSessionId)
                .OnDelete(DeleteBehavior.Restrict); // KHÔNG được cascade để tránh lỗi

           // StudentAnswer → ExamPaperDetail
            builder.Entity<StudentAnswer>()
                .HasOne(sa => sa.ShuffledExamPaperDetail)
                .WithMany(epd => epd.StudentAnswers) // nếu có navigation ngược
                .HasForeignKey(sa => sa.ExamPaperDetailId)
                .OnDelete(DeleteBehavior.Cascade); // ✅ chỉ nên để 1 cái cascade

           // StudentAnswer → Answer (SelectedAnswer)
            builder.Entity<StudentAnswer>()
                .HasOne(sa => sa.SelectedAnswer)
                .WithMany() // nếu không có navigation ngược
                .HasForeignKey(sa => sa.SelectedAnswerId)
                .OnDelete(DeleteBehavior.Restrict); // hoặc .NoAction để tránh rủi ro

            // ExamPaperRequest → ShuffledExamPaper
            builder.Entity<ExamPaperRequest>()
                .HasOne(er => er.ShuffledExamPaper)
                .WithMany() // nếu không có navigation ngược
                .HasForeignKey(er => er.ShuffledExamPaperId)
                .OnDelete(DeleteBehavior.Restrict); // Tránh cascade để bảo vệ dữ liệu

            // OriginalExamPaper → Subject
            builder.Entity<OriginalExamPaper>()
                .HasOne(e => e.Subject)
                .WithMany(s => s.OriginalExamPapers) // nếu có navigation ngược
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // ExamSessionSubject → OriginalExamPaper
            builder.Entity<ExamSessionSubject>()
                .HasOne(es => es.OriginalExamPaper)
                .WithMany(o => o.ExamSessionSubjects)
                .HasForeignKey(es => es.OriginalExamPaperId)
                .OnDelete(DeleteBehavior.Restrict);

            // ShuffledExamPaper → OriginalExamPaper
            builder.Entity<ShuffledExamPaper>()
                .HasOne(e => e.OriginalExamPaper)
                .WithMany(o => o.ShuffledExamPapers)
                .HasForeignKey(e => e.OriginalExamPaperId)
                .OnDelete(DeleteBehavior.Cascade);

            // OriginalExamPaperDetail → OriginalExamPaper
            builder.Entity<OriginalExamPaperDetail>()
                .HasOne(e => e.OriginalExamPaper)
                .WithMany(o => o.OriginalExamPaperDetails)
                .HasForeignKey(e => e.OriginalExamPaperId)
                .OnDelete(DeleteBehavior.Cascade);

            // OriginalExamPaperDetail → Chapter
            builder.Entity<OriginalExamPaperDetail>()
                .HasOne(e => e.Chapter)
                .WithMany(c => c.OriginalExamPaperDetails)
                .HasForeignKey(e => e.ChapterId)
                .OnDelete(DeleteBehavior.Restrict);

            // OriginalExamPaperDetail → Question
            builder.Entity<OriginalExamPaperDetail>()
                .HasOne(e => e.Question)
                .WithMany(q => q.OriginalExamPaperDetails)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // ExamRoomLecturerAssignment → ExamRoom
            builder.Entity<ExamRoomLecturerAssignment>()
                .HasOne(e => e.ExamRoom)
                .WithMany()
                .HasForeignKey(e => e.ExamRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // ExamRoomLecturerAssignment → Lecturer
            builder.Entity<ExamRoomLecturerAssignment>()
                .HasOne(e => e.Lecturer)
                .WithMany(l => l.ExamRoomLecturerAssignments) // nếu có navigation ngược
                .HasForeignKey(e => e.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

        }

    }
}
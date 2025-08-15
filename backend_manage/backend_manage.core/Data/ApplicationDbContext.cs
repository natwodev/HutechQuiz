using backend_manage.core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace backend_manage.core.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<OriginalExamPaper> OriginalExamPapers { get; set; }
        public DbSet<ShuffledExamPaper> ShuffledExamPapers { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<ExamBatch> ExamBatches { get; set; }
        public DbSet<ExamBatchDetail> ExamBatchDetails { get; set; }
        public DbSet<ExamSession> ExamSessions { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<ShuffledExamPaperDetail> ShuffledExamPaperDetails { get; set; }
        public DbSet<OriginalExamPaperDetail> OriginalExamPaperDetails { get; set; }
        public DbSet<Chapter> Chapters { get; set; }
        public DbSet<StudentExamSession> StudentExamSessions { get; set; }
        public DbSet<ExamSessionSubject> ExamSessionSubjects { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<AcademicYear> AcademicYears { get; set; }
        public DbSet<Semester> Semesters { get; set; }
        public DbSet<Lecturer> Lecturers { get; set; }
        public DbSet<ExamRoom> ExamRooms { get; set; }
        
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // ✅ Giữ lại các bảng Identity cần thiết cho authentication
            // builder.Ignore<IdentityUserClaim<string>>(); // Cần thiết cho user claims
            // builder.Ignore<IdentityUserLogin<string>>(); // Cần thiết cho external login
            // builder.Ignore<IdentityUserToken<string>>(); // Cần thiết cho user tokens
            // builder.Ignore<IdentityRoleClaim<string>>(); // Cần thiết cho role claims

            // ExamSessionSubject → Subject
            builder.Entity<ExamSessionSubject>()
                .HasOne(e => e.Subject)
                .WithMany(s => s.ExamSessionSubjects) // nếu có navigation ngược
                .HasForeignKey(e => e.SubjectId)
                .OnDelete(DeleteBehavior.Restrict); // tránh cascade path

            // ExamSessionSubject → ExamSession
            builder.Entity<ExamSessionSubject>()
                .HasOne(e => e.ExamSession)
                .WithMany(es => es.ExamSessionSubjects)
                .HasForeignKey(e => e.ExamSessionId)
                .OnDelete(DeleteBehavior.Cascade);

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

            // ExamPaperDetail → ShuffledExamPaper
            builder.Entity<ShuffledExamPaperDetail>()
                .HasOne(e => e.ShuffledExamPaper)
                .WithMany(e => e.ShuffledExamPaperDetails) // nếu có navigation
                .HasForeignKey(e => e.ShuffledExamPaperId)
                .OnDelete(DeleteBehavior.Cascade); // chỉ cascade 1 quan hệ

            // ShuffledExamPaperDetail → OriginalExamPaperDetail
            builder.Entity<ShuffledExamPaperDetail>()
                .HasOne(e => e.OriginalExamPaperDetail)
                .WithMany(o => o.ShuffledExamPaperDetails)
                .HasForeignKey(e => e.OriginalExamPaperDetailId)
                .OnDelete(DeleteBehavior.Restrict); // KHÔNG cascade để tránh multiple cascade paths

            // ShuffledExamPaperDetail → ParentQuestion (self-ref)
            builder.Entity<ShuffledExamPaperDetail>()
                .HasOne(e => e.ParentQuestion)
                .WithMany(p => p.ChildQuestions)
                .HasForeignKey(e => e.ParentQuestionId)
                .OnDelete(DeleteBehavior.Restrict); // KHÔNG cascade để tránh multiple cascade paths

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

            // Thêm index cho StudentExamSession để tối ưu hóa tìm kiếm theo StudentExamSessionId
            builder.Entity<StudentExamSession>()
                .HasIndex(s => s.StudentExamSessionId)
                .HasDatabaseName("IX_StudentExamSession_StudentExamSessionId");

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

            // ExamSessionSubject → Lecturer (Monitor)
            builder.Entity<ExamSessionSubject>()
                .HasOne(es => es.Monitor)
                .WithMany()
                .HasForeignKey(es => es.MonitorId)
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

            // OriginalExamPaperDetail → ParentQuestion (self-ref)
            builder.Entity<OriginalExamPaperDetail>()
                .HasOne(e => e.ParentQuestion)
                .WithMany(p => p.ChildQuestions)
                .HasForeignKey(e => e.ParentQuestionId)
                .OnDelete(DeleteBehavior.Restrict);

        }

    }
}
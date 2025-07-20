using backend_manage.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace backend_manage.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Xóa user/role Identity
            var users = userManager.Users.ToList();
            foreach (var user in users)
            {
                await userManager.DeleteAsync(user);
            }
            var roles = roleManager.Roles.ToList();
            foreach (var role in roles)
            {
                await roleManager.DeleteAsync(role);
            }

            string[] rolesToSeed = { "Admin", "Student", "Lecturer" };
            foreach (var role in rolesToSeed)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            string adminEmail = "admin";
            string adminPassword = "admin";
            string fullname = "admin";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var user = new ApplicationUser
                {
                    FullName = fullname,
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                    Console.WriteLine("✅ Admin user đã được tạo.");
                }
                else
                {
                    Console.WriteLine("❌ Lỗi tạo user admin: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                Console.WriteLine("ℹ️ Admin user đã tồn tại.");
            }
            string customerEmail = "2280602015";
            string customerPassword = "2280602015"; 
            string customerFullName = "Nguyễn Huỳnh Nam";

            var customerUser = await userManager.FindByEmailAsync(customerEmail);
            if (customerUser == null)
            {
                var user = new ApplicationUser
                {
                    FullName = customerFullName,
                    UserName = customerEmail,
                    Email = customerEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, customerPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Student");
                    Console.WriteLine("✅ Customer user đã được tạo.");
                }
                else
                {
                    Console.WriteLine("❌ Lỗi tạo user Customer: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                Console.WriteLine("ℹ️ Customer user đã tồn tại.");
            }

            // Seed 2 khoa nếu chưa có
            if (!context.Departments.Any())
            {
                var now = DateTime.UtcNow;
                var departments = new[]
                {
                    new Department
                    {
                        DepartmentId = "CNTT",
                        DepartmentName = "Khoa Công nghệ thông tin",
                        CreatedBy = "seed",
                        CreatedAt = now,
                        IsDeleted = false,
                        UpdatedAt = now,
                        UpdatedBy = "seed",
                        Version = 1
                    },
                    new Department
                    {
                        DepartmentId = "KINHTE",
                        DepartmentName = "Khoa Kinh tế",
                        CreatedBy = "seed",
                        CreatedAt = now,
                        IsDeleted = false,
                        UpdatedAt = now,
                        UpdatedBy = "seed",
                        Version = 1
                    }
                };
                context.Departments.AddRange(departments);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Đã seed 2 khoa CNTT và Kinh tế.");
            }

            // Seed 2 môn học nếu chưa có
            if (!context.Subjects.Any())
            {
                var now = DateTime.UtcNow;
                var subjects = new[]
                {
                    new Subject
                    {
                        SubjectCore = "CNTT001",
                        SubjectName = "Lập trình C#",
                        DepartmentId = "CNTT",
                        CreatedBy = "seed",
                        CreatedAt = now,
                        IsDeleted = false,
                        UpdatedAt = now,
                        UpdatedBy = "seed",
                        Version = 1
                    },
                    new Subject
                    {
                        SubjectCore = "KT001",
                        SubjectName = "Kinh tế vi mô",
                        DepartmentId = "KINHTE",
                        CreatedBy = "seed",
                        CreatedAt = now,
                        IsDeleted = false,
                        UpdatedAt = now,
                        UpdatedBy = "seed",
                        Version = 1
                    }
                };
                context.Subjects.AddRange(subjects);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Đã seed 2 môn học: Lập trình C# và Kinh tế vi mô.");
            }

            // Seed AcademicYear nếu chưa có
            if (!context.AcademicYears.Any())
            {
                var now = DateTime.UtcNow;
                var academicYear = new AcademicYear
                {
                    AcademicYearName = "2025-2026",
                    CreatedBy = "seed",
                    CreatedAt = now,
                    IsDeleted = false,
                    UpdatedAt = now,
                    UpdatedBy = "seed",
                    Version = 1
                };
                context.AcademicYears.Add(academicYear);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Đã seed năm học 2025-2026.");

                // Seed 1 học kỳ cho năm học này
                var semester = new Semester
                {
                    SemesterName = "Học kỳ 1A",
                    AcademicYearId = academicYear.AcademicYearId,
                    CreatedBy = "seed",
                    CreatedAt = now,
                    IsDeleted = false,
                    UpdatedAt = now,
                    UpdatedBy = "seed",
                    Version = 1
                };
                context.Semesters.Add(semester);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Đã seed học kỳ 1A cho năm học 2025-2026.");

                // Seed 1 ExamBatch cho học kỳ này
                var examBatch = new ExamBatch
                {
                    Name = "Đợt thi cuối kỳ 1A",
                    Description = "Đợt thi cuối kỳ cho học kỳ 1A năm học 2025-2026",
                    StartDate = now.Date,
                    EndDate = now.Date.AddDays(7),
                    IsActive = true,
                    IsCompleted = false,
                    SemesterId = semester.SemesterId,
                    CreatedBy = "seed",
                    CreatedAt = now,
                    IsDeleted = false,
                    UpdatedAt = now,
                    UpdatedBy = "seed",
                    Version = 1
                };
                context.ExamBatches.Add(examBatch);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Đã seed đợt thi cuối kỳ 1A cho học kỳ 1A.");

                // Seed 1 ExamBatchDetail cho ExamBatch này
                var examBatchDetail = new ExamBatchDetail
                {
                    ExamBatchId = examBatch.ExamBatchId,
                    Name = "Lần 1",
                    CreatedBy = "seed",
                    CreatedAt = now,
                    IsDeleted = false,
                    UpdatedAt = now,
                    UpdatedBy = "seed",
                    Version = 1
                };
                context.ExamBatchDetails.Add(examBatchDetail);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Đã seed ExamBatchDetail 'Lần 1' cho đợt thi cuối kỳ 1A.");

                // Seed 1 ExamSession cho ExamBatchDetail này
                var examSession = new ExamSession
                {
                    ExamBatchDetailId = examBatchDetail.ExamBatchDetailId,
                    Name = "Ca thi 1",
                    StartTime = now.Date.AddHours(8),
                    EndTime = now.Date.AddHours(10),
                    IsActive = true,
                    IsCompleted = false,
                    CreatedBy = "seed",
                    CreatedAt = now,
                    IsDeleted = false,
                    UpdatedAt = now,
                    UpdatedBy = "seed",
                    Version = 1
                };
                context.ExamSessions.Add(examSession);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Đã seed ExamSession 'Ca thi 1' cho ExamBatchDetail 'Lần 1'.");

                // Seed 2 ExamSessionDepartment cho ExamSession này (CNTT, KINHTE)
                var examSessionDepartments = new[]
                {
                    new ExamSessionDepartment
                    {
                        ExamSessionId = examSession.ExamSessionId,
                        DepartmentId = "CNTT",
                        CreatedBy = "seed",
                        CreatedAt = now,
                        IsDeleted = false,
                        UpdatedAt = now,
                        UpdatedBy = "seed",
                        Version = 1
                    },
                    new ExamSessionDepartment
                    {
                        ExamSessionId = examSession.ExamSessionId,
                        DepartmentId = "KINHTE",
                        CreatedBy = "seed",
                        CreatedAt = now,
                        IsDeleted = false,
                        UpdatedAt = now,
                        UpdatedBy = "seed",
                        Version = 1
                    }
                };
                context.ExamSessionDepartments.AddRange(examSessionDepartments);
                await context.SaveChangesAsync();
                Console.WriteLine("✅ Đã seed ExamSessionDepartment cho 2 khoa CNTT và KINHTE.");

                // Seed ExamSessionSubject cho mỗi ExamSessionDepartment với môn học tương ứng
                var cnttSessionDept = examSessionDepartments.FirstOrDefault(x => x.DepartmentId == "CNTT");
                var kinhteSessionDept = examSessionDepartments.FirstOrDefault(x => x.DepartmentId == "KINHTE");

                var cnttSubject = context.Subjects.FirstOrDefault(x => x.SubjectCore == "CNTT001");
                var kinhteSubject = context.Subjects.FirstOrDefault(x => x.SubjectCore == "KT001");

                if (cnttSessionDept != null && cnttSubject != null && kinhteSessionDept != null && kinhteSubject != null)
                {
                    var examSessionSubjects = new[]
                    {
                        new ExamSessionSubject
                        {
                            ExamSessionDepartmentId = cnttSessionDept.ExamSessionDepartmentId,
                            SubjectId = cnttSubject.SubjectId,
                            ExamSessionSubjectCore = $"{cnttSubject.SubjectCore}-{now:yyyyMMddHHmmss}",
                            CreatedBy = "seed",
                            CreatedAt = now,
                            IsDeleted = false,
                            UpdatedAt = now,
                            UpdatedBy = "seed",
                            Version = 1
                        },
                        new ExamSessionSubject
                        {
                            ExamSessionDepartmentId = kinhteSessionDept.ExamSessionDepartmentId,
                            SubjectId = kinhteSubject.SubjectId,
                            ExamSessionSubjectCore = $"{kinhteSubject.SubjectCore}-{now:yyyyMMddHHmmss}",
                            CreatedBy = "seed",
                            CreatedAt = now,
                            IsDeleted = false,
                            UpdatedAt = now,
                            UpdatedBy = "seed",
                            Version = 1
                        }
                    };
                    context.ExamSessionSubjects.AddRange(examSessionSubjects);
                    await context.SaveChangesAsync();
                    Console.WriteLine("✅ Đã seed ExamSessionSubject cho 2 khoa với 2 môn học tương ứng.");
                }
            }
            else
            {
                Console.WriteLine("ℹ️ Đã có dữ liệu năm học.");
            }
        }
    }
}
using backend_manage.core.Entities;
using backend_manage.core.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace backend_manage.core.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
/*
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
*/
            string[] rolesToSeed = { "Admin", "Student", "Lecturer", "AcademicAffairs", "ExamManager", "ITManager" };
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
                    logger.LogInformation("✅ Admin user đã được tạo.");
                }
                else
                {
                    logger.LogError("❌ Lỗi tạo user admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("ℹ️ Admin user đã tồn tại.");
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
                    logger.LogInformation("✅ Customer user đã được tạo.");
                }
                else
                {
                    logger.LogError("❌ Lỗi tạo user Customer: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("ℹ️ Customer user đã tồn tại.");
            }

            // Seed các user cho các role còn thiếu
            var rolesToSeedUser = new[]
            {
                new { Role = "Lecturer", Username = "lecturer", FullName = "Giảng viên" },
                new { Role = "AcademicAffairs", Username = "academicaffairs", FullName = "Phòng đào tạo" },
                new { Role = "ExamManager", Username = "exammanager", FullName = "Quản lý đợt thi" },
                new { Role = "ITManager", Username = "itmanager", FullName = "Quản trị CNTT" }
            };
            foreach (var item in rolesToSeedUser)
            {
                var existUser = await userManager.FindByEmailAsync(item.Username);
                if (existUser == null)
                {
                    var user = new ApplicationUser
                    {
                        FullName = item.FullName,
                        UserName = item.Username,
                        Email = item.Username,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(user, item.Username);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, item.Role);
                        logger.LogInformation("✅ User {Username} ({Role}) đã được tạo.", item.Username, item.Role);
                    }
                    else
                    {
                        logger.LogError("❌ Lỗi tạo user {Username}: {Errors}", item.Username, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    logger.LogInformation("ℹ️ User {Username} đã tồn tại.", item.Username);
                }
            }

            // Seed 2 khoa nếu chưa có
            if (!context.Departments.Any())
            {
                var now = DateTimeHelper.GetVietnamTime();
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
                logger.LogInformation("✅ Đã seed 2 khoa CNTT và Kinh tế.");
            }

            // Seed 2 môn học nếu chưa có
            if (!context.Subjects.Any())
            {
                var now = DateTimeHelper.GetVietnamTime();
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
                    },
                    new Subject
                    {
                        SubjectCore = "CNTT002",
                        SubjectName = "Cơ sở dữ liệu",
                        DepartmentId = "CNTT",
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
                logger.LogInformation("✅ Đã seed 3 môn học: Lập trình C#, Kinh tế vi mô và Cơ sở dữ liệu.");
            }

            // Seed AcademicYear nếu chưa có
            if (!context.AcademicYears.Any())
            {
                var now = DateTimeHelper.GetVietnamTime();
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
                logger.LogInformation("✅ Đã seed năm học 2025-2026.");

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
                logger.LogInformation("✅ Đã seed học kỳ 1A cho năm học 2025-2026.");

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
                logger.LogInformation("✅ Đã seed đợt thi cuối kỳ 1A cho học kỳ 1A.");

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
                logger.LogInformation("✅ Đã seed ExamBatchDetail 'Lần 1' cho đợt thi cuối kỳ 1A.");

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
                logger.LogInformation("✅ Đã seed ExamSession 'Ca thi 1' cho ExamBatchDetail 'Lần 1'.");

                // Seed ExamSessionSubject trực tiếp với ExamSession
                var cnttSubject = context.Subjects.FirstOrDefault(x => x.SubjectCore == "CNTT001");
                var cnttSubject2 = context.Subjects.FirstOrDefault(x => x.SubjectCore == "CNTT002");
                var kinhteSubject = context.Subjects.FirstOrDefault(x => x.SubjectCore == "KT001");

                if (cnttSubject != null && cnttSubject2 != null && kinhteSubject != null)
                {
                    var examSessionSubjects = new[]
                    {
                        new ExamSessionSubject
                        {
                            ExamSessionId = examSession.ExamSessionId,
                            SubjectId = cnttSubject.SubjectId,
                            ExamSessionSubjectCore = $"{cnttSubject.SubjectCore}-{now:yyyyMMddHHmmss}",
                            Duration = 120,
                            StartTime  = now, 
                            EndTime = now.AddMinutes(120),
                            CreatedBy = "seed",
                            CreatedAt = now,
                            IsDeleted = false,
                            UpdatedAt = now,
                            UpdatedBy = "seed",
                            Version = 1
                        },
                        new ExamSessionSubject
                        {
                            ExamSessionId = examSession.ExamSessionId,
                            SubjectId = kinhteSubject.SubjectId,
                            ExamSessionSubjectCore = $"{kinhteSubject.SubjectCore}-{now:yyyyMMddHHmmss}",
                            Duration = 120,
                            StartTime  = now, 
                            EndTime = now.AddMinutes(120),
                            CreatedBy = "seed",
                            CreatedAt = now,
                            IsDeleted = false,
                            UpdatedAt = now,
                            UpdatedBy = "seed",
                            Version = 1
                        },
                        new ExamSessionSubject
                        {
                            ExamSessionId = examSession.ExamSessionId,
                            SubjectId = cnttSubject2.SubjectId,
                            ExamSessionSubjectCore = $"{cnttSubject2.SubjectCore}-{now:yyyyMMddHHmmss}",
                            Duration = 90,
                            StartTime = now.AddHours(2), // Thi sau ca đầu 2 tiếng
                            EndTime = now.AddHours(2).AddMinutes(90),
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
                    logger.LogInformation("✅ Đã seed 3 ExamSessionSubject: Lập trình C# (120 phút), Kinh tế vi mô (120 phút), Cơ sở dữ liệu (90 phút).");
                }
            }
            else
            {
                logger.LogInformation("ℹ️ Đã có dữ liệu năm học.");
            }

            // Seed 4 phòng thi nếu chưa có
            if (!context.ExamRooms.Any())
            {
                var now = DateTimeHelper.GetVietnamTime();
                var rooms = new[]
                {
                    new ExamRoom { RoomName = "A101", CreatedBy = "seed", CreatedAt = now, UpdatedBy = "seed", UpdatedAt = now, IsDeleted = false, Version = 1 },
                    new ExamRoom { RoomName = "A102", CreatedBy = "seed", CreatedAt = now, UpdatedBy = "seed", UpdatedAt = now, IsDeleted = false, Version = 1 },
                    new ExamRoom { RoomName = "B201", CreatedBy = "seed", CreatedAt = now, UpdatedBy = "seed", UpdatedAt = now, IsDeleted = false, Version = 1 },
                    new ExamRoom { RoomName = "B202", CreatedBy = "seed", CreatedAt = now, UpdatedBy = "seed", UpdatedAt = now, IsDeleted = false, Version = 1 }
                };
                context.ExamRooms.AddRange(rooms);
                await context.SaveChangesAsync();
                logger.LogInformation("✅ Đã seed 4 phòng thi: A101, A102, B201, B202.");
            }

            // Seed giảng viên nếu chưa có
            if (!context.Lecturers.Any())
            {
                var now = DateTimeHelper.GetVietnamTime();
                var lecturers = new[]
                {
                    new Lecturer
                    {
                        LecturerCode = "GV001",
                        FirstName = "Nguyễn",
                        LastName = "Văn A",
                        Gender = true,
                        DateOfBirth = new DateTime(1980, 1, 15),
                        Email = "gv001@hutech.edu.vn",
                        PhoneNumber = "0123456789",
                        DepartmentId = "CNTT",
                        CreatedBy = "seed",
                        CreatedAt = now,
                        UpdatedBy = "seed",
                        UpdatedAt = now,
                        IsDeleted = false,
                        Version = 1
                    },
                    new Lecturer
                    {
                        LecturerCode = "GV002",
                        FirstName = "Trần",
                        LastName = "Thị B",
                        Gender = false,
                        DateOfBirth = new DateTime(1985, 5, 20),
                        Email = "gv002@hutech.edu.vn",
                        PhoneNumber = "0987654321",
                        DepartmentId = "KINHTE",
                        CreatedBy = "seed",
                        CreatedAt = now,
                        UpdatedBy = "seed",
                        UpdatedAt = now,
                        IsDeleted = false,
                        Version = 1
                    }
                };
                context.Lecturers.AddRange(lecturers);
                await context.SaveChangesAsync();
                logger.LogInformation("✅ Đã seed 2 giảng viên: GV001 (CNTT) và GV002 (KINHTE).");
            }
        }
    }
}
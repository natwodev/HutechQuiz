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
        }
    }
}
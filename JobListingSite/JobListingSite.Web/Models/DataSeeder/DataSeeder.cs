using JobListingSite.Data.Entities;
using JobListingSite.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace JobListingSite.Web.Models.DataSeeder
{
    public class DataSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            var dbContext = serviceProvider.GetRequiredService<JobListingDbContext>();

            string[] roleNames = { "Registered", "HR", "Admin", "Company" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed Admin User
            string adminEmail = "admin@gmail.com";
            string adminPassword = "Admin@123";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    NormalizedEmail = adminEmail.ToUpper(),
                    NormalizedUserName = adminEmail.ToUpper(),
                    EmailConfirmed = true,
                    Name = "Admin",
                    IsCompany = false,
                    IsApproved = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");

                    var adminProfile = new Profile
                    {
                        Bio = "Admin user",
                        UserId = adminUser.Id
                    };
                    dbContext.Profiles.Add(adminProfile);
                    await dbContext.SaveChangesAsync();
                }
            }

            string hrEmail = "hr@gmail.com";
            string hrPassword = "HR@123";

            if (await userManager.FindByEmailAsync(hrEmail) == null)
            {
                var hrUser = new User
                {
                    UserName = hrEmail,
                    Email = hrEmail,
                    NormalizedEmail = hrEmail.ToUpper(),
                    NormalizedUserName = hrEmail.ToUpper(),
                    EmailConfirmed = true,
                    Name = "HR User",
                    IsCompany = false,
                    IsApproved = true
                };

                var hrResult = await userManager.CreateAsync(hrUser, hrPassword);

                if (hrResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(hrUser, "HR");

                    var hrProfile = new Profile
                    {
                        Bio = "HR Representative",
                        UserId = hrUser.Id
                    };
                    dbContext.Profiles.Add(hrProfile);
                    await dbContext.SaveChangesAsync();
                }
            }
            if (!dbContext.Categories.Any())
            {
                var defaultCategories = new List<Category>
                    {
                        new Category { Name = "Technology" },
                        new Category { Name = "Business" },
                        new Category { Name = "Marketing" }
                    };
                dbContext.Categories.AddRange(defaultCategories);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
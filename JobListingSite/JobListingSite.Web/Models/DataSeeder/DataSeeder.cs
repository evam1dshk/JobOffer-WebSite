using JobListingSite.Data.Entities;
using JobListingSite.Web.Data;
using Microsoft.AspNetCore.Identity;

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
}

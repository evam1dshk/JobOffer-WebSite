using JobListingSite.Data.Entities;
using JobListingSite.Web.Data;
using Microsoft.AspNetCore.Identity;


namespace JobListingSite.Web.Models.DataSedeer
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

            // ✅ Seed Admin User
            string adminEmail = "admin@gmail.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Name = "Admin",
                    IsCompany = false,
                    IsApproved = true
                };

                if ((await userManager.CreateAsync(adminUser, "Admin@123")).Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");

                    dbContext.Profiles.Add(new Profile
                    {
                        Bio = "Admin user",
                        UserId = adminUser.Id
                    });
                    await dbContext.SaveChangesAsync();
                }
            }

            // ✅ Seed HR User
            string hrEmail = "hr@gmail.com";
            if (await userManager.FindByEmailAsync(hrEmail) == null)
            {
                var hrUser = new User
                {
                    UserName = hrEmail,
                    Email = hrEmail,
                    EmailConfirmed = true,
                    Name = "HR",
                    IsCompany = false,
                    IsApproved = true
                };

                if ((await userManager.CreateAsync(hrUser, "HR@123")).Succeeded)
                {
                    await userManager.AddToRoleAsync(hrUser, "HR");

                    dbContext.Profiles.Add(new Profile
                    {
                        Bio = "HR user",
                        UserId = hrUser.Id
                    });
                    await dbContext.SaveChangesAsync();
                }
            }

            // ✅ Create Categories if missing
            if (!dbContext.Categories.Any())
            {
                dbContext.Categories.AddRange(new[]
                {
                new Category { Name = "Technology" },
                new Category { Name = "Business" },
                new Category { Name = "Marketing" }
            });
                await dbContext.SaveChangesAsync();
            }

            // ✅ Seed Company User and Job Offers
            string companyEmail = "company@gmail.com";
            if (await userManager.FindByEmailAsync(companyEmail) == null)
            {
                var companyUser = new User
                {
                    UserName = companyEmail,
                    Email = companyEmail,
                    EmailConfirmed = true,
                    Name = "GreenTech Inc.",
                    IsCompany = true,
                    IsApproved = true
                };

                if ((await userManager.CreateAsync(companyUser, "Company@123")).Succeeded)
                {
                    await userManager.AddToRoleAsync(companyUser, "Company");

                    var profile = new CompanyProfile
                    {
                        UserId = companyUser.Id,
                        CompanyName = "GreenTech Inc.",
                        Industry = "Technology",
                        Phone = "123-456-7890",
                        ContactEmail = companyEmail,
                        Description = "We build eco-friendly solutions.",
                        LogoUrl = "/img/sample-logo.png", 
                        BannerImageUrl = "/img/sample-banner.jpg" 
                    };

                    dbContext.CompanyProfiles.Add(profile);
                    await dbContext.SaveChangesAsync();

                    var techCategory = dbContext.Categories.FirstOrDefault(c => c.Name == "Technology");

                    if (techCategory != null)
                    {
                        dbContext.Offers.AddRange(new[]
                        {
                        new Offer
                        {
                            Title = "Frontend Developer",
                            Description = "We are looking for a creative Frontend Developer.",
                            Salary = 50000,
                            CompanyId = companyUser.Id,
                            CategoryId = techCategory.CategoryId,
                            CreatedAt = DateTime.UtcNow
                        },
                        new Offer
                        {
                            Title = "Backend Developer",
                            Description = "Join our team to work on scalable backend systems.",
                            Salary = 55000,
                            CompanyId = companyUser.Id,
                            CategoryId = techCategory.CategoryId,
                            CreatedAt = DateTime.UtcNow.AddDays(-2)
                        }
                    });

                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            // ✅ Seed Registered User
            string registeredEmail = "testuser@gmail.com";
            if (await userManager.FindByEmailAsync(registeredEmail) == null)
            {
                var registeredUser = new User
                {
                    UserName = registeredEmail,
                    Email = registeredEmail,
                    EmailConfirmed = true,
                    Name = "Test User",
                    IsCompany = false,
                    IsApproved = true
                };

                if ((await userManager.CreateAsync(registeredUser, "Testuser@123")).Succeeded)
                {
                    await userManager.AddToRoleAsync(registeredUser, "Registered");

                    dbContext.Profiles.Add(new Profile
                    {
                        Bio = "Registered user",
                        UserId = registeredUser.Id
                    });

                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }
}

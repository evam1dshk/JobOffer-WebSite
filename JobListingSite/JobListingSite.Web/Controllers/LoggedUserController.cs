using JobListingSite.Data.Entities;
using JobListingSite.Web.Data;
using JobListingSite.Web.Models.LoggedUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobListingSite.Web.Controllers
{
    [Authorize(Roles = "Registered")]
    public class LoggedUserController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly JobListingDbContext _context;
        private readonly IWebHostEnvironment _env;

        public LoggedUserController(UserManager<User> userManager, JobListingDbContext context, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> ManageProfile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            // ✅ TEMP DEBUG: Print user and roles
            var roles = await _userManager.GetRolesAsync(user);
            Console.WriteLine($"[DEBUG] User: {user.Email}, Roles: {string.Join(", ", roles)}");

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            var model = new LoggedUserProfileViewModel
            {
                Name = user.Name,
                Phone = user.PhoneNumber,
                Bio = profile?.Bio,
                Location = profile?.Location,
                LinkedInUrl = profile?.LinkedInUrl,
                PortfolioUrl = profile?.PortfolioUrl,
                ResumeFilePath = profile?.ResumeFilePath,
                ProfileImageUrl = profile?.ProfileImageUrl,
                SelectedAvatar = profile?.SelectedAvatar
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageProfile(LoggedUserProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null)
            {
                profile = new Profile { UserId = user.Id };
                _context.Profiles.Add(profile);
            }

            user.Name = model.Name;
            user.PhoneNumber = model.Phone;

            profile.Bio = model.Bio;
            profile.Location = model.Location;
            profile.LinkedInUrl = model.LinkedInUrl;
            profile.PortfolioUrl = model.PortfolioUrl;
            profile.SelectedAvatar = model.SelectedAvatar;

            // ✅ Upload profile image
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath, "uploads", "profile-pictures");
                Directory.CreateDirectory(uploads);
                var fileName = Guid.NewGuid() + Path.GetExtension(model.ProfileImage.FileName);
                var filePath = Path.Combine(uploads, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(stream);
                }
                profile.ProfileImageUrl = "/uploads/profile-pictures/" + fileName;
            }

            // ✅ Upload resume
            if (model.Resume != null && model.Resume.Length > 0)
            {
                var uploads = Path.Combine(_env.WebRootPath, "uploads", "resumes");
                Directory.CreateDirectory(uploads);
                var fileName = Guid.NewGuid() + Path.GetExtension(model.Resume.FileName);
                var filePath = Path.Combine(uploads, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Resume.CopyToAsync(stream);
                }
                profile.ResumeFilePath = "/uploads/resumes/" + fileName;
            }

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("ManageProfile");
        }

        
        public async Task<IActionResult> MyApplications()
        {
            var userId = _userManager.GetUserId(User); // ✅ Declare userId

            var applications = await _context.JobApplications
                .Where(a => a.UserId == userId)
                .Include(a => a.Offer)
                    .ThenInclude(o => o.Company)
                        .ThenInclude(c => c.CompanyProfile)
                .Include(a => a.User)
                    .ThenInclude(u => u.Profile)
                .ToListAsync();

            var viewModel = applications.Select(a => new MyApplicationsViewModel
            {
                ApplicationId = a.Id,
                JobTitle = a.Offer.Title,
                CompanyName = a.Offer.Company.CompanyProfile.CompanyName,
                AppliedOn = a.AppliedOn,
                Status = a.Status,
                ProfileImageUrl = a.User.Profile?.ProfileImageUrl,
                SelectedAvatar = a.User.Profile?.SelectedAvatar
            }).ToList();

            return View(viewModel);
        }

        [AllowAnonymous]
        public async Task<IActionResult> PublicProfile(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null || user.Profile == null) return NotFound();

            var vm = new PublicProfileViewModel
            {
                Name = user.Name,
                Email = user.Email,
                Location = user.Profile.Location,
                Phone = user.Profile.Phone,
                Bio = user.Profile.Bio,
                LinkedInUrl = user.Profile.LinkedInUrl,
                PortfolioUrl = user.Profile.PortfolioUrl,
                ResumeFilePath = user.Profile.ResumeFilePath,
                ProfileImageUrl = user.Profile.ProfileImageUrl ?? user.Profile.SelectedAvatar
            };

            return View(vm);
        }
    }
}

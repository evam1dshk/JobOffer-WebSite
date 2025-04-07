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

        public LoggedUserController(UserManager<User> userManager, JobListingDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> ManageProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

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
                ProfileImageUrl = profile?.ProfileImageUrl
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

            // ✅ Update user info
            user.Name = model.Name;
            user.PhoneNumber = model.Phone;

            // ✅ Update profile info
            profile.Bio = model.Bio;
            profile.Location = model.Location;
            profile.LinkedInUrl = model.LinkedInUrl;
            profile.PortfolioUrl = model.PortfolioUrl;

            // ✅ Handle resume upload
            if (model.Resume != null && model.Resume.Length > 0)
            {
                var resumeFolder = Path.Combine("wwwroot", "uploads", "resumes");
                Directory.CreateDirectory(resumeFolder);

                var resumeFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.Resume.FileName);
                var resumePath = Path.Combine(resumeFolder, resumeFileName);

                using (var stream = new FileStream(resumePath, FileMode.Create))
                {
                    await model.Resume.CopyToAsync(stream);
                }

                profile.ResumeFilePath = "/uploads/resumes/" + resumeFileName;
            }

            // ✅ Handle profile image upload
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var imageFolder = Path.Combine("wwwroot", "uploads", "profile-images");
                Directory.CreateDirectory(imageFolder);

                var imageFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfileImage.FileName);
                var imagePath = Path.Combine(imageFolder, imageFileName);

                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(stream);
                }

                profile.ProfileImageUrl = "/uploads/profile-images/" + imageFileName;
            }

            // If avatar is selected (from list), use it
            if (!string.IsNullOrEmpty(model.SelectedAvatar))
            {
                profile.ProfileImageUrl = model.SelectedAvatar;
            }

            // If uploading a new file overrides selection
            if (model.ProfileImage != null && model.ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot", "uploads", "avatars");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfileImage.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfileImage.CopyToAsync(stream);
                }

                profile.ProfileImageUrl = "/uploads/avatars/" + uniqueFileName;
            }


            // ✅ Save all
            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("ManageProfile");
        }
    }
}

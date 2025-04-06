using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobListingSite.Web.Data;
using JobListingSite.Data.Entities;
using JobListingSite.Web.Models.Company;

namespace JobListingSite.Web.Controllers
{
    [Authorize(Roles = "Company")]
    public class CompanyController : Controller
    {
        private readonly JobListingDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _hostEnvironment;

        public CompanyController(JobListingDbContext context, UserManager<User> userManager, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _hostEnvironment = hostEnvironment;
        }

        [HttpGet]
        [AllowAnonymous] 
        public IActionResult Profile(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var company = _context.Users
                .Include(u => u.CompanyProfile)
                .FirstOrDefault(u => u.Id == id && u.IsCompany && u.CompanyProfile != null);

            if (company == null)
                return NotFound();

            return View(company);
        }

        [HttpGet]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> ManageProfile()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users
                .Include(u => u.CompanyProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.CompanyProfile == null)
                return NotFound();

            var vm = new CompanyProfileFormViewModel
            {
                CompanyName = user.CompanyProfile.CompanyName,
                Description = user.CompanyProfile.Description,
                Industry = user.CompanyProfile.Industry,
                ContactEmail = user.CompanyProfile.ContactEmail ?? user.Email,
                Phone = user.CompanyProfile.Phone,
                CompanyWebsite = user.CompanyProfile.CompanyWebsite,
                FoundedYear = user.CompanyProfile.FoundedYear,
                Location = user.CompanyProfile.Location,
                LinkedIn = user.CompanyProfile.LinkedIn,
                Twitter = user.CompanyProfile.Twitter,
                NumberOfEmployees = user.CompanyProfile.NumberOfEmployees,
                LogoUrl = user.CompanyProfile.LogoUrl,
                BannerUrl = user.CompanyProfile.BannerImageUrl
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> ManageProfile(CompanyProfileFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = _userManager.GetUserId(User);
            var user = await _context.Users
                .Include(u => u.CompanyProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.CompanyProfile == null)
                return NotFound();

            string uploads = Path.Combine(_hostEnvironment.WebRootPath, "uploads");

            if (model.Logo != null)
            {
                string logoFile = Guid.NewGuid() + Path.GetExtension(model.Logo.FileName);
                string logoPath = Path.Combine(uploads, "logos", logoFile);

                using (var stream = new FileStream(logoPath, FileMode.Create))
                {
                    await model.Logo.CopyToAsync(stream);
                }

                user.CompanyProfile.LogoUrl = "/uploads/logos/" + logoFile;
            }

            if (model.Banner != null)
            {
                string bannerFile = Guid.NewGuid() + Path.GetExtension(model.Banner.FileName);
                string bannerPath = Path.Combine(uploads, "banners", bannerFile);

                using (var stream = new FileStream(bannerPath, FileMode.Create))
                {
                    await model.Banner.CopyToAsync(stream);
                }

                user.CompanyProfile.BannerImageUrl = "/uploads/banners/" + bannerFile;
            }

            user.CompanyProfile.CompanyName = model.CompanyName;
            user.CompanyProfile.Description = model.Description;
            user.CompanyProfile.Industry = model.Industry;
            user.CompanyProfile.ContactEmail = model.ContactEmail;
            user.CompanyProfile.Phone = model.Phone;
            user.CompanyProfile.CompanyWebsite = model.CompanyWebsite;
            user.CompanyProfile.FoundedYear = model.FoundedYear;
            user.CompanyProfile.Location = model.Location;
            user.CompanyProfile.NumberOfEmployees = model.NumberOfEmployees;
            user.CompanyProfile.LinkedIn = model.LinkedIn;
            user.CompanyProfile.Twitter = model.Twitter;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated!";
            return RedirectToAction("ManageProfile");
        }
    }
}

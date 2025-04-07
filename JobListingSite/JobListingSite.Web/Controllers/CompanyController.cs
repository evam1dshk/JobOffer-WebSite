using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobListingSite.Web.Data;
using JobListingSite.Data.Entities;
using JobListingSite.Web.Models.Company;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Amazon.S3.Model;
using Amazon.Runtime;
using JobListingSite.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using JobListingSite.Data.Enums;

namespace JobListingSite.Web.Controllers
{
    [Authorize(Roles = "Company")]
    public class CompanyController : Controller
    {
        private readonly JobListingDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;

        public CompanyController(JobListingDbContext context, UserManager<User> userManager, IWebHostEnvironment hostEnvironment, IAmazonS3 s3Client, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _hostEnvironment = hostEnvironment;
            _s3Client = s3Client;
            _configuration = configuration;
        }
        private async Task<string> UploadFileToS3Async(IFormFile file, string folder)
        {
            var bucketName = _configuration["AWS:BucketName"];
            var fileName = $"{folder}/{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";

            try
            {
                using var stream = file.OpenReadStream();

                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileName,
                    InputStream = stream,
                    ContentType = file.ContentType
                };

                var response = await _s3Client.PutObjectAsync(request);

                if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    throw new Exception("Upload failed");

                return $"https://{bucketName}.s3.amazonaws.com/{fileName}";
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine("AmazonS3Exception: " + ex.Message);
                throw;
            }
            catch (AmazonServiceException ex)
            {
                Console.WriteLine("AmazonServiceException: " + ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("General Exception: " + ex.Message);
                throw;
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Profile(string id, string? returnTo)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var company = _context.Users
                .Include(u => u.CompanyProfile)
                .FirstOrDefault(u => u.Id == id && u.IsCompany && u.CompanyProfile != null);

            if (company == null)
                return NotFound();

            ViewData["ReturnTo"] = returnTo;
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
            var userId = _userManager.GetUserId(User);
            var user = await _context.Users
                .Include(u => u.CompanyProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return NotFound();
            if (!ModelState.IsValid) return View(model);

            // Upload to S3
            if (model.Logo != null)
            {
                user.CompanyProfile.LogoUrl = await UploadFileToS3Async(model.Logo, "logos");
            }

            if (model.Banner != null)
            {
                user.CompanyProfile.BannerImageUrl = await UploadFileToS3Async(model.Banner, "banners");
            }

            // Save other fields
            user.CompanyProfile.CompanyName = model.CompanyName;
            user.CompanyProfile.Description = model.Description;
            user.CompanyProfile.Industry = model.Industry;
            user.CompanyProfile.Phone = model.Phone;
            user.CompanyProfile.ContactEmail = model.ContactEmail;
            user.CompanyProfile.CompanyWebsite = model.CompanyWebsite;
            user.CompanyProfile.FoundedYear = model.FoundedYear;
            user.CompanyProfile.Location = model.Location;
            user.CompanyProfile.LinkedIn = model.LinkedIn;
            user.CompanyProfile.Twitter = model.Twitter;
            user.CompanyProfile.NumberOfEmployees = model.NumberOfEmployees;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated with your Canva logo!";
            return RedirectToAction("ManageProfile");
        }

        [Authorize(Roles = "Company")]
        public async Task<IActionResult> ManageJobs()
        {
            var userId = _userManager.GetUserId(User);

            var jobs = await _context.Offers
                .Where(o => o.CompanyId == userId)
                .Include(o => o.Category)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(jobs);
        }
        // GET: Company/AddJob
        [HttpGet]
        [Authorize(Roles = "Company")]
        public IActionResult AddJob()
        {
            var categories = _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList();

            var model = new JobFormViewModel
            {
                Categories = categories
            };

            return View(model);
        }

        // POST: Company/AddJob
        [HttpPost]
        [Authorize(Roles = "Company")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddJob(JobFormViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var company = await _context.Users.Include(c => c.CompanyProfile).FirstOrDefaultAsync(c => c.Id == userId);

            if (company == null || company.CompanyProfile == null)
                return Unauthorized();

            if (!ModelState.IsValid)
            {
                model.Categories = _context.Categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList();
                return View(model);
            }

            var newJob = new Offer
            {
                Title = model.Title,
                Description = model.Description,
                Salary = model.Salary,
                CategoryId = model.CategoryId,
                CompanyId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Offers.Add(newJob);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job offer posted successfully!";
            return RedirectToAction("ManageJobs");
        }


        [HttpGet]
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> EditJob(int id)
        {
            var job = await _context.Offers.FirstOrDefaultAsync(j => j.OfferId == id && j.CompanyId == _userManager.GetUserId(User));
            if (job == null) return NotFound();

            var model = new JobFormViewModel
            {
                OfferId = job.OfferId,
                Title = job.Title,
                Description = job.Description,
                Salary = job.Salary,
                CategoryId = job.CategoryId,
                Categories = _context.Categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList()
            };

            return View("AddJob", model); // Reuse AddJob view
        }

        [HttpPost]
        [Authorize(Roles = "Company")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJob(JobFormViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            var job = await _context.Offers
                .FirstOrDefaultAsync(j => j.OfferId == model.OfferId && j.CompanyId == userId);

            if (job == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                model.Categories = _context.Categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                }).ToList();

                return View("AddJob", model);
            }

            // ✅ Update job
            job.Title = model.Title;
            job.Description = model.Description;
            job.Salary = model.Salary;
            job.CategoryId = model.CategoryId;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Job updated successfully!";
            return RedirectToAction("ManageJobs");
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteJob(int id)
        {
            var job = await _context.Offers.FirstOrDefaultAsync(j => j.OfferId == id && j.CompanyId == _userManager.GetUserId(User));
            if (job == null) return NotFound();

            _context.Offers.Remove(job);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job deleted successfully!";
            return RedirectToAction("ManageJobs");
        }
        [Authorize(Roles = "Company")]
        public async Task<IActionResult> ViewApplications(int id)
        {
            var userId = _userManager.GetUserId(User);

            var offer = await _context.Offers
                .Include(o => o.JobApplications)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(o => o.OfferId == id && o.CompanyId == userId);

            if (offer == null)
                return NotFound();

            return View(offer);
        }
        [HttpPost]
        [Authorize(Roles = "Company")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveApplication(int applicationId)
        {
            var application = await _context.JobApplications
                .Include(a => a.Offer)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.Offer.CompanyId == _userManager.GetUserId(User));

            if (application == null)
                return NotFound();

            application.Status = ApplicationStatus.Approved;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Application approved!";
            return RedirectToAction("ViewApplications", new { id = application.OfferId });
        }

        [HttpPost]
        [Authorize(Roles = "Company")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectApplication(int applicationId)
        {
            var application = await _context.JobApplications
                .Include(a => a.Offer)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.Offer.CompanyId == _userManager.GetUserId(User));

            if (application == null)
                return NotFound();

            application.Status = ApplicationStatus.Rejected;
            await _context.SaveChangesAsync();

            TempData["WarningMessage"] = "Application rejected.";
            return RedirectToAction("ViewApplications", new { id = application.OfferId });
        }
        [HttpPost]
        [Authorize(Roles = "Company")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateApplicationStatus(int applicationId, string status)
        {
            var application = await _context.JobApplications
                .Include(a => a.Offer)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.Offer.CompanyId == _userManager.GetUserId(User));

            if (application == null)
                return NotFound();

            if (Enum.TryParse(status, out ApplicationStatus parsedStatus))
            {
                application.Status = parsedStatus;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Application marked as {parsedStatus}";
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid status update.";
            }

            return RedirectToAction("ManageApplications", new { offerId = application.OfferId });
        }

    }
}

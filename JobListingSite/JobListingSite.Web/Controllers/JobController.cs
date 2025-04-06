using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobListingSite.Web.Data;
using JobListingSite.Data.Entities;
using JobListingSite.Web.Models.JobListing;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JobListingSite.Web.Controllers
{
    [AllowAnonymous]
    public class JobController : Controller
    {
        private readonly JobListingDbContext _context;
        private readonly UserManager<User> _userManager;

        public JobController(JobListingDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Existing actions: Index, Details...
        public IActionResult Browse(string searchTerm, int? categoryId)
        {
            var offersQuery = _context.Offers
                .Include(o => o.Category)
                .Include(o => o.Company)
                    .ThenInclude(c => c.CompanyProfile)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerTerm = searchTerm.ToLower();
                offersQuery = offersQuery.Where(o =>
                    o.Title.ToLower().Contains(lowerTerm) ||
                    o.Description.ToLower().Contains(lowerTerm) ||
                    o.Category.Name.ToLower().Contains(lowerTerm) ||
                    o.Company.CompanyProfile.CompanyName.ToLower().Contains(lowerTerm)
                );
            }


            if (categoryId.HasValue)
            {
                offersQuery = offersQuery.Where(o => o.CategoryId == categoryId.Value);
            }

            var offers = offersQuery
                .Select(o => new OfferCardViewModel
                {
                    Id = o.OfferId,
                    Title = o.Title,
                    DescriptionSnippet = o.Description.Length > 100
                        ? o.Description.Substring(0, 100) + "..."
                        : o.Description,
                    CompanyName = o.Company.CompanyProfile.CompanyName,
                    CategoryName = o.Category.Name,
                    CreatedAt = o.CreatedAt
                })
                .ToList();

            var model = new BrowseViewModel
            {
                Offers = offers,
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                Categories = _context.Categories
                    .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.Name })
                    .ToList()
            };

            return View(model);
        }

        public IActionResult Details(int id)
        {
            var job = _context.Offers
                .Include(o => o.Category)
                .FirstOrDefault(o => o.OfferId == id);

            if (job == null)
                return NotFound();

            return View(job);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(int offerId)
        {
            var user = await _userManager.GetUserAsync(User);

            // Check if the job offer exists
            var offer = await _context.Offers.FindAsync(offerId);
            if (offer == null)
            {
                TempData["ErrorMessage"] = "Job offer not found.";
                return RedirectToAction("Index");
            }

            // Check if the user already applied
            var alreadyApplied = await _context.JobApplications
                .AnyAsync(a => a.OfferId == offerId && a.UserId == user.Id);

            if (alreadyApplied)
            {
                TempData["WarningMessage"] = "You have already applied for this job.";
                return RedirectToAction("Details", new { id = offerId });
            }

            var application = new JobApplication
            {
                OfferId = offerId,
                UserId = user.Id,
                AppliedOn = DateTime.UtcNow
            };

            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your application has been submitted!";
            return RedirectToAction("Details", new { id = offerId });
        }
    }
}

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
        [AllowAnonymous]
        public IActionResult Browse(string searchTerm, int? categoryId, int page = 1)
        {
            const int PageSize = 6;

            var query = _context.Offers
                .Include(o => o.Company)
                    .ThenInclude(c => c.CompanyProfile)
                .Include(o => o.Category)
                .AsQueryable();

            // ✅ Filter only offers with valid companies
            query = query.Where(o => o.Company != null && o.Company.CompanyProfile != null);

            // ✅ Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(o => o.Title.ToLower().Contains(searchTerm));
            }

            // ✅ Apply category filter
            if (categoryId.HasValue)
            {
                query = query.Where(o => o.CategoryId == categoryId);
            }

            var totalOffers = query.Count();

            var offers = query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var offerCards = offers.Select(o => new OfferCardViewModel
            {
                Id = o.OfferId,
                Title = o.Title,
                CompanyName = o.Company.CompanyProfile.CompanyName,
                CategoryName = o.Category.Name,
                CreatedAt = o.CreatedAt,
                DescriptionSnippet = o.Description.Length > 100
                    ? o.Description[..100] + "..."
                    : o.Description,
                ApplicantsCount = _context.JobApplications.Count(a => a.OfferId == o.OfferId),
                CompanyUserId = o.CompanyId
            }).ToList();

            var categories = _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.Name
                })
                .ToList();

            var model = new BrowseViewModel
            {
                Offers = offerCards,
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                Categories = categories,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalOffers / (double)PageSize)
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var job = await _context.Offers
                .Include(o => o.Category)
                .Include(o => o.Company)
                    .ThenInclude(c => c.CompanyProfile)
                .FirstOrDefaultAsync(o => o.OfferId == id);

            if (job == null)
                return NotFound();

            // Pass company flag via ViewData
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                ViewData["IsCompany"] = currentUser?.IsCompany ?? false;
            }
            else
            {
                ViewData["IsCompany"] = false;
            }

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
